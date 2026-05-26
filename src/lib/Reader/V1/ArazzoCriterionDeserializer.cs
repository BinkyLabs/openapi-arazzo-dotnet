using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoCriterion> CriterionFixedFields = new()
    {
        { ArazzoConstants.ArazzoCriterionContext, static (o, v, c) => o.Context = v.GetScalarValue() },
        { ArazzoConstants.ArazzoCriterionCondition, static (o, v, c) => o.Condition = v.GetScalarValue() },
        { ArazzoConstants.ArazzoCriterionType, static (o, v, c) => {
            if (v is JsonValue && v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoCriterionExpressionTypeType>(c, out var typeValue))
            {
                // Type is a string (Simple or Regex)
                o.Type = new ArazzoCriterionExpressionType
                {
                    Type = typeValue,
                    Version = null
                };
            }
            else if (v is JsonObject)
            {
                // Type is an object (JsonPath or XPath)
                o.Type = LoadCriterionExpressionType(v, c);
            }
        }}
    };

    public static readonly PatternFieldMap<ArazzoCriterion> CriterionPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static ArazzoCriterion LoadCriterion(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("Criterion", context);
        var criterion = new ArazzoCriterion();
        mapNode.ParseMap(criterion, CriterionFixedFields, CriterionPatternFields, context);

        return criterion;
    }
}