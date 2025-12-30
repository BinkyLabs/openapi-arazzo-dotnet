using System;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoCriterion> CriterionFixedFields = new()
    {
        { ArazzoConstants.ArazzoCriterionContext, (o, v) => o.Context = v.GetScalarValue() },
        { ArazzoConstants.ArazzoCriterionCondition, (o, v) => o.Condition = v.GetScalarValue() },
        { ArazzoConstants.ArazzoCriterionType, (o, v) => {
            if (v is ValueNode valueNode)
            {
                // Type is a string (Simple or Regex)
                var typeString = valueNode.GetScalarValue();
                if (typeString != null && Enum.TryParse<ArazzoCriterionExpressionTypeType>(typeString, true, out var typeValue))
                {
                    o.Type = new ArazzoCriterionExpressionType
                    {
                        Type = typeValue,
                        Version = null
                    };
                }
            }
            else if (v is MapNode)
            {
                // Type is an object (JsonPath or XPath)
                o.Type = LoadCriterionExpressionType(v);
            }
        }}
    };

    public static readonly PatternFieldMap<ArazzoCriterion> CriterionPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k, LoadExtension(k, n)) }
    };

    public static ArazzoCriterion LoadCriterion(ParseNode node)
    {
        var mapNode = node.CheckMapNode("Criterion");
        var criterion = new ArazzoCriterion();
        ParseMap(mapNode, criterion, CriterionFixedFields, CriterionPatternFields);

        return criterion;
    }
}
