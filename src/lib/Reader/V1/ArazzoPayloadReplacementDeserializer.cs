using System;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoPayloadReplacement> PayloadReplacementFixedFields = new()
    {
        { ArazzoConstants.ArazzoPayloadReplacementTarget, (o, v) => o.Target = v.GetScalarValue() },
        { ArazzoConstants.ArazzoPayloadReplacementValue, (o, v) => o.Value = v.CreateAny() }
    };

    public static readonly PatternFieldMap<ArazzoPayloadReplacement> PayloadReplacementPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k, LoadExtension(k, n)) }
    };

    public static ArazzoPayloadReplacement LoadPayloadReplacement(ParseNode node)
    {
        var mapNode = node.CheckMapNode("PayloadReplacement");
        var replacement = new ArazzoPayloadReplacement();
        ParseMap(mapNode, replacement, PayloadReplacementFixedFields, PayloadReplacementPatternFields);

        return replacement;
    }
}