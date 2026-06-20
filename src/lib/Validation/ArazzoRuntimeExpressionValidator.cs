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
    private const string SourcePattern = @"(?:" + HeaderReferencePattern + @"|query\." + NamePattern + @"|path\." + NamePattern + @"|" + BodyReferencePattern + ")";
    private const string RuntimeExpressionPattern =
        @"^\$(?:url|method|statusCode|request\." + SourcePattern + @"|response\." + SourcePattern + @"|inputs\." + NamePattern +
        @"|outputs\." + NamePattern + @"|steps\." + NamePattern + @"|workflows\." + NamePattern + @"|sourceDescriptions\." + NamePattern +
        @"|components\.parameters\." + NamePattern + @"|components\." + NamePattern + @")$";

    [GeneratedRegex(RuntimeExpressionPattern, RegexOptions.CultureInvariant)]
    private static partial Regex RuntimeExpressionRegex();

    [GeneratedRegex(@"\{(\$[^{}]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex BracedRuntimeExpressionRegex();

    /// <summary>
    /// Determines whether the supplied value matches the Arazzo runtime-expression ABNF translated to a regular expression.
    /// See <see href="https://spec.openapis.org/arazzo/v1.0.1.html#runtime-expressions">Runtime Expressions</see>.
    /// </summary>
    /// <param name="expression">The runtime expression to validate.</param>
    /// <returns><see langword="true"/> when the value matches the runtime-expression grammar; otherwise, <see langword="false"/>.</returns>
    internal static bool IsRuntimeExpression(string? expression)
    {
        return !string.IsNullOrEmpty(expression) && RuntimeExpressionRegex().IsMatch(expression);
    }

    internal static void ValidateSerializationExpressions(IEnumerable<KeyValuePair<string, string>>? expressions, string collectionName)
    {
        if (expressions is null)
        {
            return;
        }

        foreach (var (key, value) in expressions)
        {
            if (!IsRuntimeExpression(value))
            {
                throw new ArazzoSerializationException($"Values in {collectionName} must be valid runtime expressions. Invalid value for key '{key}': '{value}'.");
            }
        }
    }

    internal static void ValidateSerializationExpression(string? expression, string elementName)
    {
        if (!string.IsNullOrEmpty(expression) && !IsRuntimeExpression(expression))
        {
            throw new ArazzoSerializationException($"{elementName} must be a valid runtime expression. Invalid value: '{expression}'.");
        }
    }

    internal static void ValidateSerializationExpressionStrings(JsonNode? node, string elementName)
    {
        foreach (var error in ValidateExpressionStrings(node, elementName))
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
            if (!IsRuntimeExpression(value))
            {
                context.Diagnostic.Errors.Add(new OpenApiError($"{context.GetLocation()}/{EscapePointerSegment(key)}", $"Values in {collectionName} must be valid runtime expressions. Invalid value for key '{key}': '{value}'."));
            }
        }
    }

    internal static void ValidateDeserializationExpression(string? expression, ParsingContext context, string elementName)
    {
        if (!string.IsNullOrEmpty(expression) && !IsRuntimeExpression(expression))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{elementName} must be a valid runtime expression. Invalid value: '{expression}'."));
        }
    }

    internal static void ValidateDeserializationExpressionStrings(JsonNode? node, ParsingContext context, string elementName)
    {
        foreach (var error in ValidateExpressionStrings(node, elementName))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), error));
        }
    }

    private static IEnumerable<string> ValidateExpressionStrings(JsonNode? node, string elementName)
    {
        if (node is null)
        {
            yield break;
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var stringValue))
        {
            foreach (var error in ValidateExpressionString(stringValue, elementName))
            {
                yield return error;
            }
            yield break;
        }

        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                foreach (var error in ValidateExpressionStrings(item, elementName))
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
                foreach (var error in ValidateExpressionStrings(item, elementName))
                {
                    yield return error;
                }
            }
        }
    }

    private static IEnumerable<string> ValidateExpressionString(string value, string elementName)
    {
        if (value.StartsWith("$", StringComparison.Ordinal) && !IsRuntimeExpression(value))
        {
            yield return $"{elementName} contains an invalid runtime expression: '{value}'.";
        }

        foreach (Match match in BracedRuntimeExpressionRegex().Matches(value))
        {
            var expression = match.Groups[1].Value;
            if (!IsRuntimeExpression(expression))
            {
                yield return $"{elementName} contains an invalid runtime expression: '{expression}'.";
            }
        }
    }

    private static string EscapePointerSegment(string segment)
    {
        return segment.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
    }
}