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
            o.Type = type;
        }}
    };
    public static readonly PatternFieldMap<ArazzoSourceDescription> SourceDescriptionPatternFields = new()
    {
        {s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c))}
    };

    public static ArazzoSourceDescription LoadSourceDescription(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("SourceDescription", context);
        var sourceDescription = new ArazzoSourceDescription();
        mapNode.ParseMap(sourceDescription, SourceDescriptionFixedFields, SourceDescriptionPatternFields, context);
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