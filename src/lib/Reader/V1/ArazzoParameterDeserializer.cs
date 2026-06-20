using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

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

    public static IArazzoParameter LoadParameter(JsonNode node, ParsingContext context)
    {
        if (TryGetReferenceObject(node, out var jsonObject, out var referenceString))
        {
            var reference = CreateLocalReusableReference(
                referenceString,
                context,
                ReferenceType.Parameter,
                "Parameter",
                static (referenceId, hostDocument) => new ArazzoParameterReference(referenceId, hostDocument));

            if (jsonObject.TryGetPropertyValue(ArazzoConstants.ArazzoParameterValue, out var valueNode))
            {
                reference.Value = valueNode?.DeepClone();
                ArazzoRuntimeExpressionValidator.ValidateDeserializationExpressionStrings(reference.Value, context, $"{nameof(ArazzoParameterReference)}.{nameof(ArazzoParameterReference.Value)}");
            }

            return reference;
        }

        return LoadParameterObject(node, context);
    }

    public static ArazzoParameter LoadParameterObject(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("Parameter", context);
        var parameter = new ArazzoParameter();
        mapNode.ParseMap(parameter, ParameterFixedFields, ParameterPatternFields, context);
        ArazzoRuntimeExpressionValidator.ValidateDeserializationExpressionStrings(parameter.Value, context, $"{nameof(ArazzoParameter)}.{nameof(ArazzoParameter.Value)}");

        return parameter;
    }
}