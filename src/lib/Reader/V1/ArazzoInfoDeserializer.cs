using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoInfo> InfoFixedFields = new()
    {
        { ArazzoConstants.ArazzoInfoTitle, static (o, v, c) => o.Title = v.GetScalarValue() },
        { ArazzoConstants.ArazzoInfoVersion, static (o, v, c) => o.Version = v.GetScalarValue() }
    };
    public static readonly PatternFieldMap<ArazzoInfo> InfoPatternFields = new()
    {
        {s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k,LoadExtension(k, n, c))}
    };
    public static ArazzoInfo LoadInfo(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("Info", context);
        var info = new ArazzoInfo();
        mapNode.ParseMap(info, InfoFixedFields, InfoPatternFields, context);

        return info;
    }
}