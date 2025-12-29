using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;
/// <summary>
/// Represents the Info object in the OpenAPI Arazzo specification.
/// </summary>
public class ArazzoInfo : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the title of the Arazzo.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the version of the Arazzo.
    /// </summary>
    public string? Version { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the info object as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteProperty(ArazzoConstants.ArazzoInfoTitle, Title);
        writer.WriteProperty(ArazzoConstants.ArazzoInfoVersion, Version);
        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}