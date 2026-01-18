using System.Text;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoComponent> ComponentFixedFields = new()
    {
        { ArazzoConstants.ArazzoComponentParameters, (o, v) => o.Parameters = v.CreateMap(LoadParameter) },
        { ArazzoConstants.ArazzoComponentSuccessActions, (o, v) => o.SuccessActions = v.CreateMap(LoadSuccessAction) },
        { ArazzoConstants.ArazzoComponentFailureActions, (o, v) => o.FailureActions = v.CreateMap(LoadFailureAction) },
        { ArazzoConstants.ArazzoComponentInputs, (o, v) => o.Inputs = v.CreateMap(LoadSchema).Where(static x => x.Value != null).ToDictionary(static x => x.Key, static x => x.Value!) },
    };

    public static readonly PatternFieldMap<ArazzoComponent> ComponentPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k, LoadExtension(k, n)) }
    };

    public static OpenApiSchema? LoadSchema(ParseNode node)
    {
        //TODO this leads to double encoding and memory overhead, find a better way by adding an overload that accepts json node
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(node.JsonNode.ToJsonString()));
        return OpenApiModelFactory.Load<OpenApiSchema>(ms, OpenApiSpecVersion.OpenApi3_2, OpenApiConstants.Json, new(), out var _);
    }

    public static ArazzoComponent LoadComponent(ParseNode node)
    {
        var mapNode = node.CheckMapNode("Component");
        var component = new ArazzoComponent();

        // TODO: Implement validation during serialization/deserialization that any of the keys 
        // of Parameters, SuccessActions, FailureActions, and Inputs dictionaries must match 
        // the following regex: ^[a-zA-Z0-9\.\-_]+$

        ParseMap(mapNode, component, ComponentFixedFields, ComponentPatternFields);

        return component;
    }
}