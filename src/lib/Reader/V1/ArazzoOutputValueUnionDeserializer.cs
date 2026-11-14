using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static ArazzoOutputValueUnion LoadOutputValueUnion(JsonNode node, ParsingContext context)
    {
        if (node is JsonObject map && map.ContainsKey(ArazzoConstants.ArazzoSelectorContext))
        {
            if (context.Diagnostic.SpecificationVersion is ArazzoSpecVersion.Arazzo1_0)
                throw new ArazzoReaderException("Selector output values are not supported in Arazzo 1.0.", context);
            return new() { Selector = V1_1.ArazzoV1_1Deserializer.LoadSelectorInternal(node, context) };
        }
        if (node is JsonValue value && value.TryGetValue<string>(out var expression) &&
            ArazzoRuntimeExpressionValidator.IsRuntimeExpression(expression, context.Diagnostic.SpecificationVersion))
            return new() { Expression = new ArazzoRuntimeExpression(expression) };
        throw new ArazzoReaderException("Output values must be valid runtime expressions or Selector Objects.", context);
    }

    private static Dictionary<string, ArazzoOutputValueUnion> LoadOutputMap(JsonNode node, ParsingContext context)
    {
        var map = node.CheckMapNode("Outputs", context);
        var outputs = new Dictionary<string, ArazzoOutputValueUnion>(StringComparer.Ordinal);
        foreach (var (key, value) in map)
        {
            context.StartObject(key);
            try
            {
                if (value is null) throw new ArazzoReaderException("Output values must not be null.", context);
                outputs.Add(key, LoadOutputValueUnion(value, context));
            }
            catch (ArazzoReaderException error)
            {
                context.Diagnostic.Errors.Add(new OpenApiError(error.Pointer, error.Message));
            }
            finally
            {
                context.EndObject();
            }
        }
        return outputs;
    }
}