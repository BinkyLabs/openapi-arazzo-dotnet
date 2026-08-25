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
            if (type is ArazzoCriterionExpressionTypeType.JsonPointer && c.Diagnostic.SpecificationVersion is ArazzoSpecVersion.Arazzo1_0)
            {
                c.Diagnostic.Errors.Add(new OpenApiError(c.GetLocation(), "The value 'jsonpointer' for 'type' is not supported in Arazzo 1.0."));
            }
            o.Type = type;
        } },
        { ArazzoConstants.ArazzoCriterionExpressionTypeVersion, static (o, v, c) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoCriterionExpressionVersion>(c, out var version))
            {
                return;
            }
            if (version is ArazzoCriterionExpressionVersion.Rfc9535 or ArazzoCriterionExpressionVersion.XPath31 or ArazzoCriterionExpressionVersion.Rfc6901
                && c.Diagnostic.SpecificationVersion is ArazzoSpecVersion.Arazzo1_0)
            {
                c.Diagnostic.Errors.Add(new OpenApiError(c.GetLocation(), $"The value '{version.GetDisplayName()}' for 'version' is not supported in Arazzo 1.0."));
            }
            o.Version = version;
        } }
    };

    public static PatternFieldMap<ArazzoCriterionExpressionType> GetCriterionExpressionTypePatternFields() =>
    new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };
    public static readonly PatternFieldMap<ArazzoCriterionExpressionType> CriterionExpressionTypePatternFields = GetCriterionExpressionTypePatternFields();

    public static ArazzoCriterionExpressionType LoadCriterionExpressionType(JsonNode node, ParsingContext context)
    {
        return LoadCriterionExpressionTypeInternal(node, context, CriterionExpressionTypeFixedFields, CriterionExpressionTypePatternFields);
    }

    public static ArazzoCriterionExpressionType LoadCriterionExpressionTypeInternal(JsonNode node, ParsingContext context, FixedFieldMap<ArazzoCriterionExpressionType> criterionExpressionTypeFixedFields, PatternFieldMap<ArazzoCriterionExpressionType> criterionExpressionTypePatternFields)
    {
        var mapNode = node.CheckMapNode("CriterionExpressionType", context);
        var expressionType = new ArazzoCriterionExpressionType();
        mapNode.ParseMap(expressionType, criterionExpressionTypeFixedFields, criterionExpressionTypePatternFields, context);
        ApplyDefaultVersion(expressionType, context);
        ValidateCriterionExpressionTypeRequiredFields(expressionType, context);

        // Validate that Simple and Regex types are not deserialized as they are not supported by the specification
        if (expressionType.Type == ArazzoCriterionExpressionTypeType.Simple || expressionType.Type == ArazzoCriterionExpressionTypeType.Regex)
        {
            context.Diagnostic.Errors.Add(new Microsoft.OpenApi.OpenApiError(context.GetLocation(),
                $"Deserializing criterion expression type '{expressionType.Type?.GetDisplayName()}' as an object is NOT supported by the specification."));
        }

        return expressionType;
    }

    private static void ApplyDefaultVersion(ArazzoCriterionExpressionType expressionType, ParsingContext context)
    {
        if (context.Diagnostic.SpecificationVersion is ArazzoSpecVersion.Arazzo1_1 && expressionType.Type.HasValue && !expressionType.Version.HasValue)
        {
            expressionType.Version = ArazzoCriterionExpressionType.GetDefaultVersion(expressionType.Type.Value);
        }
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