using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a parameter definition.
/// </summary>
public interface IArazzoParameter : IArazzoSerializable, IArazzoReferenceable
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
    /// Gets the parameter value as a literal JSON value, runtime expression, or selector.
    /// </summary>
    ArazzoParameterValueUnion? Value { get; }
}