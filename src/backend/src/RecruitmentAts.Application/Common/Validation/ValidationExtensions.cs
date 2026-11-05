using System.Text.Json;
using FluentValidation;

namespace RecruitmentAts.Application.Common.Validation;

public static class ValidationExtensions
{
    /// <summary>Throws <c>VALIDATION_FAILED</c> with camelCase field paths, as the contract names them.</summary>
    public static async Task ValidateOrThrowAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(validator);

        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(failure => CamelCasePath(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());
        throw AppException.Validation(errors);
    }

    private static string CamelCasePath(string path) =>
        string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
