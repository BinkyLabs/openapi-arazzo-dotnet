using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoDocument> DocumentFixedFields = new()
    {
        { ArazzoConstants.ArazzoDocumentArazzo, static (o, v, c) => o.Arazzo = v.GetScalarValue() },
        { ArazzoConstants.ArazzoDocumentInfo, static (o, v, c) => o.Info = LoadInfo(v, c) },
        { ArazzoConstants.ArazzoDocumentSourceDescriptions, static (o, v, c) => o.SourceDescriptions = v.CreateList(LoadSourceDescription, c) },
        { ArazzoConstants.ArazzoDocumentWorkflows, static (o, v, c) => o.Workflows = v.CreateList(LoadWorkflow, c) },
        { ArazzoConstants.ArazzoDocumentComponents, static (o, v, c) => o.Components = LoadComponent(v, c) },
    };
    public static PatternFieldMap<ArazzoDocument> GetDocumentPatternFields() =>
    new()
    {
        {s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k,LoadExtension(k, n, c))}
    };
    public static readonly PatternFieldMap<ArazzoDocument> DocumentPatternFields = GetDocumentPatternFields();
    public static ArazzoDocument LoadArazzoDocument(JsonNode node, Uri location, ParsingContext context)
    {
        return LoadArazzoDocumentInternal(node, location, context, DocumentFixedFields, DocumentPatternFields);
    }
    public static ArazzoDocument LoadArazzoDocumentInternal(JsonNode node, Uri location, ParsingContext context, FixedFieldMap<ArazzoDocument> documentFixedFields, PatternFieldMap<ArazzoDocument> documentPatternFields)
    {
        var document = new ArazzoDocument();
        document.BaseUri = location;
        context.SetTempStorage("CurrentDocument", document);
        node.CheckMapNode("Document", context).ParseMap(document, documentFixedFields, documentPatternFields, context);
        context.SetTempStorage("CurrentDocument", null);
        return document;
    }
    public static ArazzoDocument LoadDocument(JsonNode node, ParsingContext context)
    {
        return LoadArazzoDocument(node, context.BaseUrl ?? new Uri(OpenApiConstants.BaseRegistryUri + Guid.NewGuid()), context);
    }
}