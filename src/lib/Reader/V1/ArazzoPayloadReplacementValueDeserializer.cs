using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader.V1_1;
using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static ArazzoPayloadReplacementValue LoadPayloadReplacementValue(JsonNode node, ParsingContext context)
    {
        if (node is JsonObject jsonObject && jsonObject.ContainsKey(ArazzoConstants.ArazzoSelectorContext))
        {
            if (context.Diagnostic.SpecificationVersion is ArazzoSpecVersion.Arazzo1_0)
            {
                context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), "Selector payload replacement values are not supported in Arazzo 1.0."));
            }

            return new() { Selector = ArazzoV1_1Deserializer.LoadSelectorInternal(node, context) };
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var expression) &&
            ArazzoRuntimeExpressionValidator.IsRuntimeExpression(expression, context.Diagnostic.SpecificationVersion))
        {
            return new() { Expression = ArazzoRuntimeExpression.Build(expression) };
        }

        return new() { Any = node };
    }
}