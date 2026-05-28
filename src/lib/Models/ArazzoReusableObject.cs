using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a reusable object definition.
/// </summary>
public class ArazzoReusableObject : IArazzoSerializable
{
    /// <summary>
    /// Gets or sets a Runtime Expression used to reference the desired object.
    /// </summary>
    public string? Reference { get; set; }

    /// <summary>
    /// Gets or sets a value of the referenced parameter. This is only applicable for parameter object references.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Serializes the reusable object as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrEmpty(Reference);

        writer.WriteStartObject();
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoReusableObjectReference, Reference);

        if (Value is not null)
        {
            writer.WriteProperty(ArazzoConstants.ArazzoReusableObjectValue, Value);
        }

        writer.WriteEndObject();
    }
}