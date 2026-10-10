using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoRequestBody> RequestBodyFixedFields = new()
    {
        { ArazzoConstants.ArazzoRequestBodyContentType, static (o, v, c) => o.ContentType = v.GetScalarValue() },
        { ArazzoConstants.ArazzoRequestBodyPayload, static (o, v, c) => o.Payload = v },
        { ArazzoConstants.ArazzoRequestBodyReplacements, static (o, v, c) => o.Replacements = v.CreateList(LoadPayloadReplacement, c) }
    };

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
        ArazzoRuntimeExpressionValidator.ValidateDeserializationExpressionStrings(requestBody.Payload, context, $"{nameof(ArazzoRequestBody)}.{nameof(ArazzoRequestBody.Payload)}");

        return requestBody;
    }
}