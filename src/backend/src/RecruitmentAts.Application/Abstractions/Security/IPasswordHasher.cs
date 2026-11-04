namespace RecruitmentAts.Application.Abstractions.Security;

public interface IPasswordHasher
{
    /// <summary>Salted, slow hash that stores its own parameters, so they can be raised later.</summary>
    string Hash(string password);

    bool Verify(string hash, string password);
}
