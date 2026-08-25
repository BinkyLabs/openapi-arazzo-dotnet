using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the type of Arazzo source description.
/// </summary>
public enum ArazzoDescriptionType
{
    /// <summary>
    /// OpenAPI specification type.
    /// </summary>
    [Display("openapi")]
    OpenAPI = 0,

    /// <summary>
    /// Arazzo specification type.
    /// </summary>
    [Display("arazzo")]
    Arazzo = 1,

    /// <summary>
    /// AsyncAPI specification type.
    /// </summary>
    [Display("asyncapi")]
    AsyncAPI = 2
}