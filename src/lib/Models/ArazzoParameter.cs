using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;
/// <summary>
/// Represents a parameter definition.
/// </summary>
public class ArazzoParameter : IArazzoParameter, IArazzoExtensible
{
    /// <inheritdoc/>
    public string? Name { get; set; }

    /// <inheritdoc/>
    public ParameterLocation? In { get; set; }

    /// <inheritdoc/>
    public JsonNode? Value { get; set; }

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

        ArgumentNullException.ThrowIfNull(Value);

        writer.WriteStartObject();
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoParameterName, Name);
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoParameterIn, In.Value.GetDisplayName());
        writer.WriteOptionalObject(ArazzoConstants.ArazzoParameterValue, Value, static (w, v) => w.WriteAny(v));
        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}