namespace RecruitmentAts.Application.Users;

/// <summary>Table <c>users</c>.</summary>
public sealed class User
{
    /// <summary>Failed logins in a row that lock the account (docs/10-class-design.md §2).</summary>
    public const int MaxFailedLogins = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public required Role Role { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>True while the password is one an Admin issued (seed, US20); cleared by changing it.</summary>
    public bool MustChangePassword { get; set; }

    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsLocked(DateTimeOffset now) => LockedUntil > now;

    /// <summary>Counts a wrong password; the fifth in a row locks the account for 15 minutes.</summary>
    public void RegisterFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= MaxFailedLogins)
        {
            LockedUntil = now + LockoutDuration;
            FailedLoginCount = 0;
        }
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockedUntil = null;
        LastLoginAt = now;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        MustChangePassword = false;
    }

    /// <summary>An Admin sets a temporary password (US20): it must be replaced at the next login, and the lockout ends.</summary>
    public void IssuePassword(string temporaryPasswordHash)
    {
        PasswordHash = temporaryPasswordHash;
        MustChangePassword = true;
        FailedLoginCount = 0;
        LockedUntil = null;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
