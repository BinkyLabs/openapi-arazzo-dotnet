using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the Source Description object in the OpenAPI Arazzo specification.
/// </summary>
public class ArazzoSourceDescription : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the name of the source description.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the URL of the source description.
    /// </summary>
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets or sets the type of the source description.
    /// </summary>
    public ArazzoDescriptionType? Type { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the source description object as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrEmpty(Name);
        ArgumentNullException.ThrowIfNull(Url);
        writer.WriteStartObject();
        writer.WriteProperty(ArazzoConstants.ArazzoSourceDescriptionName, Name);
        writer.WriteProperty(ArazzoConstants.ArazzoSourceDescriptionUrl, Url?.ToString());
        if (Type.HasValue)
        {
            writer.WriteProperty(ArazzoConstants.ArazzoSourceDescriptionType, Type.Value.GetDisplayName());
        }
        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}