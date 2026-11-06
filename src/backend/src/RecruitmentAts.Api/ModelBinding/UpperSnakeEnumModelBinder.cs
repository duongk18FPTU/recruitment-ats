using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RecruitmentAts.Api.ModelBinding;

/// <summary>
/// Binds enums in the query string and route by their contract names (<c>?role=HR_MANAGER</c>), like
/// JSON bodies. Numbers and C# names are rejected; an empty value leaves the filter unset.
/// </summary>
public sealed class UpperSnakeEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var modelType = context.Metadata.ModelType;
        var type = Nullable.GetUnderlyingType(modelType) ?? modelType;
        if (type.IsEnum)
        {
            return new UpperSnakeEnumModelBinder(type);
        }

        // OpenAPI style: form, explode: false — ?roles=RECRUITER,HR_MANAGER
        return modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
            && modelType.GenericTypeArguments[0].IsEnum
            ? new UpperSnakeEnumListModelBinder(modelType.GenericTypeArguments[0])
            : null;
    }
}

public sealed class UpperSnakeEnumModelBinder(Type enumType) : IModelBinder
{
    private readonly Dictionary<string, object> _values = UpperSnakeEnumNames.Of(enumType);

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (result == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);
        var text = result.FirstValue;
        if (string.IsNullOrEmpty(text))
        {
            return Task.CompletedTask;
        }

        if (_values.TryGetValue(text, out var value))
        {
            bindingContext.Result = ModelBindingResult.Success(value);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName, $"Giá trị không hợp lệ. Chấp nhận: {string.Join(", ", _values.Keys)}.");
        }

        return Task.CompletedTask;
    }
}

/// <summary>A comma-separated list of contract names; an empty value leaves the filter unset.</summary>
public sealed class UpperSnakeEnumListModelBinder(Type enumType) : IModelBinder
{
    private readonly Dictionary<string, object> _values = UpperSnakeEnumNames.Of(enumType);

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var result = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (result == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result);
        var names = result.Values
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToList();
        if (names.Count == 0)
        {
            return Task.CompletedTask;
        }

        var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(enumType))!;
        foreach (var name in names)
        {
            if (!_values.TryGetValue(name, out var value))
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName, $"Giá trị không hợp lệ. Chấp nhận: {string.Join(", ", _values.Keys)}.");
                return Task.CompletedTask;
            }

            list.Add(value);
        }

        bindingContext.Result = ModelBindingResult.Success(list);
        return Task.CompletedTask;
    }
}

internal static class UpperSnakeEnumNames
{
    public static Dictionary<string, object> Of(Type enumType) => Enum.GetValues(enumType).Cast<object>()
        .ToDictionary(value => JsonNamingPolicy.SnakeCaseUpper.ConvertName(value.ToString()!), StringComparer.Ordinal);
}
