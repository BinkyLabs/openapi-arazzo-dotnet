using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a parameter definition.
/// </summary>
public interface IArazzoParameter : IArazzoSerializable, IArazzoExtensible, IArazzoReferenceable
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    string? Name { get; }
    /// <summary>
    /// Gets or sets the location of the parameter.
    /// </summary>
    ParameterLocation? In { get; }
    /// <summary>
    /// Gets or sets the parameter value.
    /// </summary>
    JsonNode? Value { get; }
}