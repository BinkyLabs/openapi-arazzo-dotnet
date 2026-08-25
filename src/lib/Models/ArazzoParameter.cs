using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;
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
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <summary>
    /// Serializes the parameter as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1, static (w, obj) => obj.SerializeAsV1_1(w));
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(Name);
        ArgumentNullException.ThrowIfNull(Value);
        ArazzoRuntimeExpressionValidator.ValidateSerializationExpressionStrings(Value, $"{nameof(ArazzoParameter)}.{nameof(Value)}");

        writer.WriteStartObject();
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoParameterName, Name);
        if (In.HasValue)
        {
            writer.WriteRequiredProperty(ArazzoConstants.ArazzoParameterIn, In.Value.GetDisplayName());
        }
        writer.WriteOptionalObject(ArazzoConstants.ArazzoParameterValue, Value, static (w, v) => w.WriteAny(v));
        writer.WriteArazzoExtensions(Extensions, specVersion);
        writer.WriteEndObject();
    }
}