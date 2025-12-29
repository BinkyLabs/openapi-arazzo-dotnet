using System;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoParameter> ParameterFixedFields = new()
    {
        { ArazzoConstants.ArazzoParameterName, (o, v) => o.Name = v.GetScalarValue() },
        { ArazzoConstants.ArazzoParameterIn, (o, v) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ParameterLocation>(v.Context, out var _in))
            {
                return;
            }
            o.In = _in;
        } },
        { ArazzoConstants.ArazzoParameterValue, (o, v) => o.Value = v.CreateAny() }
    };

    public static readonly PatternFieldMap<ArazzoParameter> ParameterPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k, LoadExtension(k, n)) }
    };

    public static ArazzoParameter LoadParameter(ParseNode node)
    {
        var mapNode = node.CheckMapNode("Parameter");
        var parameter = new ArazzoParameter();
        ParseMap(mapNode, parameter, ParameterFixedFields, ParameterPatternFields);

        return parameter;
    }

    private static ParameterLocation ParseParameterLocation(ParseNode node)
    {
        var location = node.GetScalarValue();

        if (Enum.TryParse<ParameterLocation>(location, true, out var parameterLocation))
        {
            return parameterLocation;
        }

        throw new ArazzoReaderException($"Invalid parameter location '{location}'.", node.Context);
    }
}