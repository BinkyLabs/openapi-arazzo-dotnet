using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static ArazzoPayloadReplacementTargetSelectorTypeUnion LoadPayloadReplacementTargetSelectorType(JsonNode node, ParsingContext context)
    {
        if (node is JsonObject jsonObject && jsonObject.ContainsKey(ArazzoConstants.ArazzoCriterionExpressionTypeType))
        {
            return new() { ExpressionType = LoadCriterionExpressionType(node, context) };
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var selectorType))
        {
            if (!selectorType.TryGetEnumFromDisplayName<ArazzoPayloadRequirementTargetSelectorType>(out var type))
            {
                throw new ArazzoReaderException($"Enum value {selectorType} is not recognized.", context);
            }

            return new() { Type = type };
        }

        throw new ArazzoReaderException("A payload replacement target selector type must be jsonpath, xpath, jsonpointer, or an Expression Type Object with a type property.", context);
    }
}