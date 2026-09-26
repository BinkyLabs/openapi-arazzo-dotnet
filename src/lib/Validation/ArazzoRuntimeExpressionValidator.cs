using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static partial class ArazzoRuntimeExpressionValidator
{
    private const string TokenPattern = @"[!#$%&'*+\-.^_`|~0-9A-Za-z]+";
    private const string NamePattern = @"[^\x00\r\n]+";
    private const string JsonPointerPattern = @"(?:/(?:[^~/]|~[01])*)*";
    private const string HeaderReferencePattern = @"headers?\." + TokenPattern;
    private const string BodyReferencePattern = @"body(?:#" + JsonPointerPattern + ")?";
    private const string V1SourcePattern = @"(?:" + HeaderReferencePattern + @"|query\." + NamePattern + @"|path\." + NamePattern + @"|" + BodyReferencePattern + ")";
    private const string V1RuntimeExpressionPattern =
        @"^\$(?:url|method|statusCode|request\." + V1SourcePattern + @"|response\." + V1SourcePattern + @"|inputs\." + NamePattern +
        @"|outputs\." + NamePattern + @"|steps\." + NamePattern + @"|workflows\." + NamePattern + @"|sourceDescriptions\." + NamePattern +
        @"|components\.parameters\." + NamePattern + @"|components\." + NamePattern + @")$";
    private const string V1_1PayloadReferencePattern = @"payload(?:#" + JsonPointerPattern + @")?(?:\." + NamePattern + @")?";
    private const string V1_1SourcePattern = @"(?:" + HeaderReferencePattern + @"|query\." + NamePattern + @"|path\." + NamePattern + @"|" + BodyReferencePattern + @"|" + V1_1PayloadReferencePattern + ")";
    private const string V1_1RuntimeExpressionPattern =
        @"^\$(?:url|method|statusCode|request\." + V1_1SourcePattern + @"|response\." + V1_1SourcePattern + @"|inputs\." + NamePattern +
        @"|outputs\." + NamePattern + @"|steps\." + NamePattern + @"|workflows\." + NamePattern + @"\.(?:inputs|outputs)\." + NamePattern +
        @"|sourceDescriptions\." + NamePattern + @"|components\.parameters\." + NamePattern + @"|components\." + NamePattern + @"|message\." + V1_1SourcePattern + @"|self)$";

    [GeneratedRegex(V1RuntimeExpressionPattern, RegexOptions.CultureInvariant)]
    private static partial Regex V1RuntimeExpressionRegex();

    [GeneratedRegex(V1_1RuntimeExpressionPattern, RegexOptions.CultureInvariant)]
    private static partial Regex V1_1RuntimeExpressionRegex();

    [GeneratedRegex(@"\{(\$[^{}]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex BracedRuntimeExpressionRegex();

    /// <summary>
    /// Determines whether the supplied value matches the Arazzo runtime-expression ABNF translated to a regular expression.
    /// See <see href="https://spec.openapis.org/arazzo/v1.0.1.html#runtime-expressions">Runtime Expressions</see>.
    /// </summary>
    /// <param name="expression">The runtime expression to validate.</param>
    /// <param name="specVersion">The Arazzo specification version whose runtime-expression grammar applies.</param>
    /// <returns><see langword="true"/> when the value matches the runtime-expression grammar; otherwise, <see langword="false"/>.</returns>
    internal static bool IsRuntimeExpression(string? expression, ArazzoSpecVersion specVersion)
    {
        return !string.IsNullOrEmpty(expression) && GetRuntimeExpressionRegex(specVersion).IsMatch(expression);
    }

    internal static void ValidateSerializationExpressions(IEnumerable<KeyValuePair<string, string>>? expressions, string collectionName, ArazzoSpecVersion specVersion)
    {
        if (expressions is null)
        {
            return;
        }

        foreach (var (key, value) in expressions)
        {
            if (!IsRuntimeExpression(value, specVersion))
            {
                throw new ArazzoSerializationException($"Values in {collectionName} must be valid runtime expressions. Invalid value for key '{key}': '{value}'.");
            }
        }
    }

    internal static void ValidateSerializationExpression(string? expression, string elementName, ArazzoSpecVersion specVersion)
    {
        if (!string.IsNullOrEmpty(expression) && !IsRuntimeExpression(expression, specVersion))
        {
            throw new ArazzoSerializationException($"{elementName} must be a valid runtime expression. Invalid value: '{expression}'.");
        }
    }

    internal static void ValidateSerializationExpressionStrings(JsonNode? node, string elementName, ArazzoSpecVersion specVersion)
    {
        foreach (var error in ValidateExpressionStrings(node, elementName, specVersion))
        {
            throw new ArazzoSerializationException(error);
        }
    }

    internal static void ValidateDeserializationExpressions(IEnumerable<KeyValuePair<string, string>>? expressions, ParsingContext context, string collectionName)
    {
        if (expressions is null)
        {
            return;
        }

        foreach (var (key, value) in expressions)
        {
            if (!IsRuntimeExpression(value, context.Diagnostic.SpecificationVersion))
            {
                context.Diagnostic.Errors.Add(new OpenApiError($"{context.GetLocation()}/{EscapePointerSegment(key)}", $"Values in {collectionName} must be valid runtime expressions. Invalid value for key '{key}': '{value}'."));
            }
        }
    }

    internal static void ValidateDeserializationExpression(string? expression, ParsingContext context, string elementName)
    {
        if (!string.IsNullOrEmpty(expression) && !IsRuntimeExpression(expression, context.Diagnostic.SpecificationVersion))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{elementName} must be a valid runtime expression. Invalid value: '{expression}'."));
        }
    }

    internal static void ValidateDeserializationExpressionStrings(JsonNode? node, ParsingContext context, string elementName)
    {
        foreach (var error in ValidateExpressionStrings(node, elementName, context.Diagnostic.SpecificationVersion))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), error));
        }
    }

    private static IEnumerable<string> ValidateExpressionStrings(JsonNode? node, string elementName, ArazzoSpecVersion specVersion)
    {
        if (node is null)
        {
            yield break;
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var stringValue))
        {
            foreach (var error in ValidateExpressionString(stringValue, elementName, specVersion))
            {
                yield return error;
            }
            yield break;
        }

        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                foreach (var error in ValidateExpressionStrings(item, elementName, specVersion))
                {
                    yield return error;
                }
            }
            yield break;
        }

        if (node is JsonObject jsonObject)
        {
            foreach (var item in jsonObject.Select(static property => property.Value))
            {
                foreach (var error in ValidateExpressionStrings(item, elementName, specVersion))
                {
                    yield return error;
                }
            }
        }
    }

    private static IEnumerable<string> ValidateExpressionString(string value, string elementName, ArazzoSpecVersion specVersion)
    {
        if (value.StartsWith("$", StringComparison.Ordinal) && !IsRuntimeExpression(value, specVersion))
        {
            yield return $"{elementName} contains an invalid runtime expression: '{value}'.";
        }

        foreach (Match match in BracedRuntimeExpressionRegex().Matches(value))
        {
            var expression = match.Groups[1].Value;
            if (!IsRuntimeExpression(expression, specVersion))
            {
                yield return $"{elementName} contains an invalid runtime expression: '{expression}'.";
            }
        }
    }

    private static string EscapePointerSegment(string segment)
    {
        return segment.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
    }

    private static Regex GetRuntimeExpressionRegex(ArazzoSpecVersion specVersion) =>
        specVersion is ArazzoSpecVersion.Arazzo1_0 ? V1RuntimeExpressionRegex() : V1_1RuntimeExpressionRegex();
}