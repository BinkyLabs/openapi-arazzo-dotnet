using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoInfo> InfoFixedFields = new()
    {
        { ArazzoConstants.ArazzoInfoTitle, (o, v) => o.Title = v.GetScalarValue() },
        { ArazzoConstants.ArazzoInfoVersion, (o, v) => o.Version = v.GetScalarValue() }
    };
    public static readonly PatternFieldMap<ArazzoInfo> InfoPatternFields = new()
    {
        {s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k,LoadExtension(k, n))}
    };
    public static ArazzoInfo LoadInfo(ParseNode node)
    {
        var mapNode = node.CheckMapNode("Info");
        var info = new ArazzoInfo();
        ParseMap(mapNode, info, InfoFixedFields, InfoPatternFields);

        return info;
    }
}