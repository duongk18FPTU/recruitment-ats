using FluentValidation;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Applications.Contracts;
using RecruitmentAts.Application.Candidates.Contracts;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Validation;

namespace RecruitmentAts.Application.Candidates;

/// <summary>Finding the person behind a CV (BR-06).</summary>
public sealed class CandidateService(
    ICandidateRepository candidates,
    IApplicationRepository applications,
    IValidator<CandidateMatchQuery> matchValidator,
    IClock clock)
{
    /// <summary>
    /// The active candidate with the same normalized email or phone, so the recruiter sees a duplicate before
    /// saving. The email wins when it and the phone point at different candidates.
    /// </summary>
    public async Task<CandidateMatch> MatchAsync(CandidateMatchQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        await matchValidator.ValidateOrThrowAsync(query, cancellationToken);

        var (candidate, kind) = await FindAsync(
            ContactNormalizer.Email(query.Email), ContactNormalizer.Phone(query.Phone), cancellationToken);
        if (candidate is null)
        {
            return new CandidateMatch(false, null, null, false, null);
        }

        var existing = query.PositionId is { } positionId
            ? await applications.FindIdAsync(positionId, candidate.Id, cancellationToken)
            : null;
        return new CandidateMatch(true, kind, Summary(candidate, clock.UtcNow), existing is not null, existing);
    }

    internal async Task<(Candidate? Candidate, CandidateMatchKind? Kind)> FindAsync(
        string? emailNormalized, string? phoneNormalized, CancellationToken cancellationToken)
    {
        if (emailNormalized is not null && await candidates.FindByEmailAsync(emailNormalized, cancellationToken) is { } byEmail)
        {
            return (byEmail, CandidateMatchKind.Email);
        }

        if (phoneNormalized is not null && await candidates.FindByPhoneAsync(phoneNormalized, cancellationToken) is { } byPhone)
        {
            return (byPhone, CandidateMatchKind.Phone);
        }

        return (null, null);
    }

    internal static CandidateSummary Summary(Candidate candidate, DateTimeOffset now) => new(
        candidate.Id, candidate.FullName, candidate.Email, candidate.Phone, candidate.EmailInvalid,
        candidate.PoolConsentExpiresAt > now);
}
