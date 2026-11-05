using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RecruitmentAts.Api.Tests.Contract;

public sealed record MappedRoute(string? Name, string Method, string Path);

/// <summary>
/// Every route under /api is named after an operationId of docs/api/openapi.yaml and has the same
/// method and path; no operationId has two routes. Operations whose story is not built yet have
/// no route.
/// </summary>
public sealed partial class OpenApiContractTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public void Spec_has_unique_operation_ids()
    {
        var operations = OpenApiSpec.Operations();

        Assert.NotEmpty(operations);
        Assert.Empty(operations.GroupBy(o => o.OperationId).Where(g => g.Count() > 1).Select(g => g.Key));
    }

    [Fact]
    public void Api_routes_match_the_spec()
    {
        var problems = Compare(OpenApiSpec.Operations(), MappedRoutes(factory.Services));

        Assert.True(problems.Count == 0, string.Join('\n', problems));
    }

    [Fact]
    public void Comparison_reports_every_kind_of_mismatch()
    {
        SpecOperation[] spec =
        [
            new("moveApplicationStage", "POST", "/api/applications/{applicationId}/stage"),
            new("listPositions", "GET", "/api/positions"),
        ];
        MappedRoute[] routes =
        [
            new("moveApplicationStage", "POST", "/api/applications/{applicationId}/stage"),
            new("moveApplicationStage", "POST", "/api/applications/{applicationId}/stage"),
            new("listPositions", "POST", "/api/positions"),
            new("unknownOperation", "GET", "/api/unknown"),
            new(null, "GET", "/api/unnamed"),
        ];

        var problems = Compare(spec, routes);

        Assert.Equal(4, problems.Count);
        Assert.Contains(problems, p => p.Contains("moveApplicationStage has 2 routes", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("POST /api/positions", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("unknownOperation", StringComparison.Ordinal));
        Assert.Contains(problems, p => p.Contains("/api/unnamed has no route name", StringComparison.Ordinal));
    }

    [Fact]
    public void Route_constraints_are_ignored_when_comparing_paths()
    {
        Assert.Equal("/api/applications/{applicationId}/stage", NormalizePath("api/applications/{applicationId:guid}/stage"));
    }

    private static List<string> Compare(IReadOnlyList<SpecOperation> spec, IReadOnlyList<MappedRoute> routes)
    {
        var byId = spec.ToDictionary(o => o.OperationId);
        var problems = new List<string>();

        foreach (var route in routes)
        {
            if (route.Name is null)
            {
                problems.Add($"{route.Method} {route.Path} has no route name (Name = \"<operationId>\")");
            }
            else if (!byId.TryGetValue(route.Name, out var operation))
            {
                problems.Add($"{route.Method} {route.Path}: '{route.Name}' is not an operationId in openapi.yaml");
            }
            else if (operation.Method != route.Method || operation.Path != route.Path)
            {
                problems.Add($"{route.Name}: route is {route.Method} {route.Path}, spec is {operation.Method} {operation.Path}");
            }
        }

        problems.AddRange(routes
            .Where(r => r.Name is not null)
            .GroupBy(r => r.Name)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} has {g.Count()} routes"));

        return problems;
    }

    private static List<MappedRoute> MappedRoutes(IServiceProvider services) =>
    [
        .. services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => NormalizePath(e.RoutePattern.RawText ?? "").StartsWith("/api/", StringComparison.Ordinal))
            .SelectMany(e => (e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["ANY"])
                .Select(method => new MappedRoute(
                    e.Metadata.GetMetadata<IRouteNameMetadata>()?.RouteName,
                    method,
                    NormalizePath(e.RoutePattern.RawText!)))),
    ];

    private static string NormalizePath(string rawText) => "/" + RouteParameter().Replace(rawText.TrimStart('/'), "{$1}");

    // {id:guid}, {page=1}, {slug?} → {id}, {page}, {slug}
    [GeneratedRegex(@"\{\**([^}:=?]+)[^}]*\}")]
    private static partial Regex RouteParameter();
}
