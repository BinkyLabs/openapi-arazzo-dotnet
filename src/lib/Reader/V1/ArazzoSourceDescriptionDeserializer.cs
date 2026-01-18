namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoSourceDescription> SourceDescriptionFixedFields = new()
    {
        { ArazzoConstants.ArazzoSourceDescriptionName, static (o, v) => o.Name = v.GetScalarValue() },
        { ArazzoConstants.ArazzoSourceDescriptionUrl, static (o, v) => o.Url = new Uri(v.GetScalarValue() ?? string.Empty) },
        { ArazzoConstants.ArazzoSourceDescriptionType, static (o, v) => {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoDescriptionType>(v.Context, out var type))
            {
                return;
            }
            o.Type = type;
        }}
    };
    public static readonly PatternFieldMap<ArazzoSourceDescription> SourceDescriptionPatternFields = new()
    {
        {s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k, LoadExtension(k, n))}
    };

    public static ArazzoSourceDescription LoadSourceDescription(ParseNode node)
    {
        var mapNode = node.CheckMapNode("SourceDescription");
        var sourceDescription = new ArazzoSourceDescription();
        ParseMap(mapNode, sourceDescription, SourceDescriptionFixedFields, SourceDescriptionPatternFields);

        return sourceDescription;
    }
}