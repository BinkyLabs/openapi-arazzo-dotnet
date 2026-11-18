using Json.Pointer;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoSelectorSyntaxValidator
{
    internal static bool IsJsonPointer(ArazzoPayloadRequirementTargetSelectorType? type, ArazzoCriterionExpressionType? expressionType) =>
        type is ArazzoPayloadRequirementTargetSelectorType.JsonPointer ||
        expressionType?.Type is ArazzoCriterionExpressionTypeType.JsonPointer;

    internal static string? GetError(string? expression, bool isJsonPointer, string elementName) =>
        expression is not null && isJsonPointer &&
        (expression.Length > 0 && expression[0] != '/' || !JsonPointer.TryParse(expression, out _))
            ? $"{elementName} '{expression}' must be a valid RFC 6901 JSON Pointer."
            : null;

    internal static string? GetError(string? expression, ArazzoPayloadRequirementTargetSelectorType? type,
        ArazzoCriterionExpressionType? expressionType, string elementName, ArazzoSpecVersion specVersion) =>
        GetError(expression, IsJsonPointer(type, expressionType), elementName) ??
        ArazzoExpressionSyntaxValidator.GetError(expression, type switch
        {
            ArazzoPayloadRequirementTargetSelectorType.JsonPath => ArazzoCriterionExpressionTypeType.JsonPath,
            ArazzoPayloadRequirementTargetSelectorType.XPath => ArazzoCriterionExpressionTypeType.XPath,
            _ => null
        }, expressionType, elementName, specVersion);

    internal static IEnumerable<string> ValidateReplacements(ArazzoRequestBody body, ArazzoSpecVersion version)
    {
        var defaultPointer = version is ArazzoSpecVersion.Arazzo1_0 ||
            body.ContentType?.Split(';', 2)[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase) == true;
        foreach (var replacement in (body.Replacements ?? []).Where(static replacement => replacement.TargetSelectorType is null))
        {
            if (GetError(replacement.Target,
                defaultPointer, $"{nameof(ArazzoPayloadReplacement)}.{nameof(ArazzoPayloadReplacement.Target)}") is { } error)
                yield return error;
        }
    }
}