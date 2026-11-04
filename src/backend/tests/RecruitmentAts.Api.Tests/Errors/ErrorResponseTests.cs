using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Api.Tests.Errors;

/// <summary>Every error leaves the API as the problem+json of the contract.</summary>
public class ErrorResponseTests(ErrorResponseTests.Factory factory) : IClassFixture<ErrorResponseTests.Factory>
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task AppException_becomes_its_status_with_code_and_extensions()
    {
        var (response, problem) = await GetProblemAsync("/test/conflict");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("https://ats.example.vn/errors/invalid-stage-transition", problem.GetProperty("type").GetString());
        Assert.Equal("Invalid stage transition", problem.GetProperty("title").GetString());
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.Equal("Không thể chuyển từ NEW sang INTERVIEW", problem.GetProperty("detail").GetString());
        Assert.Equal("/test/conflict", problem.GetProperty("instance").GetString());
        Assert.Equal("INVALID_STAGE_TRANSITION", problem.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
        Assert.Equal(["SCREENING", "REJECTED"],
            problem.GetProperty("allowedStages").EnumerateArray().Select(s => s.GetString()));
    }

    [Fact]
    public async Task Unexpected_exception_is_INTERNAL_ERROR_without_its_message()
    {
        var (response, problem) = await GetProblemAsync("/test/crash");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("INTERNAL_ERROR", problem.GetProperty("errorCode").GetString());
        Assert.DoesNotContain("secret", problem.GetRawText(), StringComparison.Ordinal);
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Unknown_route_is_NOT_FOUND()
    {
        var (response, problem) = await GetProblemAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("NOT_FOUND", problem.GetProperty("errorCode").GetString());
        Assert.Equal("https://ats.example.vn/errors/not-found", problem.GetProperty("type").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("detail").GetString()));
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Invalid_body_is_VALIDATION_FAILED_with_camel_case_field_errors()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(new Uri("/test/validate", UriKind.Relative), new { weight = 9 }, _ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", problem.GetProperty("errorCode").GetString());
        var fields = problem.GetProperty("errors").EnumerateObject().Select(p => p.Name).Order();
        Assert.Equal(["fullName", "weight"], fields);
    }

    [Fact]
    public async Task Anonymous_request_has_a_candidate_actor_with_a_trace_id()
    {
        using var client = factory.CreateClient();

        var actor = await client.GetFromJsonAsync<JsonElement>(new Uri("/test/actor", UriKind.Relative), _ct);

        Assert.Equal("CANDIDATE", actor.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Null, actor.GetProperty("userId").ValueKind);
        Assert.False(string.IsNullOrEmpty(actor.GetProperty("traceId").GetString()));
    }

    private async Task<(HttpResponseMessage Response, JsonElement Problem)> GetProblemAsync(string path)
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync(new Uri(path, UriKind.Relative), _ct);
        return (response, await response.Content.ReadFromJsonAsync<JsonElement>(_ct));
    }

    /// <summary>The real API plus <see cref="ErrorTestController"/>, which only exists in tests.</summary>
    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureTestServices(services =>
                services.AddControllers().AddApplicationPart(typeof(ErrorTestController).Assembly));
    }
}

[ApiController]
[Route("test")]
public sealed class ErrorTestController(ActorAccessor actors) : ControllerBase
{
    [HttpGet("conflict")]
    public IActionResult ThrowConflict() => throw new AppException(
        ErrorCode.InvalidStageTransition,
        "Không thể chuyển từ NEW sang INTERVIEW",
        new Dictionary<string, object?> { ["allowedStages"] = new[] { Stage.Screening, Stage.Rejected } });

    [HttpGet("crash")]
    public IActionResult ThrowUnexpected() => throw new InvalidOperationException("secret connection string");

    [HttpPost("validate")]
    public IActionResult Validate(ValidateInput input) => Ok(input);

    [HttpGet("actor")]
    public IActionResult CurrentActor()
    {
        var actor = actors.Current();
        return Ok(new { actor.Type, actor.UserId, actor.TraceId });
    }

    public sealed record ValidateInput([Required] string? FullName, [Range(1, 5)] int Weight);
}
