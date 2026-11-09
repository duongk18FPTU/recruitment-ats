using FluentValidation;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Applications.Contracts;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Screening;

namespace RecruitmentAts.Application.Applications;

/// <summary>Applications and their pipeline (US08, US09, US11).</summary>
public sealed class ApplicationService(
    IApplicationRepository applications,
    ICandidateRepository candidates,
    IPositionRepository positions,
    IAnalysisRepository analyses,
    CandidateService candidateService,
    CvFileService cvFiles,
    IUnitOfWork unitOfWork,
    AuditWriter audit,
    IValidator<ApplicationCreateRequest> createValidator,
    IClock clock)
{
    /// <summary>
    /// Adds an application with a CV (US09). The position must be <c>OPEN</c> (BR-05); a candidate with the same
    /// normalized email or phone is reused (BR-06) and may not apply to the same position twice. The application
    /// starts in <c>NEW</c> with its first history row and a <c>PENDING</c> analysis for the Worker; the receipt
    /// email waits for US27. When anything fails, no row is saved and the stored file is removed.
    /// </summary>
    public async Task<ApplicationCreated> CreateAsync(ApplicationCreateRequest request, Actor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        CvFile? stored = null;
        try
        {
            var (applicationId, matched) = await unitOfWork.ExecuteAsync(async () =>
            {
                var position = await positions.GetAsync(request.PositionId!.Value, cancellationToken)
                    ?? throw AppException.NotFound("Không tìm thấy vị trí tuyển dụng.");
                if (position.Status != PositionStatus.Open)
                {
                    throw new AppException(
                        ErrorCode.PositionNotOpen,
                        "Vị trí này không ở trạng thái đang tuyển, nên không thể thêm hồ sơ.");
                }

                var emailNormalized = ContactNormalizer.Email(request.Email);
                var phoneNormalized = ContactNormalizer.Phone(request.Phone);
                var (existing, _) = await candidateService.FindAsync(emailNormalized, phoneNormalized, cancellationToken);
                if (existing is not null
                    && await applications.FindIdAsync(position.Id, existing.Id, cancellationToken) is { } existingApplicationId)
                {
                    throw new AppException(
                        ErrorCode.DuplicateApplication,
                        "Ứng viên này đã có hồ sơ ở vị trí này.",
                        new Dictionary<string, object?> { ["existingApplicationId"] = existingApplicationId });
                }

                var now = clock.UtcNow;
                var candidate = existing ?? NewCandidate(request, emailNormalized, phoneNormalized, now);
                if (existing is null)
                {
                    candidates.Add(candidate);
                }

                stored = await cvFiles.StoreAsync(candidate.Id, request.Cv!, actor.UserId, cancellationToken);
                candidates.Add(stored);

                var application = new JobApplication
                {
                    PositionId = position.Id,
                    CandidateId = candidate.Id,
                    Stage = Stage.New,
                    Source = request.Source!.Value,
                    SourceNote = request.Source == Source.Other ? request.SourceNote!.Trim() : null,
                    IntakeChannel = IntakeChannel.Manual,
                    CurrentCvFileId = stored.Id,
                    CreatedBy = actor.UserId,
                    AppliedAt = now,
                    StageChangedAt = now,
                    UpdatedAt = now,
                };
                applications.Add(application);
                applications.Add(new StageHistoryEntry
                {
                    ApplicationId = application.Id,
                    FromStage = null,
                    ToStage = Stage.New,
                    ActorId = actor.UserId,
                    CreatedAt = now,
                });
                analyses.Add(new CvAnalysis
                {
                    ApplicationId = application.Id,
                    CvFileId = stored.Id,
                    CriteriaVersion = position.CriteriaVersion,
                    Status = AnalysisStatus.Pending,
                    AvailableAt = now,
                    RequestedBy = actor.UserId,
                    RequestedAt = now,
                });

                audit.Write(actor, AuditModule.Application, AuditAction.Create, "Application", application.Id,
                    $"Thêm hồ sơ {candidate.FullName} vào vị trí {position.Code} {position.Title}",
                    new
                    {
                        positionId = position.Id,
                        candidateId = candidate.Id,
                        candidateMatched = existing is not null,
                        application.Source,
                        cvFileId = stored.Id,
                    });
                return (application.Id, existing is not null);
            }, cancellationToken);

            var detail = await applications.GetDetailAsync(applicationId, cancellationToken)
                ?? throw AppException.NotFound("Không tìm thấy hồ sơ ứng tuyển.");
            return new ApplicationCreated(
                detail with { AllowedStages = StageTransitions.AllowedNext(detail.Stage, actor.Role) },
                matched,
                ReceiptEmailQueued: false);
        }
        catch
        {
            if (stored is not null)
            {
                await cvFiles.DiscardAsync(stored, CancellationToken.None);
            }

            throw;
        }
    }

    private static Candidate NewCandidate(
        ApplicationCreateRequest request, string? emailNormalized, string? phoneNormalized, DateTimeOffset now) => new()
    {
        FullName = request.FullName!.Trim(),
        Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
        EmailNormalized = emailNormalized,
        Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
        PhoneNormalized = phoneNormalized,
        Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        LastActivityAt = now,
    };
}
