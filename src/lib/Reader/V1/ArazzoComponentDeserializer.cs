using System.Text;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoComponent> ComponentFixedFields = new()
    {
        { ArazzoConstants.ArazzoComponentParameters, static (o, v, c) =>
        {
            ArazzoKeyValidator.ValidateDeserializationKeys(v, c, $"{nameof(ArazzoComponent)}.{nameof(ArazzoComponent.Parameters)}");
            o.Parameters = v.CreateMap(LoadParameterObject, c);
        } },
        { ArazzoConstants.ArazzoComponentSuccessActions, static (o, v, c) =>
        {
            ArazzoKeyValidator.ValidateDeserializationKeys(v, c, $"{nameof(ArazzoComponent)}.{nameof(ArazzoComponent.SuccessActions)}");
            o.SuccessActions = v.CreateMap(LoadSuccessActionObject, c);
        } },
        { ArazzoConstants.ArazzoComponentFailureActions, static (o, v, c) =>
        {
            ArazzoKeyValidator.ValidateDeserializationKeys(v, c, $"{nameof(ArazzoComponent)}.{nameof(ArazzoComponent.FailureActions)}");
            o.FailureActions = v.CreateMap(LoadFailureActionObject, c);
        } },
        { ArazzoConstants.ArazzoComponentInputs, static (o, v, c) =>
        {
            ArazzoKeyValidator.ValidateDeserializationKeys(v, c, $"{nameof(ArazzoComponent)}.{nameof(ArazzoComponent.Inputs)}");
            o.Inputs = v.CreateMap(LoadSchema, c).Where(static x => x.Value != null).ToDictionary(static x => x.Key, static x => x.Value!);
        } },
    };

    public static readonly PatternFieldMap<ArazzoComponent> ComponentPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static IArazzoInput? LoadSchema(JsonNode node, ParsingContext context)
    {
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

        var jsonReader = new OpenApiJsonReader();
        var schema = jsonReader.ReadFragment<OpenApiSchema>(node, OpenApiSpecVersion.OpenApi3_2, new(), out var _);
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

    internal static TReference CreateLocalReusableReference<TReference>(
        string referenceString,
        ParsingContext context,
        ReferenceType referenceType,
        string elementName,
        Func<string, ArazzoDocument?, TReference> createReference)
        where TReference : IArazzoReferenceHolder<BaseArazzoReference>
    {
        ThrowIfExternalReferenceNotSupported(referenceString, elementName);
        ArazzoReusableObjectReferenceValidator.ValidateDeserializationReference(referenceString, context, referenceType, elementName);

        var hostDocument = context.GetFromTempStorage<ArazzoDocument>("CurrentDocument");
        var reference = createReference(GetReferenceId(referenceString), hostDocument);
        reference.Reference.EnsureHostDocumentIsSet(hostDocument ?? new ArazzoDocument());
        reference.Reference.SetJsonPointerPath(referenceString, context.GetLocation());
        return reference;
    }

    internal static bool TryGetReferenceObject(JsonNode node, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out JsonObject? jsonObject, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? referenceString)
    {
        jsonObject = node as JsonObject;
        referenceString = null;

        if (jsonObject?.TryGetPropertyValue(ArazzoConstants.ArazzoReusableObjectReference, out var referenceNode) == true &&
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

        mapNode.ParseMap(component, ComponentFixedFields, ComponentPatternFields, context);

        return component;
    }
}