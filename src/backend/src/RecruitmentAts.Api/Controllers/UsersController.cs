using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Application.Users.Contracts;

namespace RecruitmentAts.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class UsersController(UserService users, ActorAccessor actors) : ControllerBase
{
    [HttpGet(Name = "listUsers")]
    public Task<PagedResult<UserAccount>> List([FromQuery] UserQuery query, CancellationToken cancellationToken) =>
        users.ListAsync(query, cancellationToken);

    [HttpPost(Name = "createUser")]
    public async Task<ActionResult<UserAccount>> Create(UserCreateRequest request, CancellationToken cancellationToken)
    {
        var user = await users.CreateAsync(request, actors.Current(), cancellationToken);
        return CreatedAtRoute("getUser", new { userId = user.Id }, user);
    }

    [HttpGet("{userId:guid}", Name = "getUser")]
    public Task<UserAccount> Get(Guid userId, CancellationToken cancellationToken) =>
        users.GetAsync(userId, cancellationToken);

    [HttpPut("{userId:guid}", Name = "updateUser")]
    public Task<UserAccount> Update(Guid userId, UserUpdateRequest request, CancellationToken cancellationToken) =>
        users.UpdateAsync(userId, request, actors.Current(), cancellationToken);

    [HttpPut("{userId:guid}/status", Name = "updateUserStatus")]
    public Task<UserAccount> UpdateStatus(Guid userId, UserStatusRequest request, CancellationToken cancellationToken) =>
        users.SetActiveAsync(userId, request, actors.Current(), cancellationToken);
}
