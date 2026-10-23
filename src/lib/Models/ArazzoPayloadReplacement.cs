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
    /// Gets or sets the selector type used to resolve the target.
    /// </summary>
    public ArazzoPayloadReplacementTargetSelectorTypeUnion? TargetSelectorType { get; set; }

    /// <summary>
    /// Gets or sets the replacement value.
    /// </summary>
    public ArazzoPayloadReplacementValueUnion? Value { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the payload replacement as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <summary>
    /// Serializes the payload replacement as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1, static (w, obj) => obj.SerializeAsV1_1(w));
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(Target);
        ArgumentNullException.ThrowIfNull(Value);

        writer.WriteStartObject();
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoPayloadReplacementTarget, Target);
        writer.WriteOptionalObject(
            specVersion is ArazzoSpecVersion.Arazzo1_0
                ? ArazzoConstants.GetArazzo1_0ExtensionName(ArazzoConstants.ArazzoPayloadReplacementTargetSelectorType)
                : ArazzoConstants.ArazzoPayloadReplacementTargetSelectorType,
            TargetSelectorType,
            callback);
        writer.WriteOptionalObject(ArazzoConstants.ArazzoPayloadReplacementValue, Value, callback);
        writer.WriteArazzoExtensions(Extensions, specVersion);
        writer.WriteEndObject();
    }
}