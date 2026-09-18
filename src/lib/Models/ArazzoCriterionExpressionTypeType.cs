using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the type of a criterion expression.
/// </summary>
public enum ArazzoCriterionExpressionTypeType
{
    /// <summary>
    /// Simple criterion expression type.
    /// </summary>
    [Display("simple")]
    Simple = 0,
    /// <summary>
    /// Regular expression criterion expression type.
    /// </summary>
    [Display("regex")]
    Regex = 1,
    /// <summary>
    /// JSONPath criterion expression type.
    /// </summary>
    [Display("jsonpath")]
    JsonPath = 2,

    /// <summary>
    /// XPath criterion expression type.
    /// </summary>
    [Display("xpath")]
    XPath = 3,

    /// <summary>
    /// JSON Pointer criterion expression type.
    /// </summary>
    [Display("jsonpointer")]
    JsonPointer = 4,
}