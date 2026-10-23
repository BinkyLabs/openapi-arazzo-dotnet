using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoSourceDescription> SourceDescriptionFixedFields = new()
    {
        { ArazzoConstants.ArazzoSourceDescriptionName, static (o, v, c) => o.Name = v.GetScalarValue() },
        { ArazzoConstants.ArazzoSourceDescriptionUrl, static (o, v, c) => o.Url = LoadSourceDescriptionUrl(v) },
        { ArazzoConstants.ArazzoSourceDescriptionType, static (o, v, c) => {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoDescriptionType>(c, out var type))
            {
                return;
            }
            if (type is ArazzoDescriptionType.AsyncAPI && c.Diagnostic.SpecificationVersion is ArazzoSpecVersion.Arazzo1_0)
            {
                c.Diagnostic.Errors.Add(new OpenApiError(c.GetLocation(), "The value 'asyncapi' for 'type' is not supported in Arazzo 1.0."));
            }
            o.Type = type;
        }}
    };
    public static PatternFieldMap<ArazzoSourceDescription> GetSourceDescriptionPatternFields() =>
    new()
    {
        {s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c))}
    };
    public static readonly PatternFieldMap<ArazzoSourceDescription> SourceDescriptionPatternFields = GetSourceDescriptionPatternFields();

    public static ArazzoSourceDescription LoadSourceDescription(JsonNode node, ParsingContext context)
    {
        return LoadSourceDescriptionInternal(node, context, SourceDescriptionFixedFields, SourceDescriptionPatternFields);
    }

    public static ArazzoSourceDescription LoadSourceDescriptionInternal(JsonNode node, ParsingContext context, FixedFieldMap<ArazzoSourceDescription> sourceDescriptionFixedFields, PatternFieldMap<ArazzoSourceDescription> sourceDescriptionPatternFields)
    {
        var mapNode = node.CheckMapNode("SourceDescription", context);
        var sourceDescription = new ArazzoSourceDescription();
        mapNode.ParseMap(sourceDescription, sourceDescriptionFixedFields, sourceDescriptionPatternFields, context);
        ValidateSourceDescriptionRequiredFields(sourceDescription, context);

        return sourceDescription;
    }

    private static Uri? LoadSourceDescriptionUrl(JsonNode node)
    {
        var value = node.GetScalarValue();
        return string.IsNullOrEmpty(value) ? null : new Uri(value, UriKind.RelativeOrAbsolute);
    }

    private static void ValidateSourceDescriptionRequiredFields(ArazzoSourceDescription sourceDescription, ParsingContext context)
    {
        if (string.IsNullOrEmpty(sourceDescription.Name))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoSourceDescription)}.{nameof(ArazzoSourceDescription.Name)} is a REQUIRED field."));
        }

        if (sourceDescription.Url is null)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoSourceDescription)}.{nameof(ArazzoSourceDescription.Url)} is a REQUIRED field."));
        }
    }
}