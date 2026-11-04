using System.Text.Json;
using System.Text.Json.Serialization;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Api.Errors;
using RecruitmentAts.Application;
using RecruitmentAts.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));
builder.Services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context => ProblemDetailsMapper.Complete(context.ProblemDetails, context.HttpContext));
builder.Services.AddExceptionHandler<AppExceptionHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ActorAccessor>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Enums travel as UPPER_SNAKE_CASE, like the database values; numbers are rejected.
static void ConfigureJson(JsonSerializerOptions options) =>
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false));
