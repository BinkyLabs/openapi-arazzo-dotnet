using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoRequestBody> RequestBodyFixedFields = new()
    {
        { ArazzoConstants.ArazzoRequestBodyContentType, static (o, v, c) => o.ContentType = v.GetScalarValue() },
        { ArazzoConstants.ArazzoRequestBodyPayload, static (o, v, c) => o.Payload = v },
        { ArazzoConstants.ArazzoRequestBodyReplacements, static (o, v, c) => o.Replacements = v.CreateList(LoadPayloadReplacement, c) }
    };

    public static readonly PatternFieldMap<ArazzoRequestBody> RequestBodyPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static ArazzoRequestBody LoadRequestBody(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("RequestBody", context);
        var requestBody = new ArazzoRequestBody();
        mapNode.ParseMap(requestBody, RequestBodyFixedFields, RequestBodyPatternFields, context);

        return requestBody;
    }
}