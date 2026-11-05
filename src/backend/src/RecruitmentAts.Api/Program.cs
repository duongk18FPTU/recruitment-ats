using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Api.Errors;
using RecruitmentAts.Api.ModelBinding;
using RecruitmentAts.Api.RateLimiting;
using RecruitmentAts.Application;
using RecruitmentAts.Infrastructure;
using RecruitmentAts.Infrastructure.Persistence;
using RecruitmentAts.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers(options => options.ModelBinderProviders.Insert(0, new UpperSnakeEnumModelBinderProvider()))
    .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));
builder.Services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context => ProblemDetailsMapper.Complete(context.ProblemDetails, context.HttpContext));
builder.Services.AddExceptionHandler<AppExceptionHandler>();

// Bearer access tokens (AD-06). Claims keep their JWT names (sub, role, sid); every endpoint
// requires a signed-in user unless it is marked [AllowAnonymous].
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = JwtSettings.From(builder.Configuration).ValidationParameters();
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddAtsRateLimits();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ActorAccessor>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// One-off deployment step: `dotnet RecruitmentAts.Api.dll migrate` applies migrations and seed data, then exits.
if (args.Contains("migrate", StringComparer.Ordinal))
{
    await app.Services.MigrateAndSeedAsync(app.Lifetime.ApplicationStopping);
    return;
}

_ = JwtSettings.From(app.Configuration); // fail at startup, not on the first request, without Jwt__SigningKey

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<PasswordChangeGate>();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

await app.RunAsync();

// Enums travel as UPPER_SNAKE_CASE, like the database values; numbers are rejected.
static void ConfigureJson(JsonSerializerOptions options) =>
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));
