using Microsoft.AspNetCore.Http;
using RecruitmentAts.Api.Errors;
using RecruitmentAts.Api.Tests.Contract;
using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Api.Tests.Errors;

public class ProblemDetailsMapperTests
{
    [Fact]
    public void Error_codes_are_the_ones_in_the_contract()
    {
        var names = Enum.GetValues<ErrorCode>().Select(ProblemDetailsMapper.NameOf);

        Assert.Equal(OpenApiSpec.ErrorCodes(), names);
    }

    [Fact]
    public void Every_error_code_has_a_status() =>
        Assert.All(Enum.GetValues<ErrorCode>(), code => Assert.InRange(ProblemDetailsMapper.StatusOf(code), 400, 599));

    [Theory]
    [InlineData(ErrorCode.ValidationFailed, 400)]
    [InlineData(ErrorCode.NoExtractableText, 400)]
    [InlineData(ErrorCode.InvalidCredentials, 401)]
    [InlineData(ErrorCode.Forbidden, 403)]
    [InlineData(ErrorCode.ConsentTokenInvalid, 404)]
    [InlineData(ErrorCode.InvalidStageTransition, 409)]
    [InlineData(ErrorCode.JobPostExpired, 409)]
    [InlineData(ErrorCode.FileTooLarge, 413)]
    [InlineData(ErrorCode.RateLimited, 429)]
    [InlineData(ErrorCode.AiUnavailable, 502)]
    [InlineData(ErrorCode.InternalError, 500)]
    public void Status_follows_the_contract_table(ErrorCode code, int status) =>
        Assert.Equal(status, ProblemDetailsMapper.StatusOf(code));

    [Fact]
    public void Map_sets_type_title_code_and_extensions()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/applications/1/stage";
        var exception = new AppException(ErrorCode.InvalidStageTransition, "Không thể chuyển từ NEW sang INTERVIEW",
            new Dictionary<string, object?> { ["allowedStages"] = new[] { "SCREENING", "REJECTED" } });

        var problem = ProblemDetailsMapper.Map(exception, httpContext);

        Assert.Equal(409, problem.Status);
        Assert.Equal("https://ats.example.vn/errors/invalid-stage-transition", problem.Type);
        Assert.Equal("Invalid stage transition", problem.Title);
        Assert.Equal("Không thể chuyển từ NEW sang INTERVIEW", problem.Detail);
        Assert.Equal("/api/applications/1/stage", problem.Instance);
        Assert.Equal("INVALID_STAGE_TRANSITION", problem.Extensions["errorCode"]);
        Assert.Equal(new[] { "SCREENING", "REJECTED" }, problem.Extensions["allowedStages"]);
    }
}
