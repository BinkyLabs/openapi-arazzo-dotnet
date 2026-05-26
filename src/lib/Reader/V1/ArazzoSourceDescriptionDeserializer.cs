using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoSourceDescription> SourceDescriptionFixedFields = new()
    {
        { ArazzoConstants.ArazzoSourceDescriptionName, static (o, v, c) => o.Name = v.GetScalarValue() },
        { ArazzoConstants.ArazzoSourceDescriptionUrl, static (o, v, c) => o.Url = new Uri(v.GetScalarValue() ?? string.Empty) },
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

        return sourceDescription;
    }
}