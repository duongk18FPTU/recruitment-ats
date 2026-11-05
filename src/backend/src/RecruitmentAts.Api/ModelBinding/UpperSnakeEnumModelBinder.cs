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
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type.IsEnum ? new UpperSnakeEnumModelBinder(type) : null;
    }
}

public sealed class UpperSnakeEnumModelBinder(Type enumType) : IModelBinder
{
    private readonly Dictionary<string, object> _values = Enum.GetValues(enumType).Cast<object>()
        .ToDictionary(value => JsonNamingPolicy.SnakeCaseUpper.ConvertName(value.ToString()!), StringComparer.Ordinal);

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
