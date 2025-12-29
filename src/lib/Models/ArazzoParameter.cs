using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;
/// <summary>
/// Represents a reusable parameter definition.
/// </summary>
public class ArazzoParameter : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the location of the parameter.
    /// </summary>
    public ParameterLocation? In { get; set; }

    /// <summary>
    /// Gets or sets the parameter value.
    /// </summary>
    public string? Value { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the parameter as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(Name);
        if (!In.HasValue)
        {
            throw new ArgumentNullException(nameof(In));
        }

        ArgumentException.ThrowIfNullOrEmpty(Value);

        writer.WriteStartObject();
        writer.WriteRequiredProperty("name", Name);
        writer.WriteRequiredProperty("in", In.Value.GetDisplayName());
        writer.WriteRequiredProperty("value", Value);
        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}
