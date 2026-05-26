using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoPayloadReplacement> PayloadReplacementFixedFields = new()
    {
        { ArazzoConstants.ArazzoPayloadReplacementTarget, static (o, v, c) => o.Target = v.GetScalarValue() },
        { ArazzoConstants.ArazzoPayloadReplacementValue, static (o, v, c) => o.Value = v }
    };

    public static readonly PatternFieldMap<ArazzoPayloadReplacement> PayloadReplacementPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static ArazzoPayloadReplacement LoadPayloadReplacement(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("PayloadReplacement", context);
        var replacement = new ArazzoPayloadReplacement();
        mapNode.ParseMap(replacement, PayloadReplacementFixedFields, PayloadReplacementPatternFields, context);

        return replacement;
    }
}