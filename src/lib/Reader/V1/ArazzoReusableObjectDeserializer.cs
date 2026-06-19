using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoReusableObject> ReusableObjectFixedFields = new()
    {
        { ArazzoConstants.ArazzoReusableObjectReference, static (o, v, c) => o.Reference = v.GetScalarValue() },
        { ArazzoConstants.ArazzoReusableObjectValue, static (o, v, c) => o.Value = v.GetScalarValue() }
    };

    public static readonly PatternFieldMap<ArazzoReusableObject> ReusableObjectPatternFields = new();

    public static ArazzoReusableObject LoadReusableObject(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("ReusableObject", context);
        var reusableObject = new ArazzoReusableObject();
        mapNode.ParseMap(reusableObject, ReusableObjectFixedFields, ReusableObjectPatternFields, context);
        ArazzoReusableObjectReferenceValidator.ValidateDeserializationReference(reusableObject.Reference, context, null, nameof(ArazzoReusableObject));

        return reusableObject;
    }
}