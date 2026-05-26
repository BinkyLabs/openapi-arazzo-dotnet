using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoParameter> ParameterFixedFields = new()
    {
        { ArazzoConstants.ArazzoParameterName, static (o, v, c) => o.Name = v.GetScalarValue() },
        { ArazzoConstants.ArazzoParameterIn, static (o, v, c) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ParameterLocation>(c, out var _in))
            {
                return;
            }
            o.In = _in;
        } },
        { ArazzoConstants.ArazzoParameterValue, static (o, v, c) => o.Value = v }
    };

    public static readonly PatternFieldMap<ArazzoParameter> ParameterPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static ArazzoParameter LoadParameter(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("Parameter", context);
        var parameter = new ArazzoParameter();
        mapNode.ParseMap(parameter, ParameterFixedFields, ParameterPatternFields, context);

        return parameter;
    }
}