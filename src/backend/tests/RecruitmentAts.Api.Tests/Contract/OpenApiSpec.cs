using YamlDotNet.RepresentationModel;

namespace RecruitmentAts.Api.Tests.Contract;

public sealed record SpecOperation(string OperationId, string Method, string Path);

/// <summary>Reads docs/api/openapi.yaml, the source of truth for the HTTP contract.</summary>
public static class OpenApiSpec
{
    private static readonly string[] Methods = ["get", "post", "put", "patch", "delete"];

    private static readonly Lazy<YamlMappingNode> Root = new(Load);

    public static IReadOnlyList<SpecOperation> Operations()
    {
        var operations = new List<SpecOperation>();
        foreach (var (path, item) in Map(Root.Value, "paths").Children)
        {
            foreach (var (method, operation) in ((YamlMappingNode)item).Children)
            {
                var name = ((YamlScalarNode)method).Value!;
                if (Methods.Contains(name))
                {
                    var operationId = ((YamlScalarNode)((YamlMappingNode)operation)["operationId"]).Value!;
                    operations.Add(new SpecOperation(operationId, name.ToUpperInvariant(), ((YamlScalarNode)path).Value!));
                }
            }
        }

        return operations;
    }

    public static IReadOnlyList<string> ErrorCodes() =>
        [.. ((YamlSequenceNode)Map(Map(Map(Root.Value, "components"), "schemas"), "ErrorCode")["enum"])
            .Children.Select(node => ((YamlScalarNode)node).Value!)];

    private static YamlMappingNode Map(YamlMappingNode node, string key) => (YamlMappingNode)node[key];

    private static YamlMappingNode Load()
    {
        using var reader = new StreamReader(FindSpec());
        var stream = new YamlStream();
        stream.Load(reader);
        return (YamlMappingNode)stream.Documents[0].RootNode;
    }

    private static string FindSpec()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "docs", "api", "openapi.yaml");
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("docs/api/openapi.yaml not found above " + AppContext.BaseDirectory);
    }
}
