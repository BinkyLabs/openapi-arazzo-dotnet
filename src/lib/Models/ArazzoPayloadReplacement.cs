using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;
/// <summary>
/// Represents a payload replacement definition.
/// </summary>
public class ArazzoPayloadReplacement : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the target path within the payload to replace.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the replacement value.
    /// </summary>
    public JsonNode? Value { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the payload replacement as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(Target);
        ArgumentNullException.ThrowIfNull(Value);

        writer.WriteStartObject();
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoPayloadReplacementTarget, Target);
        writer.WriteOptionalObject(ArazzoConstants.ArazzoPayloadReplacementValue, Value, static (w, v) => w.WriteAny(v));
        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}