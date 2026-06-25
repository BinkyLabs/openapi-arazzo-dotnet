using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoCriterionExpressionType> CriterionExpressionTypeFixedFields = new()
    {
        { ArazzoConstants.ArazzoCriterionExpressionTypeType, static (o, v, c) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoCriterionExpressionTypeType>(c, out var type))
            {
                return;
            }
            o.Type = type;
        } },
        { ArazzoConstants.ArazzoCriterionExpressionTypeVersion, static (o, v, c) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoCriterionExpressionVersion>(c, out var version))
            {
                return;
            }
            o.Version = version;
        } }
    };

    public static readonly PatternFieldMap<ArazzoCriterionExpressionType> CriterionExpressionTypePatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static ArazzoCriterionExpressionType LoadCriterionExpressionType(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("CriterionExpressionType", context);
        var expressionType = new ArazzoCriterionExpressionType();
        mapNode.ParseMap(expressionType, CriterionExpressionTypeFixedFields, CriterionExpressionTypePatternFields, context);
        ValidateCriterionExpressionTypeRequiredFields(expressionType, context);

        // Validate that Simple and Regex types are not deserialized as they are not supported by the specification
        if (expressionType.Type == ArazzoCriterionExpressionTypeType.Simple || expressionType.Type == ArazzoCriterionExpressionTypeType.Regex)
        {
            context.Diagnostic.Errors.Add(new Microsoft.OpenApi.OpenApiError(context.GetLocation(),
                $"Deserializing criterion expression type '{expressionType.Type?.GetDisplayName()}' as an object is NOT supported by the specification."));
        }

        return expressionType;
    }

    private static void ValidateCriterionExpressionTypeRequiredFields(ArazzoCriterionExpressionType expressionType, ParsingContext context)
    {
        if (!expressionType.Type.HasValue)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoCriterionExpressionType)}.{nameof(ArazzoCriterionExpressionType.Type)} is a REQUIRED field."));
        }

        if (!expressionType.Version.HasValue)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoCriterionExpressionType)}.{nameof(ArazzoCriterionExpressionType.Version)} is a REQUIRED field."));
        }
    }
}