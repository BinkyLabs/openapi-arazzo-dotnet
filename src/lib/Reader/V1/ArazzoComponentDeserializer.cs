using System.Text;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoComponent> ComponentFixedFields = new()
    {
        { ArazzoConstants.ArazzoComponentParameters, static (o, v, c) => o.Parameters = v.CreateMap(LoadParameterObject, c) },
        { ArazzoConstants.ArazzoComponentSuccessActions, static (o, v, c) => o.SuccessActions = v.CreateMap(LoadSuccessActionObject, c) },
        { ArazzoConstants.ArazzoComponentFailureActions, static (o, v, c) => o.FailureActions = v.CreateMap(LoadFailureActionObject, c) },
        { ArazzoConstants.ArazzoComponentInputs, static (o, v, c) => o.Inputs = v.CreateMap(LoadSchema, c).Where(static x => x.Value != null).ToDictionary(static x => x.Key, static x => x.Value!) },
    };

    public static readonly PatternFieldMap<ArazzoComponent> ComponentPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static IArazzoInput? LoadSchema(JsonNode node, ParsingContext context)
    {
        //TODO this leads to double encoding and memory overhead, find a better way by adding an overload that accepts json node
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(node.ToJsonString()));
        if (node is JsonObject jsonObject &&
            jsonObject.TryGetPropertyValue("$ref", out var reference)
            && reference is JsonValue referenceValue
            && referenceValue.TryGetValue<string>(out var referenceString)
            && !string.IsNullOrEmpty(referenceString))
        {
            var hostDocument = context.GetFromTempStorage<ArazzoDocument>("CurrentDocument");
            var inputReference = new ArazzoInputReference(GetReferenceId(referenceString), hostDocument, GetExternalResource(referenceString));
            inputReference.SetMetadataFromJsonObject(jsonObject);
            inputReference.Reference.EnsureHostDocumentIsSet(hostDocument ?? new ArazzoDocument());
            inputReference.Reference.SetJsonPointerPath(referenceString, context.GetLocation());
            return inputReference;
        }

        var schema = OpenApiModelFactory.Load<OpenApiSchema>(ms, OpenApiSpecVersion.OpenApi3_2, OpenApiConstants.Json, new(), out var _);
        var host = context.GetFromTempStorage<ArazzoDocument>("CurrentDocument");
        return schema is OpenApiSchema openApiSchema ? ArazzoInput.ConvertFromOpenApiSchema(openApiSchema, host) : null;
    }

    internal static string GetReferenceId(string referenceString)
    {
        var fragment = referenceString.Contains('#', StringComparison.Ordinal)
            ? referenceString[(referenceString.IndexOf('#', StringComparison.Ordinal) + 1)..]
            : referenceString;

        if (fragment.StartsWith("$components.", StringComparison.OrdinalIgnoreCase))
        {
            var componentPath = fragment["$components.".Length..];
            var separatorIndex = componentPath.IndexOf('.', StringComparison.Ordinal);
            if (separatorIndex > 0 && separatorIndex < componentPath.Length - 1)
            {
                return componentPath[(separatorIndex + 1)..];
            }
        }

        var trimmedFragment = fragment.TrimEnd('/');
        if (trimmedFragment.Contains('/', StringComparison.Ordinal))
        {
            var segment = trimmedFragment[(trimmedFragment.LastIndexOf('/') + 1)..];
            if (!string.IsNullOrEmpty(segment))
            {
                return segment;
            }
        }

        if (referenceString.Contains('/', StringComparison.Ordinal))
        {
            var segment = referenceString[(referenceString.LastIndexOf('/') + 1)..];
            if (!string.IsNullOrEmpty(segment))
            {
                return segment;
            }
        }

        return referenceString;
    }

    internal static string? GetExternalResource(string referenceString)
    {
        var fragmentIndex = referenceString.IndexOf('#', StringComparison.Ordinal);
        return fragmentIndex > 0 ? referenceString[..fragmentIndex] : null;
    }

    internal static void ThrowIfExternalReferenceNotSupported(string referenceString, string elementName)
    {
        if (!string.IsNullOrEmpty(GetExternalResource(referenceString)))
        {
            throw new OpenApiException($"{elementName} references do not support external resources: '{referenceString}'.");
        }
    }

    internal static bool TryGetReferenceObject(JsonNode node, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out JsonObject? jsonObject, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? referenceString)
    {
        jsonObject = node as JsonObject;
        referenceString = null;

        if (jsonObject?.TryGetPropertyValue(OpenApiConstants.DollarRef, out var referenceNode) == true &&
            referenceNode is JsonValue referenceValue &&
            referenceValue.TryGetValue<string>(out var parsedReferenceString) &&
            !string.IsNullOrEmpty(parsedReferenceString))
        {
            referenceString = parsedReferenceString;
            return true;
        }

        return false;
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