using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoInfo> InfoFixedFields = new()
    {
        { ArazzoConstants.ArazzoInfoTitle, static (o, v, c) => o.Title = v.GetScalarValue() },
        { ArazzoConstants.ArazzoInfoVersion, static (o, v, c) => o.Version = v.GetScalarValue() },
        { ArazzoConstants.ArazzoInfoSummary, static (o, v, c) => o.Summary = v.GetScalarValue() },
        { ArazzoConstants.ArazzoInfoDescription, static (o, v, c) => o.Description = v.GetScalarValue() }
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
        ValidateInfoRequiredFields(info, context);

        return info;
    }

    private static void ValidateInfoRequiredFields(ArazzoInfo info, ParsingContext context)
    {
        if (string.IsNullOrEmpty(info.Title))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoInfo)}.{nameof(ArazzoInfo.Title)} is a REQUIRED field."));
        }

        if (string.IsNullOrEmpty(info.Version))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoInfo)}.{nameof(ArazzoInfo.Version)} is a REQUIRED field."));
        }
    }
}