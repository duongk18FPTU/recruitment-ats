using Microsoft.AspNetCore.Mvc;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Profile;
using RecruitmentAts.Application.Profile.Contracts;

namespace RecruitmentAts.Api.Controllers;

[ApiController]
[Route("api/me")]
public sealed class ProfileController(ProfileService profile, ActorAccessor actors) : ControllerBase
{
    [HttpPut("password", Name = "changePassword")]
    [AllowPendingPasswordChange]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await profile.ChangePasswordAsync(request, actors.Current(), cancellationToken);
        return NoContent();
    }
}
