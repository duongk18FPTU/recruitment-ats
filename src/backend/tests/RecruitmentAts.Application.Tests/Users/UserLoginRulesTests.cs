using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Tests.Users;

public class UserLoginRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 9, 0, 0, TimeSpan.Zero);

    private static User NewUser() => new() { Email = "a@example.test", PasswordHash = "h", FullName = "Lê Hoàng Nam", Role = Role.Recruiter };

    [Fact]
    public void Four_failures_do_not_lock_the_fifth_locks_for_15_minutes()
    {
        var user = NewUser();
        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(Now);
        }
        Assert.False(user.IsLocked(Now));

        user.RegisterFailedLogin(Now);

        Assert.True(user.IsLocked(Now));
        Assert.True(user.IsLocked(Now.AddMinutes(14)));
        Assert.False(user.IsLocked(Now.AddMinutes(15)));
    }

    [Fact]
    public void After_a_lockout_the_count_starts_again()
    {
        var user = NewUser();
        for (var i = 0; i < 5; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        user.RegisterFailedLogin(Now.AddMinutes(16));

        Assert.Equal(1, user.FailedLoginCount);
        Assert.False(user.IsLocked(Now.AddMinutes(16)));
    }

    [Fact]
    public void A_successful_login_clears_failures_and_records_the_time()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now);

        user.RegisterSuccessfulLogin(Now.AddMinutes(1));

        Assert.Equal(0, user.FailedLoginCount);
        Assert.Null(user.LockedUntil);
        Assert.Equal(Now.AddMinutes(1), user.LastLoginAt);
    }

    [Fact]
    public void Changing_the_password_clears_the_issued_password_flag()
    {
        var user = NewUser();
        user.MustChangePassword = true;

        user.ChangePassword("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
        Assert.False(user.MustChangePassword);
    }
}
