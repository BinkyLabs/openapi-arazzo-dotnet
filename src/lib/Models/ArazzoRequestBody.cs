using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;
using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;
/// <summary>
/// Represents a request body definition.
/// </summary>
public class ArazzoRequestBody : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the content type of the request body.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the payload for the request body.
    /// </summary>
    public JsonNode? Payload { get; set; }

    /// <summary>
    /// Gets or sets the list of payload replacements to apply.
    /// </summary>
    public List<ArazzoPayloadReplacement>? Replacements { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the request body as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArazzoRuntimeExpressionValidator.ValidateSerializationExpressionStrings(Payload, $"{nameof(ArazzoRequestBody)}.{nameof(Payload)}");

        writer.WriteStartObject();
        writer.WriteProperty(ArazzoConstants.ArazzoRequestBodyContentType, ContentType);
        writer.WriteOptionalObject(ArazzoConstants.ArazzoRequestBodyPayload, Payload, static (w, v) => w.WriteAny(v));

        writer.WriteOptionalCollection(ArazzoConstants.ArazzoRequestBodyReplacements, Replacements, static (w, r) => r?.SerializeAsV1(w));

        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}