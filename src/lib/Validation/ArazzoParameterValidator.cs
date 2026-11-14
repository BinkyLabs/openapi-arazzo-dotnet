using System.Text.Json.Nodes;

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

        if (GetQueryLocationError(parameters, elementName) is string error)
        {
            yield return error;
        }
    }

    internal static string? GetInheritedQueryLocationError(
        IEnumerable<IArazzoParameter>? inheritedParameters,
        IEnumerable<IArazzoParameter>? parameters,
        string elementName)
    {
        if (inheritedParameters is null || parameters is null ||
            GetQueryLocationError(inheritedParameters, elementName) is not null ||
            GetQueryLocationError(parameters, elementName) is not null)
        {
            return null;
        }

        return GetQueryLocationError(inheritedParameters.Concat(parameters), elementName);
    }

    private static string? GetQueryLocationError(IEnumerable<IArazzoParameter> parameters, string elementName) =>
        parameters.Any(static parameter => parameter.In is ParameterLocation.Query) &&
        parameters.Any(static parameter => parameter.In is ParameterLocation.QueryString)
            ? $"{elementName} 'query' and 'querystring' parameters cannot coexist."
            : null;

    internal static string? GetQueryStringValueError(IArazzoParameter parameter) =>
        parameter.In is ParameterLocation.QueryString &&
        parameter.Value?.Any is JsonNode literal &&
        (literal.IsJsonNullSentinel() || literal is not JsonValue value || !value.TryGetValue<string>(out _))
            ? $"querystring parameter '{parameter.Name}' value must resolve to a string."
            : null;

    internal static IEnumerable<string> ValidateActionParameters(IEnumerable<IArazzoParameter>? parameters, string elementName)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in parameters ?? [])
        {
            if (parameter.In.HasValue)
            {
                yield return $"{elementName} parameter '{parameter.Name}' must not specify 'in'.";
            }

            if (!string.IsNullOrEmpty(parameter.Name) && !names.Add(parameter.Name))
            {
                yield return $"{elementName} contains duplicate parameter '{parameter.Name}'.";
            }
        }
    }

    internal static void ValidateDeserializationRequiredFields(ArazzoParameter parameter, ParsingContext context)
    {
        AddRequiredFieldErrorIfMissing(parameter.Name, nameof(ArazzoParameter), nameof(ArazzoParameter.Name), context);
        AddRequiredFieldErrorIfMissing(parameter.Value, nameof(ArazzoParameter), nameof(ArazzoParameter.Value), context);
        if (GetQueryStringValueError(parameter) is string error)
        {
            context.Diagnostic.Errors.Add(new OpenApiError($"{context.GetLocation()}/{ArazzoConstants.ArazzoParameterValue}", error));
        }
    }

    private static void AddRequiredFieldErrorIfMissing(object? value, string elementName, string fieldName, ParsingContext context)
    {
        if (value is null || value is string stringValue && string.IsNullOrEmpty(stringValue))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{elementName}.{fieldName} is a REQUIRED field."));
        }
    }

    internal static string GetDuplicateDetectionName(string name, ParameterLocation? location) =>
        location == ParameterLocation.Header ? name.ToUpperInvariant() : name;

    private static string GetParameterLocationDisplayName(ParameterLocation? location) =>
        location.HasValue ? location.Value.GetDisplayName() : "<unspecified>";
}