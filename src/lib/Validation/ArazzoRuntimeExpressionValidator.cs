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

    private static string EscapePointerSegment(string segment)
    {
        return segment.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
    }
}