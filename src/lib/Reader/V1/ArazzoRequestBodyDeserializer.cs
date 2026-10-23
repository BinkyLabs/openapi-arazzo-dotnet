using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoRequestBody> RequestBodyFixedFields = new()
    {
        { ArazzoConstants.ArazzoRequestBodyContentType, static (o, v, c) => o.ContentType = v.GetScalarValue() },
        { ArazzoConstants.ArazzoRequestBodyPayload, static (o, v, c) => o.Payload = LoadRequestBodyPayload(v, c) },
        { ArazzoConstants.ArazzoRequestBodyReplacements, static (o, v, c) => o.Replacements = v.CreateList(LoadPayloadReplacement, c) }
    };

    public static ArazzoRequestBodyPayloadUnion LoadRequestBodyPayload(JsonNode node, ParsingContext context)
    {
        if (node is JsonObject map && map.ContainsKey(ArazzoConstants.ArazzoSelectorContext) &&
            context.Diagnostic.SpecificationVersion is ArazzoSpecVersion.Arazzo1_0)
        {
            throw new ArazzoReaderException("Selector request body payloads are not supported in Arazzo 1.0.", context);
        }

        var value = LoadPayloadReplacementValue(node, context);
        ArazzoRuntimeExpressionValidator.ValidateDeserializationExpressionStrings(value.Any, context, $"{nameof(ArazzoRequestBody)}.{nameof(ArazzoRequestBody.Payload)}");
        return new() { Any = value.Any, Expression = value.Expression, Selector = value.Selector };
    }

    public static PatternFieldMap<ArazzoRequestBody> GetRequestBodyPatternFields() =>
    new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };
    public static readonly PatternFieldMap<ArazzoRequestBody> RequestBodyPatternFields = GetRequestBodyPatternFields();

    public static ArazzoRequestBody LoadRequestBody(JsonNode node, ParsingContext context)
    {
        return LoadRequestBodyInternal(node, context, RequestBodyFixedFields, RequestBodyPatternFields);
    }

    public static ArazzoRequestBody LoadRequestBodyInternal(JsonNode node, ParsingContext context, FixedFieldMap<ArazzoRequestBody> requestBodyFixedFields, PatternFieldMap<ArazzoRequestBody> requestBodyPatternFields)
    {
        var mapNode = node.CheckMapNode("RequestBody", context);
        var requestBody = new ArazzoRequestBody();
        mapNode.ParseMap(requestBody, requestBodyFixedFields, requestBodyPatternFields, context);
        if (mapNode.TryGetPropertyValue(ArazzoConstants.ArazzoRequestBodyPayload, out var payloadNode) && payloadNode is null)
        {
            requestBody.Payload = new() { Any = JsonNullSentinel.JsonNull };
        }

        return requestBody;
    }
}