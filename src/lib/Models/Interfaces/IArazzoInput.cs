using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a JSON Schema-based input definition in an Arazzo document.
/// </summary>
public interface IArazzoInput : IArazzoReferenceable, IArazzoExtensible
{
    /// <summary>
    /// Follow JSON Schema definition. Short text providing information about the data.
    /// </summary>
    string? Title { get; set; }

    /// <summary>
    /// $schema, a JSON Schema dialect identifier. Value must be a URI
    /// </summary>
    Uri? Schema { get; set; }

    /// <summary>
    /// $id - Identifies a schema resource with its canonical URI.
    /// </summary>
    string? Id { get; set; }

    /// <summary>
    /// $comment - reserves a location for comments from schema authors to readers or maintainers of the schema.
    /// </summary>
    string? Comment { get; set; }

    /// <summary>
    /// $vocabulary- used in meta-schemas to identify the vocabularies available for use in schemas described by that meta-schema.
    /// </summary>
    IDictionary<string, bool>? Vocabulary { get; set; }

    /// <summary>
    /// $dynamicRef - an applicator that allows for deferring the full resolution until runtime, at which point it is resolved each time it is encountered while evaluating an instance
    /// </summary>
    string? DynamicRef { get; set; }

    /// <summary>
    /// $dynamicAnchor - used to create plain name fragments that are not tied to any particular structural location for referencing purposes, which are taken into consideration for dynamic referencing.
    /// </summary>
    string? DynamicAnchor { get; set; }

    /// <summary>
    /// $defs - reserves a location for schema authors to inline re-usable JSON Schemas into a more general schema.
    /// The keyword does not directly affect the validation result
    /// </summary>
    IDictionary<string, IArazzoInput>? Definitions { get; set; }

    /// <summary>
    /// $anchor - identifies a plain-name location-independent fragment within the schema resource.
    /// </summary>
    string? Anchor { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    string? ExclusiveMaximum { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    string? ExclusiveMinimum { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// Value MUST be a string in V2 and V3.
    /// </summary>
    JsonSchemaType? Type { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    string? Const { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// While relying on JSON Schema's defined formats,
    /// the OAS offers a few additional predefined formats.
    /// </summary>
    string? Format { get; set; }

    /// <summary>
    /// Long description for the example.
    /// CommonMark syntax MAY be used for rich text representation.
    /// </summary>
    string? Description { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    string? Maximum { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    string? Minimum { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    int? MaxLength { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    int? MinLength { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// This string SHOULD be a valid regular expression, according to the ECMA 262 regular expression dialect
    /// </summary>
    string? Pattern { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    decimal? MultipleOf { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// The default value represents what would be assumed by the consumer of the input as the value of the schema if one is not provided.
    /// Unlike JSON Schema, the value MUST conform to the defined type for the Schema Object defined at the same level.
    /// For example, if type is string, then default can be "foo" but cannot be 1.
    /// You must use the <see cref="JsonNullSentinel.IsJsonNullSentinel(JsonNode?)"/> method to check whether Default was assigned a null value in the document.
    /// Assign <see cref="JsonNullSentinel.JsonNull"/> to use get null as a serialized value.
    /// </summary>
    JsonNode? Default { get; set; }

    /// <summary>
    /// Relevant only for Schema "properties" definitions. Declares the property as "read only".
    /// This means that it MAY be sent as part of a response but SHOULD NOT be sent as part of the request.
    /// If the property is marked as readOnly being true and is in the required list,
    /// the required will take effect on the response only.
    /// A property MUST NOT be marked as both readOnly and writeOnly being true.
    /// Default value is false.
    /// </summary>
    bool ReadOnly { get; set; }

    /// <summary>
    /// Relevant only for Schema "properties" definitions. Declares the property as "write only".
    /// Therefore, it MAY be sent as part of a request but SHOULD NOT be sent as part of the response.
    /// If the property is marked as writeOnly being true and is in the required list,
    /// the required will take effect on the request only.
    /// A property MUST NOT be marked as both readOnly and writeOnly being true.
    /// Default value is false.
    /// </summary>
    bool WriteOnly { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// Inline or referenced schema MUST be of a Schema Object and not a standard JSON Schema.
    /// </summary>
    IList<IArazzoInput>? AllOf { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// Inline or referenced schema MUST be of a Schema Object and not a standard JSON Schema.
    /// </summary>
    IList<IArazzoInput>? OneOf { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// Inline or referenced schema MUST be of a Schema Object and not a standard JSON Schema.
    /// </summary>
    IList<IArazzoInput>? AnyOf { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// Inline or referenced schema MUST be of a Schema Object and not a standard JSON Schema.
    /// </summary>
    IArazzoInput? Not { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    ISet<string>? Required { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// Value MUST be an object and not an array. Inline or referenced schema MUST be of a Schema Object
    /// and not a standard JSON Schema. items MUST be present if the type is array.
    /// </summary>
    IArazzoInput? Items { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    int? MaxItems { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    int? MinItems { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    bool? UniqueItems { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// Property definitions MUST be a Schema Object and not a standard JSON Schema (inline or referenced).
    /// </summary>
    IDictionary<string, IArazzoInput>? Properties { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// PatternProperty definitions MUST be a Schema Object and not a standard JSON Schema (inline or referenced)
    /// Each property name of this object SHOULD be a valid regular expression according to the ECMA 262 regular expression dialect.
    /// Each property value of this object MUST be an object, and each object MUST be a valid Schema Object not a standard JSON Schema.
    /// </summary>
    IDictionary<string, IArazzoInput>? PatternProperties { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    int? MaxProperties { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    int? MinProperties { get; set; }

    /// <summary>
    /// Indicates if the schema can contain properties other than those defined by the properties map.
    /// </summary>
    bool AdditionalPropertiesAllowed { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// Value can be boolean or object. Inline or referenced schema
    /// MUST be of a Schema Object and not a standard JSON Schema.
    /// </summary>
    IArazzoInput? AdditionalProperties { get; set; }

    /// <summary>
    /// A free-form property to include examples of an instance for this schema.
    /// To represent examples that cannot be naturally represented in JSON or YAML,
    /// a list of values can be used to contain the examples with escaping where necessary.
    /// </summary>
    IList<JsonNode>? Examples { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// </summary>
    IList<JsonNode>? Enum { get; set; }

    /// <summary>
    /// Indicates whether unevaluated properties are allowed. When false, no unevaluated properties are permitted.
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-core#name-unevaluatedproperties
    /// Only serialized when false and UnevaluatedPropertiesSchema is null.
    /// </summary>
    bool UnevaluatedProperties { get; set; }

    /// <summary>
    /// The schema to apply to unevaluated properties when present.
    /// </summary>
    IArazzoInput? UnevaluatedPropertiesSchema { get; set; }

    /// <summary>
    /// contentEncoding - identifies the encoding of string content.
    /// </summary>
    string? ContentEncoding { get; set; }

    /// <summary>
    /// contentMediaType - identifies the media type of string content.
    /// </summary>
    string? ContentMediaType { get; set; }

    /// <summary>
    /// contentSchema - provides a schema that describes the decoded string content.
    /// </summary>
    IArazzoInput? ContentSchema { get; set; }

    /// <summary>
    /// propertyNames - provides a schema that validates property names.
    /// </summary>
    IArazzoInput? PropertyNames { get; set; }

    /// <summary>
    /// dependentSchemas - maps property names to schemas that are applied when that property is present.
    /// </summary>
    IDictionary<string, IArazzoInput>? DependentSchemas { get; set; }

    /// <summary>
    /// if - applies a conditional schema that determines whether <see cref="Then"/> or <see cref="Else"/> should be evaluated.
    /// </summary>
    IArazzoInput? If { get; set; }

    /// <summary>
    /// then - applies when <see cref="If"/> evaluates successfully.
    /// </summary>
    IArazzoInput? Then { get; set; }

    /// <summary>
    /// else - applies when <see cref="If"/> does not evaluate successfully.
    /// </summary>
    IArazzoInput? Else { get; set; }

    /// <summary>
    /// Specifies that a schema is deprecated and SHOULD be transitioned out of usage.
    /// Default value is false.
    /// </summary>
    bool Deprecated { get; set; }

    /// <summary>
    /// Follow JSON Schema definition:https://json-schema.org/draft/2020-12/json-schema-validation#section-6.5.4
    /// </summary>
    IDictionary<string, HashSet<string>>? DependentRequired { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-core#name-contains
    /// An array instance is valid against "contains" if at least one of its elements is valid against this schema.
    /// Inline or referenced schema MUST be of a Schema Object and not a standard JSON Schema.
    /// </summary>
    IArazzoInput? Contains { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// The number of elements matching the "contains" schema MUST be less than or equal to this value.
    /// </summary>
    uint? MaxContains { get; set; }

    /// <summary>
    /// Follow JSON Schema definition: https://json-schema.org/draft/2020-12/json-schema-validation
    /// The number of elements matching the "contains" schema MUST be greater than or equal to this value.
    /// </summary>
    uint? MinContains { get; set; }
}