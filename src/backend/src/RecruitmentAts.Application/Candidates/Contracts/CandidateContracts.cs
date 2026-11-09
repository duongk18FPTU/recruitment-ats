using RecruitmentAts.Application.Applications.Contracts;

namespace RecruitmentAts.Application.Candidates.Contracts;

/// <summary>Query of <c>matchCandidate</c>: at least one of email or phone.</summary>
public sealed record CandidateMatchQuery(string? Email, string? Phone, Guid? PositionId);

/// <summary>Which contact identified the candidate; serialized as <c>EMAIL</c> / <c>PHONE</c>.</summary>
public enum CandidateMatchKind
{
    Email,
    Phone,
}

/// <summary>OpenAPI <c>CandidateMatch</c>.</summary>
public sealed record CandidateMatch(
    bool Matched,
    CandidateMatchKind? MatchedBy,
    CandidateSummary? Candidate,
    bool AlreadyApplied,
    Guid? ExistingApplicationId);
