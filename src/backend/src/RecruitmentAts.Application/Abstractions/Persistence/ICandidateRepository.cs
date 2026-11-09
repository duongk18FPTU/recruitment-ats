using RecruitmentAts.Application.Candidates;

namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface ICandidateRepository
{
    /// <summary>The active (not anonymized) candidate with this normalized email, or null.</summary>
    Task<Candidate?> FindByEmailAsync(string emailNormalized, CancellationToken cancellationToken);

    /// <summary>The active (not anonymized) candidate with this normalized phone number, or null.</summary>
    Task<Candidate?> FindByPhoneAsync(string phoneNormalized, CancellationToken cancellationToken);

    void Add(Candidate candidate);

    void Add(CvFile cvFile);
}
