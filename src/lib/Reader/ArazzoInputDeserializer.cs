using System.Text.Json.Nodes;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

using OpenApiJsonNodeExtension = Microsoft.OpenApi.JsonNodeExtension;

namespace BinkyLabs.OpenApi.Arazzo.Reader;

internal static class ArazzoInputDeserializer
{
    /// <summary>
    /// Deserializes an input schema from its original JSON node, preserving reference text and metadata.
    /// </summary>
    /// <param name="node">The original JSON or YAML-derived schema node, before OpenAPI schema parsing can normalize references.</param>
    /// <param name="context">The parsing context used to associate references with their host document and location.</param>
    /// <returns>The deserialized input schema, or <see langword="null"/> if the OpenAPI reader does not return a schema.</returns>
    internal static IArazzoInput? LoadSchema(JsonNode node, ParsingContext context)
    {
        if (node is JsonObject jsonObject &&
            jsonObject.TryGetPropertyValue(OpenApiConstants.DollarRef, out var reference)
            && reference is JsonValue referenceValue
            && referenceValue.TryGetValue<string>(out var referenceString)
            && !string.IsNullOrEmpty(referenceString))
        {
            var hostDocument = context.GetFromTempStorage<ArazzoDocument>("CurrentDocument");
            var inputReference = new ArazzoInputReference(V1.ArazzoV1Deserializer.GetReferenceId(referenceString),
                hostDocument, V1.ArazzoV1Deserializer.GetExternalResource(referenceString));
            inputReference.SetMetadataFromJsonObject(jsonObject);
            inputReference.Reference.EnsureHostDocumentIsSet(hostDocument ?? new ArazzoDocument());
            inputReference.Reference.SetJsonPointerPath(referenceString, context.GetLocation());
            return inputReference;
        }

        var jsonReader = new OpenApiJsonReader();
        var schema = jsonReader.ReadFragment<OpenApiSchema>(node, OpenApiSpecVersion.OpenApi3_2, new(), out var _);
        var host = context.GetFromTempStorage<ArazzoDocument>("CurrentDocument");
        return schema is OpenApiSchema openApiSchema ? ConvertFromOpenApiSchema(openApiSchema, host, node) : null;
    }

    /// <summary>
    /// Converts an OpenAPI schema to an Arazzo input while preserving original schema reference text.
    /// </summary>
    /// <remarks>
    /// OpenAPI parsing can normalize nested references relative to the outer document. Reading the
    /// original node preserves their spelling so references within nested $id resources retain their scope.
    /// The source node is conversion context only; it is neither modified nor retained by the input model.
    /// </remarks>
    /// <param name="schema">The parsed OpenAPI schema to convert.</param>
    /// <param name="hostDocument">The Arazzo document that owns the input references.</param>
    /// <param name="sourceNode">The corresponding original schema node, when available, used to read unmodified $ref values.</param>
    /// <returns>The converted input schema or reference.</returns>
    internal static IArazzoInput ConvertFromOpenApiSchema(IOpenApiSchema schema, ArazzoDocument? hostDocument = null, JsonNode? sourceNode = null)
    {
        if (schema is OpenApiSchemaReference schemaReference)
        {
            var sourceReference = sourceNode is JsonObject sourceObject
                ? BaseArazzoReference.GetPropertyValueFromNode(sourceObject, OpenApiConstants.DollarRef)
                : null;
            return ArazzoInputReferenceFactory.Create(schemaReference, hostDocument, sourceReference);
        }

        if (schema is not OpenApiSchema openApiSchema)
        {
            throw new NotSupportedException($"Conversion from {schema.GetType().Name} is not supported.");
        }

        ValidateUnsupportedOpenApiKeywords(openApiSchema);
        var source = sourceNode as JsonObject;

        return new ArazzoInput
        {
            Title = schema.Title,
            Schema = schema.Schema,
            Id = schema.Id,
            Comment = schema.Comment,
            Vocabulary = schema.Vocabulary is null ? null : new Dictionary<string, bool>(schema.Vocabulary),
            DynamicRef = schema.DynamicRef,
            DynamicAnchor = schema.DynamicAnchor,
            Definitions = ConvertSchemaMap(schema.Definitions, hostDocument, source?[OpenApiConstants.Defs]),
            Anchor = openApiSchema.Anchor,
            ExclusiveMaximum = schema.ExclusiveMaximum,
            ExclusiveMinimum = schema.ExclusiveMinimum,
            Type = schema.Type,
            Const = schema.Const,
            Format = schema.Format,
            Description = schema.Description,
            Maximum = schema.Maximum,
            Minimum = schema.Minimum,
            MaxLength = schema.MaxLength,
            MinLength = schema.MinLength,
            Pattern = schema.Pattern,
            MultipleOf = schema.MultipleOf,
            Default = ArazzoInput.CloneNode(schema.Default),
            ReadOnly = schema.ReadOnly,
            WriteOnly = schema.WriteOnly,
            AllOf = ConvertSchemaList(schema.AllOf, hostDocument, source?[OpenApiConstants.AllOf]),
            OneOf = ConvertSchemaList(schema.OneOf, hostDocument, source?[OpenApiConstants.OneOf]),
            AnyOf = ConvertSchemaList(schema.AnyOf, hostDocument, source?[OpenApiConstants.AnyOf]),
            Not = schema.Not is null ? null : ConvertFromOpenApiSchema(schema.Not, hostDocument, source?[OpenApiConstants.Not]),
            Required = schema.Required is null ? null : new HashSet<string>(schema.Required),
            Items = schema.Items is null ? null : ConvertFromOpenApiSchema(schema.Items, hostDocument, source?[OpenApiConstants.Items]),
            MaxItems = schema.MaxItems,
            MinItems = schema.MinItems,
            UniqueItems = schema.UniqueItems,
            Contains = openApiSchema.Contains is null ? null : ConvertFromOpenApiSchema(openApiSchema.Contains, hostDocument, source?[OpenApiConstants.Contains]),
            MaxContains = openApiSchema.MaxContains,
            MinContains = openApiSchema.MinContains,
            Properties = ConvertSchemaMap(schema.Properties, hostDocument, source?[OpenApiConstants.Properties]),
            PatternProperties = ConvertSchemaMap(schema.PatternProperties, hostDocument, source?[OpenApiConstants.PatternProperties]),
            MaxProperties = schema.MaxProperties,
            MinProperties = schema.MinProperties,
            AdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed,
            AdditionalProperties = schema.AdditionalProperties is null ? null : ConvertFromOpenApiSchema(schema.AdditionalProperties, hostDocument, source?[OpenApiConstants.AdditionalProperties]),
            Examples = ArazzoInput.CloneNodeList(schema.Examples),
            Enum = ArazzoInput.CloneNodeList(schema.Enum),
            UnevaluatedProperties = openApiSchema.UnevaluatedProperties,
            UnevaluatedPropertiesSchema = openApiSchema.UnevaluatedPropertiesSchema is null ? null : ConvertFromOpenApiSchema(openApiSchema.UnevaluatedPropertiesSchema, hostDocument, source?[OpenApiConstants.UnevaluatedProperties]),
            ContentEncoding = openApiSchema.ContentEncoding,
            ContentMediaType = openApiSchema.ContentMediaType,
            ContentSchema = openApiSchema.ContentSchema is null ? null : ConvertFromOpenApiSchema(openApiSchema.ContentSchema, hostDocument, source?[OpenApiConstants.ContentSchema]),
            PropertyNames = openApiSchema.PropertyNames is null ? null : ConvertFromOpenApiSchema(openApiSchema.PropertyNames, hostDocument, source?[OpenApiConstants.PropertyNames]),
            DependentSchemas = ConvertSchemaMap(openApiSchema.DependentSchemas, hostDocument, source?[OpenApiConstants.DependentSchemas]),
            If = openApiSchema.If is null ? null : ConvertFromOpenApiSchema(openApiSchema.If, hostDocument, source?[OpenApiConstants.If]),
            Then = openApiSchema.Then is null ? null : ConvertFromOpenApiSchema(openApiSchema.Then, hostDocument, source?[OpenApiConstants.Then]),
            Else = openApiSchema.Else is null ? null : ConvertFromOpenApiSchema(openApiSchema.Else, hostDocument, source?[OpenApiConstants.Else]),
            Deprecated = schema.Deprecated,
            Extensions = ConvertExtensions(openApiSchema.Extensions),
            DependentRequired = ArazzoInput.CloneDependentRequired(schema.DependentRequired)
        };
    }

    private static void ValidateUnsupportedOpenApiKeywords(OpenApiSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var unsupportedKeywords = new List<string>();

        if (schema.Discriminator is not null)
        {
            unsupportedKeywords.Add(nameof(schema.Discriminator));
        }

#pragma warning disable CS0618 // Type or member is obsolete
        if (schema.Example is not null)
        {
            unsupportedKeywords.Add(nameof(schema.Example));
        }
#pragma warning restore CS0618 // Type or member is obsolete

        if (schema.ExternalDocs is not null)
        {
            unsupportedKeywords.Add(nameof(schema.ExternalDocs));
        }

        if (schema.Xml is not null)
        {
            unsupportedKeywords.Add(nameof(schema.Xml));
        }

        if (unsupportedKeywords.Count > 0)
        {
            throw new InvalidOperationException(
                $"OpenAPI-specific schema keywords are not supported by {nameof(ArazzoInput)}: {string.Join(", ", unsupportedKeywords)}.");
        }
    }

    /// <summary>
    /// Converts a schema map, matching each entry to its original JSON property to preserve nested reference scope.
    /// </summary>
    /// <param name="source">The parsed schemas keyed by property or definition name.</param>
    /// <param name="hostDocument">The document that owns the input references.</param>
    /// <param name="sourceNode">The original map node whose children supply unmodified reference values.</param>
    /// <returns>The converted schema map, or <see langword="null"/> when no map is supplied.</returns>
    private static IDictionary<string, IArazzoInput>? ConvertSchemaMap(
        IDictionary<string, IOpenApiSchema>? source,
        ArazzoDocument? hostDocument,
        JsonNode? sourceNode = null)
    {
        if (source is null)
        {
            return null;
        }

        var sourceObject = sourceNode as JsonObject;
        return source.ToDictionary(static pair => pair.Key, pair => ConvertFromOpenApiSchema(pair.Value, hostDocument, sourceObject?[pair.Key]));
    }

    /// <summary>
    /// Converts a schema list, matching each entry to its original JSON array element to preserve nested reference scope.
    /// </summary>
    /// <param name="source">The parsed schema list.</param>
    /// <param name="hostDocument">The document that owns the input references.</param>
    /// <param name="sourceNode">The original array node whose elements supply unmodified reference values.</param>
    /// <returns>The converted schema list, or <see langword="null"/> when no list is supplied.</returns>
    private static IList<IArazzoInput>? ConvertSchemaList(
        IList<IOpenApiSchema>? source,
        ArazzoDocument? hostDocument,
        JsonNode? sourceNode = null)
    {
        if (source is null)
        {
            return null;
        }

        var sourceArray = sourceNode as JsonArray;
        return source.Select((schema, index) => ConvertFromOpenApiSchema(schema, hostDocument,
            sourceArray is not null && index < sourceArray.Count ? sourceArray[index] : null)).ToList();
    }

    private static IDictionary<string, IArazzoExtension>? ConvertExtensions(IDictionary<string, IOpenApiExtension>? extensions)
    {
        if (extensions is null)
        {
            return null;
        }

        return extensions.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value switch
            {
                OpenApiJsonNodeExtension jsonNodeExtension => (IArazzoExtension)new JsonNodeExtension(ArazzoInput.CloneNode(jsonNodeExtension.Node)!),
                _ => new ArazzoExtensionAdapter(pair.Value)
            });
    }

    private sealed class ArazzoExtensionAdapter(IOpenApiExtension extension) : IArazzoExtension
    {
        public void Write(IOpenApiWriter writer, ArazzoSpecVersion specVersion)
        {
            extension.Write(writer, OpenApiSpecVersion.OpenApi3_2);
        }
    }

    private static class ArazzoInputReferenceFactory
    {
        internal static ArazzoInputReference Create(OpenApiSchemaReference schemaReference, ArazzoDocument? hostDocument, string? sourceReference)
        {
            var referenceId = sourceReference is null
                ? schemaReference.Reference.Id ?? throw new InvalidOperationException("Schema reference Id is required.")
                : V1.ArazzoV1Deserializer.GetReferenceId(sourceReference);
            var externalResource = sourceReference is null
                ? schemaReference.Reference.ExternalResource
                : V1.ArazzoV1Deserializer.GetExternalResource(sourceReference) ??
                    (sourceReference.StartsWith("#", StringComparison.Ordinal) ? null : schemaReference.Reference.ExternalResource);
            var reference = new ArazzoInputReference(referenceId, hostDocument, externalResource)
            {
                Title = schemaReference.Reference.Title,
                Description = schemaReference.Reference.Description,
                Default = ArazzoInput.CloneNode(schemaReference.Reference.Default),
                Examples = ArazzoInput.CloneNodeList(schemaReference.Reference.Examples),
                Extensions = ConvertExtensions(schemaReference.Reference.Extensions)
            };

            if (schemaReference.Reference.ReadOnly.HasValue)
            {
                reference.ReadOnly = schemaReference.Reference.ReadOnly.Value;
            }

            if (schemaReference.Reference.WriteOnly.HasValue)
            {
                reference.WriteOnly = schemaReference.Reference.WriteOnly.Value;
            }

            if (schemaReference.Reference.Deprecated.HasValue)
            {
                reference.Deprecated = schemaReference.Reference.Deprecated.Value;
            }

            var referenceValue = sourceReference ?? schemaReference.Reference.ReferenceV3;
            if (!string.IsNullOrEmpty(referenceValue))
            {
                reference.Reference.SetJsonPointerPath(referenceValue, "#");
            }

            return reference;
        }
    }
}