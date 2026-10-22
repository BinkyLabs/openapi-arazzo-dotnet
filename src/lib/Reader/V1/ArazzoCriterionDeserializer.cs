using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoCriterion> CriterionFixedFields = new()
    {
        { ArazzoConstants.ArazzoCriterionContext, static (o, v, c) => o.Context = v.GetScalarValue() },
        { ArazzoConstants.ArazzoCriterionCondition, static (o, v, c) => o.Condition = v.GetScalarValue() },
        { ArazzoConstants.ArazzoCriterionType, static (o, v, c) => o.Type = LoadCriterionTypeUnion(v, c) }
    };

    public static ArazzoCriterionTypeUnion LoadCriterionTypeUnion(JsonNode node, ParsingContext context)
    {
        if (node is JsonObject map && map.ContainsKey(ArazzoConstants.ArazzoCriterionExpressionTypeType))
            return new() { ExpressionType = LoadCriterionExpressionType(node, context) };
        if (node is JsonValue value && value.TryGetValue<string>(out var text) &&
            text.TryGetEnumFromDisplayName<ArazzoCriterionExpressionTypeType>(out var type) &&
            type is not ArazzoCriterionExpressionTypeType.JsonPointer)
            return new() { Type = type };
        throw new ArazzoReaderException("Criterion type must be simple, regex, jsonpath, xpath, or an Expression Type Object.", context);
    }

    public static PatternFieldMap<ArazzoCriterion> GetCriterionPatternFields() =>
    new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };
    public static readonly PatternFieldMap<ArazzoCriterion> CriterionPatternFields = GetCriterionPatternFields();

    public static ArazzoCriterion LoadCriterion(JsonNode node, ParsingContext context)
    {
        return LoadCriterionInternal(node, context, CriterionFixedFields, CriterionPatternFields);
    }

    public static ArazzoCriterion LoadCriterionInternal(JsonNode node, ParsingContext context, FixedFieldMap<ArazzoCriterion> criterionFixedFields, PatternFieldMap<ArazzoCriterion> criterionPatternFields)
    {
        var mapNode = node.CheckMapNode("Criterion", context);
        var criterion = new ArazzoCriterion();
        mapNode.ParseMap(criterion, criterionFixedFields, criterionPatternFields, context);
        ArazzoCriterionValidator.ValidateDeserialization(criterion, context);
        ArazzoRuntimeExpressionValidator.ValidateDeserializationExpression(criterion.Context, context, $"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Context)}");

        return criterion;
    }
}