using System.Text.Json.Nodes;

using Microsoft.OpenApi;

using OpenApiJsonNodeExtension = Microsoft.OpenApi.JsonNodeExtension;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a JSON Schema-based input definition in an Arazzo document.
/// </summary>
public class ArazzoInput : IArazzoInput
{

    /// <summary>
    /// Initializes a new instance of the <see cref="ArazzoInput"/> class.
    /// </summary>
    public ArazzoInput()
    {
    }

    internal ArazzoInput(IArazzoInput source, ArazzoInputReference overrides)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(overrides);

        Title = overrides.Title ?? source.Title;
        Schema = source.Schema;
        Id = source.Id;
        Comment = source.Comment;
        Vocabulary = source.Vocabulary is null ? null : new Dictionary<string, bool>(source.Vocabulary);
        DynamicRef = source.DynamicRef;
        DynamicAnchor = source.DynamicAnchor;
        Definitions = source.Definitions is null ? null : new Dictionary<string, IArazzoInput>(source.Definitions);
        Anchor = source.Anchor;
        ExclusiveMaximum = source.ExclusiveMaximum;
        ExclusiveMinimum = source.ExclusiveMinimum;
        Type = source.Type;
        Const = source.Const;
        Format = source.Format;
        Description = overrides.Description ?? source.Description;
        Maximum = source.Maximum;
        Minimum = source.Minimum;
        MaxLength = source.MaxLength;
        MinLength = source.MinLength;
        Pattern = source.Pattern;
        MultipleOf = source.MultipleOf;
        Default = overrides.Default is null ? CloneNode(source.Default) : CloneNode(overrides.Default);
        ReadOnly = overrides.ReadOnly;
        WriteOnly = overrides.WriteOnly;
        AllOf = source.AllOf is null ? null : [.. source.AllOf];
        OneOf = source.OneOf is null ? null : [.. source.OneOf];
        AnyOf = source.AnyOf is null ? null : [.. source.AnyOf];
        Not = source.Not;
        Required = source.Required is null ? null : new HashSet<string>(source.Required);
        Items = source.Items;
        MaxItems = source.MaxItems;
        MinItems = source.MinItems;
        UniqueItems = source.UniqueItems;
        Contains = source.Contains;
        MaxContains = source.MaxContains;
        MinContains = source.MinContains;
        Properties = source.Properties is null ? null : new Dictionary<string, IArazzoInput>(source.Properties);
        PatternProperties = source.PatternProperties is null ? null : new Dictionary<string, IArazzoInput>(source.PatternProperties);
        MaxProperties = source.MaxProperties;
        MinProperties = source.MinProperties;
        AdditionalPropertiesAllowed = source.AdditionalPropertiesAllowed;
        AdditionalProperties = source.AdditionalProperties;
        Examples = overrides.Examples is null ? CloneNodeList(source.Examples) : CloneNodeList(overrides.Examples);
        Enum = CloneNodeList(source.Enum);
        UnevaluatedProperties = source.UnevaluatedProperties;
        UnevaluatedPropertiesSchema = source.UnevaluatedPropertiesSchema;
        ContentEncoding = source.ContentEncoding;
        ContentMediaType = source.ContentMediaType;
        ContentSchema = source.ContentSchema;
        PropertyNames = source.PropertyNames;
        DependentSchemas = source.DependentSchemas is null ? null : new Dictionary<string, IArazzoInput>(source.DependentSchemas);
        If = source.If;
        Then = source.Then;
        Else = source.Else;
        Deprecated = overrides.Deprecated;
        DependentRequired = CloneDependentRequired(source.DependentRequired);
        Extensions = overrides.Extensions is null ? CloneArazzoExtensions(source.Extensions) : CloneArazzoExtensions(overrides.Extensions);
    }

    /// <inheritdoc />
    public string? Title { get; set; }

    /// <inheritdoc />
    public Uri? Schema { get; set; }

    /// <inheritdoc />
    public string? Id { get; set; }

    /// <inheritdoc />
    public string? Comment { get; set; }

    /// <inheritdoc />
    public IDictionary<string, bool>? Vocabulary { get; set; }

    /// <inheritdoc />
    public string? DynamicRef { get; set; }

    /// <inheritdoc />
    public string? DynamicAnchor { get; set; }

    /// <inheritdoc />
    public IDictionary<string, IArazzoInput>? Definitions { get; set; }

    /// <inheritdoc />
    public string? Anchor { get; set; }

    /// <inheritdoc />
    public string? ExclusiveMaximum { get; set; }

    /// <inheritdoc />
    public string? ExclusiveMinimum { get; set; }

    /// <inheritdoc />
    public JsonSchemaType? Type { get; set; }

    /// <inheritdoc />
    public string? Const { get; set; }

    /// <inheritdoc />
    public string? Format { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public string? Maximum { get; set; }

    /// <inheritdoc />
    public string? Minimum { get; set; }

    /// <inheritdoc />
    public int? MaxLength { get; set; }

    /// <inheritdoc />
    public int? MinLength { get; set; }

    /// <inheritdoc />
    public string? Pattern { get; set; }

    /// <inheritdoc />
    public decimal? MultipleOf { get; set; }

    /// <inheritdoc />
    public JsonNode? Default { get; set; }

    /// <inheritdoc />
    public bool ReadOnly { get; set; }

    /// <inheritdoc />
    public bool WriteOnly { get; set; }

    /// <inheritdoc />
    public IList<IArazzoInput>? AllOf { get; set; }

    /// <inheritdoc />
    public IList<IArazzoInput>? OneOf { get; set; }

    /// <inheritdoc />
    public IList<IArazzoInput>? AnyOf { get; set; }

    /// <inheritdoc />
    public IArazzoInput? Not { get; set; }

    /// <inheritdoc />
    public ISet<string>? Required { get; set; }

    /// <inheritdoc />
    public IArazzoInput? Items { get; set; }

    /// <inheritdoc />
    public int? MaxItems { get; set; }

    /// <inheritdoc />
    public int? MinItems { get; set; }

    /// <inheritdoc />
    public bool? UniqueItems { get; set; }

    /// <inheritdoc />
    public IArazzoInput? Contains { get; set; }

    /// <inheritdoc />
    public uint? MaxContains { get; set; }

    /// <inheritdoc />
    public uint? MinContains { get; set; }

    /// <inheritdoc />
    public IDictionary<string, IArazzoInput>? Properties { get; set; }

    /// <inheritdoc />
    public IDictionary<string, IArazzoInput>? PatternProperties { get; set; }

    /// <inheritdoc />
    public int? MaxProperties { get; set; }

    /// <inheritdoc />
    public int? MinProperties { get; set; }

    /// <inheritdoc />
    public bool AdditionalPropertiesAllowed { get; set; } = true;

    /// <inheritdoc />
    public IArazzoInput? AdditionalProperties { get; set; }

    /// <inheritdoc />
    public IList<JsonNode>? Examples { get; set; }

    /// <inheritdoc />
    public IList<JsonNode>? Enum { get; set; }

    /// <inheritdoc />
    public bool UnevaluatedProperties { get; set; } = true;

    /// <inheritdoc />
    public IArazzoInput? UnevaluatedPropertiesSchema { get; set; }

    /// <inheritdoc />
    public string? ContentEncoding { get; set; }

    /// <inheritdoc />
    public string? ContentMediaType { get; set; }

    /// <inheritdoc />
    public IArazzoInput? ContentSchema { get; set; }

    /// <inheritdoc />
    public IArazzoInput? PropertyNames { get; set; }

    /// <inheritdoc />
    public IDictionary<string, IArazzoInput>? DependentSchemas { get; set; }

    /// <inheritdoc />
    public IArazzoInput? If { get; set; }

    /// <inheritdoc />
    public IArazzoInput? Then { get; set; }

    /// <inheritdoc />
    public IArazzoInput? Else { get; set; }

    /// <inheritdoc />
    public bool Deprecated { get; set; }

    /// <inheritdoc />
    public IDictionary<string, HashSet<string>>? DependentRequired { get; set; }

    /// <inheritdoc />
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <inheritdoc />
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ConvertToOpenApiSchema(this).SerializeAsV32(writer);
    }

    /// <summary>
    /// Converts an OpenAPI schema to an Arazzo input.
    /// </summary>
    /// <param name="value">The OpenAPI schema to convert.</param>
    public static implicit operator ArazzoInput?(OpenApiSchema? value)
    {
        if (value is null)
        {
            return null;
        }

        ValidateUnsupportedOpenApiKeywords(value);

        return ConvertFromOpenApiSchema(value) as ArazzoInput;
    }

    /// <summary>
    /// Converts an Arazzo input to an OpenAPI schema.
    /// </summary>
    /// <param name="value">The Arazzo input to convert.</param>
    public static implicit operator OpenApiSchema?(ArazzoInput? value)
    {
        if (value is null)
        {
            return null;
        }

        return (OpenApiSchema)ConvertToOpenApiSchema(value);
    }

    internal static IArazzoInput ConvertFromOpenApiSchema(IOpenApiSchema schema, ArazzoDocument? hostDocument = null)
    {
        if (schema is OpenApiSchemaReference schemaReference)
        {
            return ArazzoInputReferenceFactory.Create(schemaReference, hostDocument);
        }

        if (schema is not OpenApiSchema openApiSchema)
        {
            throw new NotSupportedException($"Conversion from {schema.GetType().Name} is not supported.");
        }

        ValidateUnsupportedOpenApiKeywords(openApiSchema);

        return new ArazzoInput
        {
            Title = schema.Title,
            Schema = schema.Schema,
            Id = schema.Id,
            Comment = schema.Comment,
            Vocabulary = schema.Vocabulary is null ? null : new Dictionary<string, bool>(schema.Vocabulary),
            DynamicRef = schema.DynamicRef,
            DynamicAnchor = schema.DynamicAnchor,
            Definitions = ConvertSchemaMap(schema.Definitions, hostDocument),
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
            Default = CloneNode(schema.Default),
            ReadOnly = schema.ReadOnly,
            WriteOnly = schema.WriteOnly,
            AllOf = ConvertSchemaList(schema.AllOf, hostDocument),
            OneOf = ConvertSchemaList(schema.OneOf, hostDocument),
            AnyOf = ConvertSchemaList(schema.AnyOf, hostDocument),
            Not = schema.Not is null ? null : ConvertFromOpenApiSchema(schema.Not, hostDocument),
            Required = schema.Required is null ? null : new HashSet<string>(schema.Required),
            Items = schema.Items is null ? null : ConvertFromOpenApiSchema(schema.Items, hostDocument),
            MaxItems = schema.MaxItems,
            MinItems = schema.MinItems,
            UniqueItems = schema.UniqueItems,
            Contains = openApiSchema.Contains is null ? null : ConvertFromOpenApiSchema(openApiSchema.Contains, hostDocument),
            MaxContains = openApiSchema.MaxContains,
            MinContains = openApiSchema.MinContains,
            Properties = ConvertSchemaMap(schema.Properties, hostDocument),
            PatternProperties = ConvertSchemaMap(schema.PatternProperties, hostDocument),
            MaxProperties = schema.MaxProperties,
            MinProperties = schema.MinProperties,
            AdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed,
            AdditionalProperties = schema.AdditionalProperties is null ? null : ConvertFromOpenApiSchema(schema.AdditionalProperties, hostDocument),
            Examples = CloneNodeList(schema.Examples),
            Enum = CloneNodeList(schema.Enum),
            UnevaluatedProperties = openApiSchema.UnevaluatedProperties,
            UnevaluatedPropertiesSchema = openApiSchema.UnevaluatedPropertiesSchema is null ? null : ConvertFromOpenApiSchema(openApiSchema.UnevaluatedPropertiesSchema, hostDocument),
            ContentEncoding = openApiSchema.ContentEncoding,
            ContentMediaType = openApiSchema.ContentMediaType,
            ContentSchema = openApiSchema.ContentSchema is null ? null : ConvertFromOpenApiSchema(openApiSchema.ContentSchema, hostDocument),
            PropertyNames = openApiSchema.PropertyNames is null ? null : ConvertFromOpenApiSchema(openApiSchema.PropertyNames, hostDocument),
            DependentSchemas = ConvertSchemaMap(openApiSchema.DependentSchemas, hostDocument),
            If = openApiSchema.If is null ? null : ConvertFromOpenApiSchema(openApiSchema.If, hostDocument),
            Then = openApiSchema.Then is null ? null : ConvertFromOpenApiSchema(openApiSchema.Then, hostDocument),
            Else = openApiSchema.Else is null ? null : ConvertFromOpenApiSchema(openApiSchema.Else, hostDocument),
            Deprecated = schema.Deprecated,
            Extensions = ConvertExtensions(openApiSchema.Extensions),
            DependentRequired = CloneDependentRequired(schema.DependentRequired)
        };
    }

    internal static IOpenApiSchema ConvertToOpenApiSchema(IArazzoInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input is ArazzoInputReference reference)
        {
            return reference.ToOpenApiSchemaReference();
        }

        var schema = new OpenApiSchema
        {
            Title = input.Title,
            Schema = input.Schema,
            Id = input.Id,
            Comment = input.Comment,
            Vocabulary = input.Vocabulary is null ? null : new Dictionary<string, bool>(input.Vocabulary),
            DynamicRef = input.DynamicRef,
            DynamicAnchor = input.DynamicAnchor,
            Anchor = input.Anchor,
            Definitions = ConvertSchemaMap(input.Definitions),
            ExclusiveMaximum = input.ExclusiveMaximum,
            ExclusiveMinimum = input.ExclusiveMinimum,
            Type = input.Type,
            Const = input.Const,
            Format = input.Format,
            Description = input.Description,
            Maximum = input.Maximum,
            Minimum = input.Minimum,
            MaxLength = input.MaxLength,
            MinLength = input.MinLength,
            Pattern = input.Pattern,
            MultipleOf = input.MultipleOf,
            Default = CloneNode(input.Default),
            ReadOnly = input.ReadOnly,
            WriteOnly = input.WriteOnly,
            AllOf = ConvertSchemaList(input.AllOf),
            OneOf = ConvertSchemaList(input.OneOf),
            AnyOf = ConvertSchemaList(input.AnyOf),
            Not = input.Not is null ? null : ConvertToOpenApiSchema(input.Not),
            Required = input.Required is null ? null : new HashSet<string>(input.Required),
            Items = input.Items is null ? null : ConvertToOpenApiSchema(input.Items),
            MaxItems = input.MaxItems,
            MinItems = input.MinItems,
            UniqueItems = input.UniqueItems,
            Contains = input.Contains is null ? null : ConvertToOpenApiSchema(input.Contains),
            MaxContains = input.MaxContains,
            MinContains = input.MinContains,
            Properties = ConvertSchemaMap(input.Properties),
            PatternProperties = ConvertSchemaMap(input.PatternProperties),
            MaxProperties = input.MaxProperties,
            MinProperties = input.MinProperties,
            AdditionalPropertiesAllowed = input.AdditionalPropertiesAllowed,
            AdditionalProperties = input.AdditionalProperties is null ? null : ConvertToOpenApiSchema(input.AdditionalProperties),
            Examples = CloneNodeList(input.Examples),
            Enum = CloneNodeList(input.Enum),
            UnevaluatedProperties = input.UnevaluatedProperties,
            UnevaluatedPropertiesSchema = input.UnevaluatedPropertiesSchema is null ? null : ConvertToOpenApiSchema(input.UnevaluatedPropertiesSchema),
            ContentEncoding = input.ContentEncoding,
            ContentMediaType = input.ContentMediaType,
            ContentSchema = input.ContentSchema is null ? null : ConvertToOpenApiSchema(input.ContentSchema),
            PropertyNames = input.PropertyNames is null ? null : ConvertToOpenApiSchema(input.PropertyNames),
            DependentSchemas = ConvertSchemaMap(input.DependentSchemas),
            If = input.If is null ? null : ConvertToOpenApiSchema(input.If),
            Then = input.Then is null ? null : ConvertToOpenApiSchema(input.Then),
            Else = input.Else is null ? null : ConvertToOpenApiSchema(input.Else),
            Deprecated = input.Deprecated,
            Extensions = ConvertToOpenApiExtensions(input.Extensions),
            DependentRequired = CloneDependentRequired(input.DependentRequired)
        };

        return schema;
    }

    private static void ValidateUnsupportedOpenApiKeywords(OpenApiSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var unsupportedKeywords = new List<string>();

        if (schema.Discriminator is not null)
        {
            unsupportedKeywords.Add(nameof(schema.Discriminator));
        }

        if (schema.Example is not null)
        {
            unsupportedKeywords.Add(nameof(schema.Example));
        }

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

    private static IDictionary<string, IArazzoInput>? ConvertSchemaMap(
        IDictionary<string, IOpenApiSchema>? source,
        ArazzoDocument? hostDocument)
    {
        if (source is null)
        {
            return null;
        }

        return source.ToDictionary(static pair => pair.Key, pair => ConvertFromOpenApiSchema(pair.Value, hostDocument));
    }

    private static IDictionary<string, IOpenApiSchema>? ConvertSchemaMap(
        IDictionary<string, IArazzoInput>? source)
    {
        if (source is null)
        {
            return null;
        }

        return source.ToDictionary(static pair => pair.Key, static pair => (IOpenApiSchema)ConvertToOpenApiSchema(pair.Value));
    }

    private static IList<IArazzoInput>? ConvertSchemaList(
        IList<IOpenApiSchema>? source,
        ArazzoDocument? hostDocument)
    {
        if (source is null)
        {
            return null;
        }

        return source.Select(schema => ConvertFromOpenApiSchema(schema, hostDocument)).ToList();
    }

    private static IList<IOpenApiSchema>? ConvertSchemaList(
        IList<IArazzoInput>? source)
    {
        if (source is null)
        {
            return null;
        }

        return source.Select(static schema => (IOpenApiSchema)ConvertToOpenApiSchema(schema)).ToList();
    }

    internal static JsonNode? CloneNode(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }
        return JsonNode.Parse(node.ToJsonString());
    }

    internal static IList<JsonNode>? CloneNodeList(IList<JsonNode>? nodes)
    {
        if (nodes is null)
        {
            return null;
        }

        return nodes.Select(static node => CloneNode(node)!).ToList();
    }

    internal static IDictionary<string, HashSet<string>>? CloneDependentRequired(IDictionary<string, HashSet<string>>? dependentRequired)
    {
        if (dependentRequired is null)
        {
            return null;
        }

        return dependentRequired.ToDictionary(static pair => pair.Key, static pair => new HashSet<string>(pair.Value));
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
                OpenApiJsonNodeExtension jsonNodeExtension => (IArazzoExtension)new JsonNodeExtension(CloneNode(jsonNodeExtension.Node)!),
                _ => new ArazzoExtensionAdapter(pair.Value)
            });
    }

    internal static IDictionary<string, IOpenApiExtension>? ConvertToOpenApiExtensions(IDictionary<string, IArazzoExtension>? extensions)
    {
        if (extensions is null)
        {
            return null;
        }

        return extensions.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value switch
            {
                JsonNodeExtension jsonNodeExtension => (IOpenApiExtension)new OpenApiJsonNodeExtension(CloneNode(jsonNodeExtension.Node)!),
                _ => new OpenApiExtensionAdapter(pair.Value)
            });
    }

    private sealed class ArazzoExtensionAdapter(IOpenApiExtension extension) : IArazzoExtension
    {
        public void Write(IOpenApiWriter writer, ArazzoSpecVersion specVersion)
        {
            extension.Write(writer, OpenApiSpecVersion.OpenApi3_2);
        }
    }

    private sealed class OpenApiExtensionAdapter(IArazzoExtension extension) : IOpenApiExtension
    {
        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
        {
            extension.Write(writer, ArazzoSpecVersion.Arazzo1_0);
        }
    }

    internal static IDictionary<string, IArazzoExtension>? CloneArazzoExtensions(IDictionary<string, IArazzoExtension>? extensions)
    {
        if (extensions is null)
        {
            return null;
        }

        return extensions.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value is JsonNodeExtension jsonNodeExtension
                ? (IArazzoExtension)new JsonNodeExtension(CloneNode(jsonNodeExtension.Node)!)
                : pair.Value);
    }

    private static class ArazzoInputReferenceFactory
    {
        internal static ArazzoInputReference Create(OpenApiSchemaReference schemaReference, ArazzoDocument? hostDocument)
        {
            var referenceId = schemaReference.Reference.Id ?? throw new InvalidOperationException("Schema reference Id is required.");
            var reference = new ArazzoInputReference(referenceId, hostDocument, schemaReference.Reference.ExternalResource)
            {
                Title = schemaReference.Reference.Title,
                Description = schemaReference.Reference.Description,
                Default = CloneNode(schemaReference.Reference.Default),
                Examples = CloneNodeList(schemaReference.Reference.Examples),
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

            if (!string.IsNullOrEmpty(schemaReference.Reference.ReferenceV3))
            {
                reference.Reference.SetJsonPointerPath(schemaReference.Reference.ReferenceV3!, "#");
            }

            return reference;
        }
    }

}