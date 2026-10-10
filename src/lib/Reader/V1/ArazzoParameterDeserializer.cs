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
            var location = v.GetScalarValue();
            if (!location.TryGetEnumFromDisplayName<ParameterLocation>(c, out var _in))
            {
                return;
            }
            if ("querystring".Equals(location, StringComparison.OrdinalIgnoreCase) && c.Diagnostic.SpecificationVersion is ArazzoSpecVersion.Arazzo1_0)
            {
                c.Diagnostic.Errors.Add(new OpenApiError(c.GetLocation(), "The value 'querystring' for 'in' is not supported in Arazzo 1.0."));
                return;
            }
            o.In = _in;
        } },
        { ArazzoConstants.ArazzoParameterValue, static (o, v, c) => o.Value = v }
    };

    public static PatternFieldMap<ArazzoParameter> GetParameterPatternFields() =>
    new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };
    public static readonly PatternFieldMap<ArazzoParameter> ParameterPatternFields = GetParameterPatternFields();

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
        return LoadParameterObjectInternal(node, context, ParameterFixedFields, ParameterPatternFields);
    }

    public static ArazzoParameter LoadParameterObjectInternal(JsonNode node, ParsingContext context, FixedFieldMap<ArazzoParameter> parameterFixedFields, PatternFieldMap<ArazzoParameter> parameterPatternFields)
    {
        var mapNode = node.CheckMapNode("Parameter", context);
        var parameter = new ArazzoParameter();
        mapNode.ParseMap(parameter, parameterFixedFields, parameterPatternFields, context);
        ArazzoRuntimeExpressionValidator.ValidateDeserializationExpressionStrings(parameter.Value, context, $"{nameof(ArazzoParameter)}.{nameof(ArazzoParameter.Value)}");
        ArazzoParameterValidator.ValidateDeserializationRequiredFields(parameter, context);

        return parameter;
    }
}