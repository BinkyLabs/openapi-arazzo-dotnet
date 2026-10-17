using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the version of a criterion expression.
/// </summary>
public enum ArazzoCriterionExpressionVersion
{
    /// <summary>
    /// draft-goessner-dispatch-jsonpath-00 version.
    /// </summary>
    [Display("draft-goessner-dispatch-jsonpath-00")]
    DraftGoessnerDispatchJsonPath00 = 0,

    /// <summary>
    /// XPath 3.0 version.
    /// </summary>
    [Display("xpath-30")]
    XPath30 = 1,

    /// <summary>
    /// XPath 2.0 version.
    /// </summary>
    [Display("xpath-20")]
    XPath20 = 2,

    /// <summary>
    /// XPath 1.0 version.
    /// </summary>
    [Display("xpath-10")]
    XPath10 = 3,

    /// <summary>
    /// RFC 9535 JSONPath version.
    /// </summary>
    [Display("rfc9535")]
    Rfc9535 = 4,

    /// <summary>
    /// XPath 3.1 version.
    /// </summary>
    [Display("xpath-31")]
    XPath31 = 5,

    /// <summary>
    /// RFC 6901 JSON Pointer version.
    /// </summary>
    [Display("rfc6901")]
    Rfc6901 = 6
}