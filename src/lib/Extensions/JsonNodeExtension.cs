
// Licensed under the MIT license.

using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// A wrapper class for JsonNode
/// </summary>
public class JsonNodeExtension : IOpenApiElement, IArazzoExtension
{
    private readonly JsonNode jsonNode;

    /// <summary>
    /// Initializes the <see cref="JsonNodeExtension"/> class.
    /// </summary>
    /// <param name="jsonNode"></param>
    public JsonNodeExtension(JsonNode jsonNode)
    {
        this.jsonNode = jsonNode;
    }

    /// <summary>
    /// Gets the underlying JsonNode.
    /// </summary>
    public JsonNode Node { get { return jsonNode; } }

    /// <inheritdoc/>
    public void Write(IOpenApiWriter writer, ArazzoSpecVersion specVersion)
    {
        writer.WriteAny(Node);
    }
}