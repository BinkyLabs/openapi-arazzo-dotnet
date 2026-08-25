using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader.V1;
using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1_1;

internal static partial class ArazzoV1_1Deserializer
{
    public static readonly FixedFieldMap<ArazzoSelector> SelectorFixedFields = new()
    {
        { ArazzoConstants.ArazzoSelectorContext, static (o, v, c) => o.Context = v.GetScalarValue() },
        { ArazzoConstants.ArazzoSelectorSelector, static (o, v, c) => o.Selector = v.GetScalarValue() },
        { ArazzoConstants.ArazzoSelectorType, static (o, v, c) => o.Type = v }
    };

    public static PatternFieldMap<ArazzoSelector> GetSelectorPatternFields() =>
    new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, ArazzoV1Deserializer.LoadExtension(k, n, c)) }
    };

    public static readonly PatternFieldMap<ArazzoSelector> SelectorPatternFields = GetSelectorPatternFields();

    public static ArazzoSelector LoadSelector(JsonNode node, ParsingContext context)
    {
        context.Diagnostic.SpecificationVersion = ArazzoSpecVersion.Arazzo1_1;
        var mapNode = node.CheckMapNode("Selector", context);
        var selector = new ArazzoSelector();
        mapNode.ParseMap(selector, SelectorFixedFields, SelectorPatternFields, context);
        ValidateSelectorRequiredFields(selector, context);
        ArazzoRuntimeExpressionValidator.ValidateDeserializationExpression(selector.Context, context, $"{nameof(ArazzoSelector)}.{nameof(ArazzoSelector.Context)}");

        return selector;
    }

    private static void ValidateSelectorRequiredFields(ArazzoSelector selector, ParsingContext context)
    {
        if (string.IsNullOrEmpty(selector.Context))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoSelector)}.{nameof(ArazzoSelector.Context)} is a REQUIRED field."));
        }

        if (string.IsNullOrEmpty(selector.Selector))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoSelector)}.{nameof(ArazzoSelector.Selector)} is a REQUIRED field."));
        }

        if (selector.Type is null)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoSelector)}.{nameof(ArazzoSelector.Type)} is a REQUIRED field."));
        }
    }
}
