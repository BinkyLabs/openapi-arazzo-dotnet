using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoParameterValidator
{
    internal static void ValidateSerializationParameters(
        IEnumerable<IArazzoParameter>? parameters,
        string elementName,
        bool requiresLocation,
        string locationRequirementReason)
    {
        foreach (var error in Validate(parameters, elementName, requiresLocation, locationRequirementReason))
        {
            throw new ArazzoSerializationException(error);
        }
    }

    internal static IEnumerable<string> Validate(
        IEnumerable<IArazzoParameter>? parameters,
        string elementName,
        bool requiresLocation,
        string locationRequirementReason)
    {
        if (parameters is null)
        {
            yield break;
        }

        var parameterKeys = new HashSet<(string Name, ParameterLocation? In)>();
        foreach (var parameter in parameters)
        {
            var parameterName = parameter.Name;
            if (string.IsNullOrEmpty(parameterName))
            {
                continue;
            }

            if (!parameterKeys.Add((GetDuplicateDetectionName(parameterName, parameter.In), parameter.In)))
            {
                yield return $"{elementName} contains duplicate parameter '{parameterName}' in '{GetParameterLocationDisplayName(parameter.In)}'.";
            }

            if (requiresLocation && !parameter.In.HasValue)
            {
                yield return $"{elementName} parameter '{parameterName}' must specify 'in' {locationRequirementReason}.";
            }
        }
    }

    internal static void ValidateDeserializationRequiredFields(ArazzoParameter parameter, ParsingContext context)
    {
        AddRequiredFieldErrorIfMissing(parameter.Name, nameof(ArazzoParameter), nameof(ArazzoParameter.Name), context);
        AddRequiredFieldErrorIfMissing(parameter.Value, nameof(ArazzoParameter), nameof(ArazzoParameter.Value), context);
    }

    private static void AddRequiredFieldErrorIfMissing(object? value, string elementName, string fieldName, ParsingContext context)
    {
        if (value is null || value is string stringValue && string.IsNullOrEmpty(stringValue))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{elementName}.{fieldName} is a REQUIRED field."));
        }
    }

    private static string GetDuplicateDetectionName(string name, ParameterLocation? location) =>
        location == ParameterLocation.Header ? name.ToUpperInvariant() : name;

    private static string GetParameterLocationDisplayName(ParameterLocation? location) =>
        location.HasValue ? location.Value.GetDisplayName() : "<unspecified>";
}