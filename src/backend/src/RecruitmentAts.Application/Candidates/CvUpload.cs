namespace RecruitmentAts.Application.Candidates;

/// <summary>A CV file as received from any intake channel; the caller owns and disposes the stream.</summary>
public sealed record CvUpload(string FileName, string ContentType, Stream Content);
