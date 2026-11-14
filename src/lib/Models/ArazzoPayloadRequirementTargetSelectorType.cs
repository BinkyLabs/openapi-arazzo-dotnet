using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Specifies the selector language used to resolve a payload replacement target.
/// </summary>
public enum ArazzoPayloadRequirementTargetSelectorType
{
    /// <summary>
    /// JSONPath selector.
    /// </summary>
    [Display("jsonpath")]
    JsonPath = 0,

    /// <summary>
    /// XPath selector.
    /// </summary>
    [Display("xpath")]
    XPath = 1,

    /// <summary>
    /// JSON Pointer selector.
    /// </summary>
    [Display("jsonpointer")]
    JsonPointer = 2
}