using System.Text;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoComponent> ComponentFixedFields = new()
    {
        { ArazzoConstants.ArazzoComponentParameters, static (o, v, c) => o.Parameters = v.CreateMap(LoadParameter, c) },
        { ArazzoConstants.ArazzoComponentSuccessActions, static (o, v, c) => o.SuccessActions = v.CreateMap(LoadSuccessAction, c) },
        { ArazzoConstants.ArazzoComponentFailureActions, static (o, v, c) => o.FailureActions = v.CreateMap(LoadFailureAction, c) },
        { ArazzoConstants.ArazzoComponentInputs, static (o, v, c) => o.Inputs = v.CreateMap(LoadSchema, c).Where(static x => x.Value != null).ToDictionary(static x => x.Key, static x => x.Value!) },
    };

    public static readonly PatternFieldMap<ArazzoComponent> ComponentPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static IOpenApiSchema? LoadSchema(JsonNode node, ParsingContext context)
    {
        //TODO this leads to double encoding and memory overhead, find a better way by adding an overload that accepts json node
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(node.ToJsonString()));
        if (node is JsonObject jsonObject &&
            jsonObject.TryGetPropertyValue("$ref", out var reference)
            && reference is JsonValue referenceValue
            && referenceValue.TryGetValue<string>(out var referenceString)
            && !string.IsNullOrEmpty(referenceString))
        {
            // TODO the reference might fail to resolve because of the underlying infrastructure. We might need to come up with our own type.
            return OpenApiModelFactory.Load<OpenApiSchemaReference>(ms, OpenApiSpecVersion.OpenApi3_2, OpenApiConstants.Json, new(), out var varRef);
        }
        return OpenApiModelFactory.Load<OpenApiSchema>(ms, OpenApiSpecVersion.OpenApi3_2, OpenApiConstants.Json, new(), out var _);
    }

    public static ArazzoComponent LoadComponent(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("Component", context);
        var component = new ArazzoComponent();

        // TODO: Implement validation during serialization/deserialization that any of the keys 
        // of Parameters, SuccessActions, FailureActions, and Inputs dictionaries must match 
        // the following regex: ^[a-zA-Z0-9\.\-_]+$

        mapNode.ParseMap(component, ComponentFixedFields, ComponentPatternFields, context);

        return component;
    }
}