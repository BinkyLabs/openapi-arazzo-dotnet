using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents an OpenAPI Arazzo element that comes with serialization functionality.
/// </summary>
public interface IArazzoSerializable
{
    /// <summary>
    /// Serializes the object to the OpenAPI Arazzo v1 format.
    /// </summary>
    /// <param name="writer">A Microsoft.OpenAPI writer</param>
    void SerializeAsV1(IOpenApiWriter writer);
}