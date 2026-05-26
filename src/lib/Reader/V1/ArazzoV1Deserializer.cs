using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    private static IArazzoExtension LoadExtension(string name, JsonNode node, ParsingContext context)
    {
        if (context.ExtensionParsers is not null && context.ExtensionParsers.TryGetValue(name, out var parser) && parser(
            node, ArazzoSpecVersion.Arazzo1_0) is { } result)
        {
            return result;
        }
        else
        {
            return new JsonNodeExtension(node);
        }
    }
}