using System.Text.RegularExpressions;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static partial class ArazzoSelectorSyntaxValidator
{
    [GeneratedRegex(@"\A(?:/(?:[^~/]|~[01])*)*\z", RegexOptions.CultureInvariant)]
    private static partial Regex JsonPointerRegex();

    internal static bool IsJsonPointer(ArazzoPayloadRequirementTargetSelectorType? type, ArazzoCriterionExpressionType? expressionType) =>
        type is ArazzoPayloadRequirementTargetSelectorType.JsonPointer ||
        expressionType?.Type is ArazzoCriterionExpressionTypeType.JsonPointer;

    internal static string? GetError(string? expression, bool isJsonPointer, string elementName) =>
        expression is not null && isJsonPointer && !JsonPointerRegex().IsMatch(expression)
            ? $"{elementName} '{expression}' must be a valid RFC 6901 JSON Pointer."
            : null;

    internal static IEnumerable<string> ValidateReplacements(ArazzoRequestBody body, ArazzoSpecVersion version)
    {
        var defaultPointer = version is ArazzoSpecVersion.Arazzo1_0 ||
            body.ContentType?.Split(';', 2)[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase) == true;
        foreach (var replacement in body.Replacements ?? [])
        {
            var type = replacement.TargetSelectorType;
            if (GetError(replacement.Target,
                type is null ? defaultPointer : IsJsonPointer(type.Type, type.ExpressionType),
                $"{nameof(ArazzoPayloadReplacement)}.{nameof(ArazzoPayloadReplacement.Target)}") is { } error)
                yield return error;
        }
    }
}