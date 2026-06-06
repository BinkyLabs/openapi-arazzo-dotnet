using System.Reflection;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Input reference object.
/// </summary>
internal class ArazzoInputReference : BaseArazzoReferenceHolder<ArazzoInput, IArazzoInput, BaseArazzoReference>, IArazzoInput
{
    private string? _description;
    private string? _title;
    private JsonNode? _default;
    private bool? _readOnly;
    private bool? _writeOnly;
    private IList<JsonNode>? _examples;
    private bool? _deprecated;
    private IDictionary<string, IArazzoExtension>? _extensions;

    /// <summary>
    /// Constructor initializing the reference object.
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <param name="hostDocument">The host document.</param>
    /// <param name="externalResource">The external resource.</param>
    public ArazzoInputReference(string referenceId, ArazzoDocument? hostDocument = null, string? externalResource = null)
        : base(referenceId, hostDocument, ReferenceType.Input, externalResource)
    {
    }
    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="reference">The reference to copy.</param>
    public ArazzoInputReference(ArazzoInputReference reference)
        : base(reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        _description = reference._description;
        _title = reference._title;
        _default = ArazzoInput.CloneNode(reference._default);
        _readOnly = reference._readOnly;
        _writeOnly = reference._writeOnly;
        _examples = ArazzoInput.CloneNodeList(reference._examples);
        _deprecated = reference._deprecated;
        _extensions = ArazzoInput.CloneArazzoExtensions(reference._extensions);
    }

    /// <inheritdoc />
    public string? Title
    {
        get => _title ?? Target?.Title;
        set => _title = value;
    }

    /// <inheritdoc />
    public Uri? Schema
    {
        get => Target?.Schema;
        set => ThrowUnsupportedOverride(nameof(Schema));
    }

    /// <inheritdoc />
    public string? Id
    {
        get => Target?.Id;
        set => ThrowUnsupportedOverride(nameof(Id));
    }

    /// <inheritdoc />
    public string? Comment
    {
        get => Target?.Comment;
        set => ThrowUnsupportedOverride(nameof(Comment));
    }

    /// <inheritdoc />
    public IDictionary<string, bool>? Vocabulary
    {
        get => Target?.Vocabulary;
        set => ThrowUnsupportedOverride(nameof(Vocabulary));
    }

    /// <inheritdoc />
    public string? DynamicRef
    {
        get => Target?.DynamicRef;
        set => ThrowUnsupportedOverride(nameof(DynamicRef));
    }

    /// <inheritdoc />
    public string? DynamicAnchor
    {
        get => Target?.DynamicAnchor;
        set => ThrowUnsupportedOverride(nameof(DynamicAnchor));
    }

    /// <inheritdoc />
    public IDictionary<string, IArazzoInput>? Definitions
    {
        get => Target?.Definitions;
        set => ThrowUnsupportedOverride(nameof(Definitions));
    }

    /// <inheritdoc />
    public string? ExclusiveMaximum
    {
        get => Target?.ExclusiveMaximum;
        set => ThrowUnsupportedOverride(nameof(ExclusiveMaximum));
    }

    /// <inheritdoc />
    public string? ExclusiveMinimum
    {
        get => Target?.ExclusiveMinimum;
        set => ThrowUnsupportedOverride(nameof(ExclusiveMinimum));
    }

    /// <inheritdoc />
    public JsonSchemaType? Type
    {
        get => Target?.Type;
        set => ThrowUnsupportedOverride(nameof(Type));
    }

    /// <inheritdoc />
    public string? Const
    {
        get => Target?.Const;
        set => ThrowUnsupportedOverride(nameof(Const));
    }

    /// <inheritdoc />
    public string? Format
    {
        get => Target?.Format;
        set => ThrowUnsupportedOverride(nameof(Format));
    }

    /// <inheritdoc />
    public string? Description
    {
        get => _description ?? Target?.Description;
        set => _description = value;
    }

    /// <inheritdoc />
    public string? Maximum
    {
        get => Target?.Maximum;
        set => ThrowUnsupportedOverride(nameof(Maximum));
    }

    /// <inheritdoc />
    public string? Minimum
    {
        get => Target?.Minimum;
        set => ThrowUnsupportedOverride(nameof(Minimum));
    }

    /// <inheritdoc />
    public int? MaxLength
    {
        get => Target?.MaxLength;
        set => ThrowUnsupportedOverride(nameof(MaxLength));
    }

    /// <inheritdoc />
    public int? MinLength
    {
        get => Target?.MinLength;
        set => ThrowUnsupportedOverride(nameof(MinLength));
    }

    /// <inheritdoc />
    public string? Pattern
    {
        get => Target?.Pattern;
        set => ThrowUnsupportedOverride(nameof(Pattern));
    }

    /// <inheritdoc />
    public decimal? MultipleOf
    {
        get => Target?.MultipleOf;
        set => ThrowUnsupportedOverride(nameof(MultipleOf));
    }

    /// <inheritdoc />
    public JsonNode? Default
    {
        get => _default ?? Target?.Default;
        set => _default = value;
    }

    /// <inheritdoc />
    public bool ReadOnly
    {
        get => _readOnly ?? Target?.ReadOnly ?? false;
        set => _readOnly = value;
    }

    /// <inheritdoc />
    public bool WriteOnly
    {
        get => _writeOnly ?? Target?.WriteOnly ?? false;
        set => _writeOnly = value;
    }

    /// <inheritdoc />
    public IList<IArazzoInput>? AllOf
    {
        get => Target?.AllOf;
        set => ThrowUnsupportedOverride(nameof(AllOf));
    }

    /// <inheritdoc />
    public IList<IArazzoInput>? OneOf
    {
        get => Target?.OneOf;
        set => ThrowUnsupportedOverride(nameof(OneOf));
    }

    /// <inheritdoc />
    public IList<IArazzoInput>? AnyOf
    {
        get => Target?.AnyOf;
        set => ThrowUnsupportedOverride(nameof(AnyOf));
    }

    /// <inheritdoc />
    public IArazzoInput? Not
    {
        get => Target?.Not;
        set => ThrowUnsupportedOverride(nameof(Not));
    }

    /// <inheritdoc />
    public ISet<string>? Required
    {
        get => Target?.Required;
        set => ThrowUnsupportedOverride(nameof(Required));
    }

    /// <inheritdoc />
    public IArazzoInput? Items
    {
        get => Target?.Items;
        set => ThrowUnsupportedOverride(nameof(Items));
    }

    /// <inheritdoc />
    public int? MaxItems
    {
        get => Target?.MaxItems;
        set => ThrowUnsupportedOverride(nameof(MaxItems));
    }

    /// <inheritdoc />
    public int? MinItems
    {
        get => Target?.MinItems;
        set => ThrowUnsupportedOverride(nameof(MinItems));
    }

    /// <inheritdoc />
    public bool? UniqueItems
    {
        get => Target?.UniqueItems;
        set => ThrowUnsupportedOverride(nameof(UniqueItems));
    }

    /// <inheritdoc />
    public IDictionary<string, IArazzoInput>? Properties
    {
        get => Target?.Properties;
        set => ThrowUnsupportedOverride(nameof(Properties));
    }

    /// <inheritdoc />
    public IDictionary<string, IArazzoInput>? PatternProperties
    {
        get => Target?.PatternProperties;
        set => ThrowUnsupportedOverride(nameof(PatternProperties));
    }

    /// <inheritdoc />
    public int? MaxProperties
    {
        get => Target?.MaxProperties;
        set => ThrowUnsupportedOverride(nameof(MaxProperties));
    }

    /// <inheritdoc />
    public int? MinProperties
    {
        get => Target?.MinProperties;
        set => ThrowUnsupportedOverride(nameof(MinProperties));
    }

    /// <inheritdoc />
    public bool AdditionalPropertiesAllowed
    {
        get => Target?.AdditionalPropertiesAllowed ?? true;
        set => ThrowUnsupportedOverride(nameof(AdditionalPropertiesAllowed));
    }

    /// <inheritdoc />
    public IArazzoInput? AdditionalProperties
    {
        get => Target?.AdditionalProperties;
        set => ThrowUnsupportedOverride(nameof(AdditionalProperties));
    }

    /// <inheritdoc />
    public IList<JsonNode>? Examples
    {
        get => _examples ?? Target?.Examples;
        set => _examples = value;
    }

    /// <inheritdoc />
    public IList<JsonNode>? Enum
    {
        get => Target?.Enum;
        set => ThrowUnsupportedOverride(nameof(Enum));
    }

    /// <inheritdoc />
    public bool UnevaluatedProperties
    {
        get => Target?.UnevaluatedProperties ?? true;
        set => ThrowUnsupportedOverride(nameof(UnevaluatedProperties));
    }

    /// <inheritdoc />
    public IArazzoInput? UnevaluatedPropertiesSchema
    {
        get => Target?.UnevaluatedPropertiesSchema;
        set => ThrowUnsupportedOverride(nameof(UnevaluatedPropertiesSchema));
    }

    /// <inheritdoc />
    public bool Deprecated
    {
        get => _deprecated ?? Target?.Deprecated ?? false;
        set => _deprecated = value;
    }

    /// <inheritdoc />
    public IDictionary<string, HashSet<string>>? DependentRequired
    {
        get => Target?.DependentRequired;
        set => ThrowUnsupportedOverride(nameof(DependentRequired));
    }

    /// <inheritdoc />
    public IDictionary<string, IArazzoExtension>? Extensions
    {
        get => _extensions ?? Target?.Extensions;
        set => _extensions = value;
    }

    /// <inheritdoc />
    public override IArazzoInput CopyReferenceAsTargetElementWithOverrides(IArazzoInput source)
    {
        if (source is ArazzoInput input)
        {
            return new ArazzoInput(input, this);
        }

        return source;
    }

    /// <inheritdoc />
    public override void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ToOpenApiSchemaReference().SerializeAsV32(writer);
    }

    /// <summary>
    /// Sets metadata annotations from the source JSON object.
    /// </summary>
    /// <param name="jsonObject">The source object.</param>
    internal void SetMetadataFromJsonObject(JsonObject jsonObject)
    {
        ArgumentNullException.ThrowIfNull(jsonObject);

        var title = BaseArazzoReference.GetPropertyValueFromNode(jsonObject, OpenApiConstants.Title);
        if (!string.IsNullOrEmpty(title))
        {
            Title = title;
        }

        var description = BaseArazzoReference.GetPropertyValueFromNode(jsonObject, OpenApiConstants.Description);
        if (!string.IsNullOrEmpty(description))
        {
            Description = description;
        }

        if (jsonObject.TryGetPropertyValue(OpenApiConstants.ReadOnly, out var readOnlyNode) &&
            readOnlyNode is JsonValue readOnlyValue &&
            readOnlyValue.TryGetValue<bool>(out var readOnly))
        {
            ReadOnly = readOnly;
        }

        if (jsonObject.TryGetPropertyValue(OpenApiConstants.WriteOnly, out var writeOnlyNode) &&
            writeOnlyNode is JsonValue writeOnlyValue &&
            writeOnlyValue.TryGetValue<bool>(out var writeOnly))
        {
            WriteOnly = writeOnly;
        }

        if (jsonObject.TryGetPropertyValue(OpenApiConstants.Deprecated, out var deprecatedNode) &&
            deprecatedNode is JsonValue deprecatedValue &&
            deprecatedValue.TryGetValue<bool>(out var deprecated))
        {
            Deprecated = deprecated;
        }

        if (jsonObject.TryGetPropertyValue(OpenApiConstants.Default, out var defaultNode))
        {
            Default = defaultNode?.DeepClone();
        }

        if (jsonObject.TryGetPropertyValue(OpenApiConstants.Examples, out var examplesNode) &&
            examplesNode is JsonArray examplesArray)
        {
            Examples = examplesArray.OfType<JsonNode>().Select(static node => node.DeepClone()).ToList();
        }

        foreach (var property in jsonObject.Where(static p => p.Key.StartsWith(OpenApiConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase) &&
                                                              p.Value is not null))
        {
            Extensions ??= new Dictionary<string, IArazzoExtension>(StringComparer.OrdinalIgnoreCase);
            Extensions[property.Key] = new JsonNodeExtension(property.Value!.DeepClone());
        }
    }

    internal OpenApiSchemaReference ToOpenApiSchemaReference()
    {
        var schemaReference = new OpenApiSchemaReference(Reference.Id ?? throw new InvalidOperationException("Reference Id is required."), null, Reference.ExternalResource)
        {
            Title = _title,
            Description = _description,
            Default = ArazzoInput.CloneNode(_default),
            Examples = ArazzoInput.CloneNodeList(_examples),
            Extensions = ArazzoInput.ConvertToOpenApiExtensions(_extensions)
        };

        if (_readOnly.HasValue)
        {
            schemaReference.ReadOnly = _readOnly.Value;
        }

        if (_writeOnly.HasValue)
        {
            schemaReference.WriteOnly = _writeOnly.Value;
        }

        if (_deprecated.HasValue)
        {
            schemaReference.Deprecated = _deprecated.Value;
        }

        if (!string.IsNullOrEmpty(Reference.ReferenceV1))
        {
            ReferenceV3Property.SetValue(schemaReference.Reference, Reference.ReferenceV1);
        }

        return schemaReference;
    }

    /// <inheritdoc />
    protected override BaseArazzoReference CopyReference(BaseArazzoReference sourceReference)
    {
        return new BaseArazzoReference(sourceReference);
    }

    private static void ThrowUnsupportedOverride(string propertyName)
    {
        throw new NotSupportedException($"Property '{propertyName}' cannot be overridden on {nameof(ArazzoInputReference)}.");
    }

    private static PropertyInfo ReferenceV3Property { get; } =
        typeof(BaseOpenApiReference).GetProperty(nameof(BaseOpenApiReference.ReferenceV3), BindingFlags.Instance | BindingFlags.Public)
        ?? throw new InvalidOperationException($"Could not find {nameof(BaseOpenApiReference.ReferenceV3)} on {nameof(BaseOpenApiReference)}.");
}