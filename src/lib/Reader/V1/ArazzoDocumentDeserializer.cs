using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoDocument> DocumentFixedFields = new()
    {
        { ArazzoConstants.ArazzoDocumentArazzo, (o, v) => o.Arazzo = v.GetScalarValue() },
        { ArazzoConstants.ArazzoDocumentInfo, (o, v) => o.Info = LoadInfo(v) },
    };
    public static readonly PatternFieldMap<ArazzoDocument> DocumentPatternFields = new()
    {
        {s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k,LoadExtension(k, n))}
    };
    public static ArazzoDocument LoadArazzoDocument(RootNode rootNode, Uri location)
    {
        var document = new ArazzoDocument();
        ParseMap(rootNode.GetMap(), document, DocumentFixedFields, DocumentPatternFields);
        return document;
    }
    public static ArazzoDocument LoadDocument(ParseNode node)
    {
        var mapNode = node.CheckMapNode("Document");
        var info = new ArazzoDocument();
        ParseMap(mapNode, info, DocumentFixedFields, DocumentPatternFields);

        return info;
    }
}