using System.Text;

using Json.Path;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoExpressionSyntaxValidator
{
    internal static string? GetError(string? expression, ArazzoCriterionExpressionTypeType? type,
        ArazzoCriterionExpressionType? expressionType, string elementName, ArazzoSpecVersion specVersion,
        bool allowRuntimeExpressions = false)
    {
        if (string.IsNullOrEmpty(expression))
            return null;
        if (expressionType is { Type: { } objectType, Version: { } objectVersion } &&
            ArazzoCriterionExpressionType.GetVersionValidationError(objectType, objectVersion) is not null)
            return null;
        var language = expressionType?.Type ?? type;
        // XPath grammar validation is deferred for all versions; do not treat XPath 1.0 as a 3.1 parser.
        if (language is not ArazzoCriterionExpressionTypeType.JsonPath)
            return null;
        // JsonPath.Net implements RFC 9535, not the legacy draft's implementation-defined scripting.
        if (expressionType?.Version is ArazzoCriterionExpressionVersion.DraftGoessnerDispatchJsonPath00)
            return null;
        var source = allowRuntimeExpressions ? MaskRuntimeExpressions(expression, specVersion) : expression;
        return source is null || JsonPath.TryParse(source, out _) ? null : $"{elementName} '{expression}' must be a valid RFC 9535 JSONPath.";
    }

    private static string? MaskRuntimeExpressions(string expression, ArazzoSpecVersion specVersion)
    {
        if (!expression.Contains("{$", StringComparison.Ordinal))
            return expression;
        var result = new StringBuilder(expression.Length);
        var quote = '\0';
        for (var index = 0; index < expression.Length; index++)
        {
            var character = expression[index];
            if (character == '{' && index + 1 < expression.Length && expression[index + 1] == '$')
            {
                var end = expression.IndexOf('}', index + 2);
                if (end >= 0 && ArazzoRuntimeExpressionValidator.IsRuntimeExpression(expression[(index + 1)..end], specVersion))
                {
                    // Unquoted substitutions can supply arbitrary query syntax; validate after evaluation.
                    if (quote == '\0')
                        return null;
                    result.Append('0');
                    index = end;
                    continue;
                }
            }
            result.Append(character);
            if (quote != '\0' && character == '\\' && index + 1 < expression.Length)
            {
                result.Append(expression[++index]);
            }
            else if (character == quote)
            {
                quote = '\0';
            }
            else if (quote == '\0' && character is '\'' or '"')
            {
                quote = character;
            }
        }
        return result.ToString();
    }
}