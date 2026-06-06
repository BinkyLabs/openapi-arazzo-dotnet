using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoInputTests
{
    [Fact]
    public void SerializeAsV1_WritesJsonSchemaKeywords()
    {
        var input = new ArazzoInput
        {
            Schema = new Uri("https://json-schema.org/draft/2020-12/schema"),
            Id = "urn:test:input",
            Title = "customer",
            Description = "A customer payload",
            Type = JsonSchemaType.Object,
            Required = new HashSet<string> { "id" },
            Properties = new Dictionary<string, IArazzoInput>
            {
                ["id"] = new ArazzoInput { Type = JsonSchemaType.String }
            },
            PatternProperties = new Dictionary<string, IArazzoInput>
            {
                ["^x-"] = new ArazzoInput { Type = JsonSchemaType.String }
            },
            AdditionalPropertiesAllowed = false,
            UnevaluatedPropertiesSchema = new ArazzoInput { Type = JsonSchemaType.String },
            Examples =
            [
                JsonValue.Create("example-1")!
            ],
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-extra"] = new JsonNodeExtension(JsonValue.Create("ok")!)
            }
        };

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        input.SerializeAsV1(writer);

        var json = JsonNode.Parse(textWriter.ToString());

        Assert.Equal("https://json-schema.org/draft/2020-12/schema", json?["$schema"]?.GetValue<string>());
        Assert.Equal("urn:test:input", json?["$id"]?.GetValue<string>());
        Assert.Equal("customer", json?["title"]?.GetValue<string>());
        Assert.Equal("A customer payload", json?["description"]?.GetValue<string>());
        Assert.Equal("object", json?["type"]?.GetValue<string>());
        Assert.False(json?["additionalProperties"]?.GetValue<bool>());
        Assert.Equal("string", json?["unevaluatedProperties"]?["type"]?.GetValue<string>());
        Assert.Equal("string", json?["properties"]?["id"]?["type"]?.GetValue<string>());
        Assert.Equal("string", json?["patternProperties"]?["^x-"]?["type"]?.GetValue<string>());
        Assert.Equal("example-1", json?["examples"]?[0]?.GetValue<string>());
        Assert.Equal("ok", json?["x-extra"]?.GetValue<string>());
    }

    [Fact]
    public void SerializeAsV1_WithReference_WritesExactReferenceAndOverrides()
    {
        var input = new ArazzoInputReference("shared")
        {
            Title = "override",
            Description = "override description",
            Default = JsonValue.Create("guest"),
            ReadOnly = true,
            WriteOnly = true,
            Deprecated = true,
            Examples =
            [
                JsonValue.Create("guest")!
            ],
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-extra"] = new JsonNodeExtension(JsonValue.Create("value")!)
            }
        };

        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);

        input.SerializeAsV1(writer);

        var json = JsonNode.Parse(textWriter.ToString());

        Assert.Equal("$components.inputs.shared", json?["$ref"]?.GetValue<string>());
        Assert.Equal("override", json?["title"]?.GetValue<string>());
        Assert.Equal("override description", json?["description"]?.GetValue<string>());
        Assert.Equal("guest", json?["default"]?.GetValue<string>());
        Assert.True(json?["readOnly"]?.GetValue<bool>());
        Assert.True(json?["writeOnly"]?.GetValue<bool>());
        Assert.True(json?["deprecated"]?.GetValue<bool>());
        Assert.Equal("guest", json?["examples"]?[0]?.GetValue<string>());
        Assert.Equal("value", json?["x-extra"]?.GetValue<string>());
    }
    [Fact]
    public void ImplicitConversion_FromOpenApiSchema_CopiesJsonSchemaKeywords()
    {
        var schema = new OpenApiSchema
        {
            Schema = new Uri("https://json-schema.org/draft/2020-12/schema"),
            Id = "urn:test:schema",
            Title = "widget",
            Comment = "comment",
            Vocabulary = new Dictionary<string, bool>
            {
                ["https://json-schema.org/draft/2020-12/vocab/core"] = true
            },
            DynamicRef = "#item",
            DynamicAnchor = "item",
            Definitions = new Dictionary<string, IOpenApiSchema>
            {
                ["item"] = new OpenApiSchema { Type = JsonSchemaType.String }
            },
            ExclusiveMaximum = "100",
            ExclusiveMinimum = "1",
            Type = JsonSchemaType.Object,
            Const = "fixed",
            Format = "date-time",
            Description = "description",
            Maximum = "99",
            Minimum = "2",
            MaxLength = 10,
            MinLength = 2,
            Pattern = "^[A-Z]+$",
            MultipleOf = 5,
            Default = JsonValue.Create("default"),
            ReadOnly = true,
            WriteOnly = true,
            AllOf = [new OpenApiSchema { Type = JsonSchemaType.String }],
            OneOf = [new OpenApiSchema { Type = JsonSchemaType.Number }],
            AnyOf = [new OpenApiSchema { Type = JsonSchemaType.Integer }],
            Not = new OpenApiSchema { Type = JsonSchemaType.Null },
            Required = new HashSet<string> { "id" },
            Items = new OpenApiSchema { Type = JsonSchemaType.String },
            MaxItems = 3,
            MinItems = 1,
            UniqueItems = true,
            Properties = new Dictionary<string, IOpenApiSchema>
            {
                ["id"] = new OpenApiSchema { Type = JsonSchemaType.String }
            },
            PatternProperties = new Dictionary<string, IOpenApiSchema>
            {
                ["^x-"] = new OpenApiSchema { Type = JsonSchemaType.String }
            },
            MaxProperties = 5,
            MinProperties = 1,
            AdditionalPropertiesAllowed = false,
            AdditionalProperties = new OpenApiSchema { Type = JsonSchemaType.String },
            Examples = [JsonValue.Create("ex-1")!],
            Enum = [JsonValue.Create("a")!, JsonValue.Create("b")!],
            UnevaluatedProperties = false,
            UnevaluatedPropertiesSchema = new OpenApiSchema { Type = JsonSchemaType.Boolean },
            Deprecated = true,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["x-extra"] = new Microsoft.OpenApi.JsonNodeExtension(JsonValue.Create("value")!)
            },
            DependentRequired = new Dictionary<string, HashSet<string>>
            {
                ["a"] = ["b", "c"]
            }
        };

        ArazzoInput input = schema!;

        Assert.Equal(schema.Schema, input.Schema);
        Assert.Equal(schema.Id, input.Id);
        Assert.Equal(schema.Title, input.Title);
        Assert.Equal(schema.Comment, input.Comment);
        Assert.Equal(schema.Vocabulary, input.Vocabulary);
        Assert.Equal(schema.DynamicRef, input.DynamicRef);
        Assert.Equal(schema.DynamicAnchor, input.DynamicAnchor);
        Assert.Equal(JsonSchemaType.String, input.Definitions?["item"].Type);
        Assert.Equal(schema.ExclusiveMaximum, input.ExclusiveMaximum);
        Assert.Equal(schema.ExclusiveMinimum, input.ExclusiveMinimum);
        Assert.Equal(schema.Type, input.Type);
        Assert.Equal(schema.Const, input.Const);
        Assert.Equal(schema.Format, input.Format);
        Assert.Equal(schema.Description, input.Description);
        Assert.Equal(schema.Maximum, input.Maximum);
        Assert.Equal(schema.Minimum, input.Minimum);
        Assert.Equal(schema.MaxLength, input.MaxLength);
        Assert.Equal(schema.MinLength, input.MinLength);
        Assert.Equal(schema.Pattern, input.Pattern);
        Assert.Equal(schema.MultipleOf, input.MultipleOf);
        Assert.Equal("default", input.Default?.GetValue<string>());
        Assert.True(input.ReadOnly);
        Assert.True(input.WriteOnly);
        Assert.Equal(JsonSchemaType.String, input.AllOf?.Single().Type);
        Assert.Equal(JsonSchemaType.Number, input.OneOf?.Single().Type);
        Assert.Equal(JsonSchemaType.Integer, input.AnyOf?.Single().Type);
        Assert.Equal(JsonSchemaType.Null, input.Not?.Type);
        Assert.Contains("id", input.Required!);
        Assert.Equal(JsonSchemaType.String, input.Items?.Type);
        Assert.Equal(schema.MaxItems, input.MaxItems);
        Assert.Equal(schema.MinItems, input.MinItems);
        Assert.Equal(schema.UniqueItems, input.UniqueItems);
        Assert.Equal(JsonSchemaType.String, input.Properties?["id"].Type);
        Assert.Equal(JsonSchemaType.String, input.PatternProperties?["^x-"].Type);
        Assert.Equal(schema.MaxProperties, input.MaxProperties);
        Assert.Equal(schema.MinProperties, input.MinProperties);
        Assert.False(input.AdditionalPropertiesAllowed);
        Assert.Equal(JsonSchemaType.String, input.AdditionalProperties?.Type);
        Assert.Equal("ex-1", input.Examples?.Single().GetValue<string>());
        Assert.Equal(2, input.Enum?.Count);
        Assert.False(input.UnevaluatedProperties);
        Assert.Equal(JsonSchemaType.Boolean, input.UnevaluatedPropertiesSchema?.Type);
        Assert.True(input.Deprecated);
        Assert.Equal(["b", "c"], input.DependentRequired?["a"]);
        Assert.Equal("value", Assert.IsType<JsonNodeExtension>(input.Extensions?["x-extra"]).Node.GetValue<string>());
    }

    [Theory]
    [InlineData("Discriminator")]
    [InlineData("Example")]
    [InlineData("ExternalDocs")]
    [InlineData("Xml")]
    public void ImplicitConversion_FromOpenApiSchema_WithOpenApiSpecificKeywords_Throws(string keyword)
    {
        var schema = new OpenApiSchema();

        switch (keyword)
        {
            case "Discriminator":
                schema.Discriminator = new OpenApiDiscriminator { PropertyName = "kind" };
                break;
            case "Example":
                schema.Example = JsonValue.Create("sample");
                break;
            case "ExternalDocs":
                schema.ExternalDocs = new OpenApiExternalDocs { Url = new Uri("https://example.com") };
                break;
            case "Xml":
                schema.Xml = new OpenApiXml { Name = "item" };
                break;
            default:
                throw new InvalidOperationException("Unsupported test input.");
        }

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = (ArazzoInput)schema!;
        });

        Assert.Contains(keyword, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ImplicitConversion_ToOpenApiSchema_CopiesNestedKeywords()
    {
        var input = new ArazzoInput
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IArazzoInput>
            {
                ["name"] = new ArazzoInput { Type = JsonSchemaType.String }
            },
            AdditionalProperties = new ArazzoInput { Type = JsonSchemaType.Integer },
            PatternProperties = new Dictionary<string, IArazzoInput>
            {
                ["^x-"] = new ArazzoInput { Type = JsonSchemaType.Boolean }
            },
            UnevaluatedPropertiesSchema = new ArazzoInput { Type = JsonSchemaType.Number },
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-extra"] = new JsonNodeExtension(JsonValue.Create("value")!)
            }
        };

        OpenApiSchema schema = input!;

        Assert.Equal(JsonSchemaType.Object, schema.Type);
        Assert.Equal(JsonSchemaType.String, schema.Properties?["name"].Type);
        Assert.Equal(JsonSchemaType.Integer, schema.AdditionalProperties?.Type);
        Assert.Equal(JsonSchemaType.Boolean, schema.PatternProperties?["^x-"].Type);
        Assert.Equal(JsonSchemaType.Number, schema.UnevaluatedPropertiesSchema?.Type);
        Assert.Equal(
            "value",
            Assert.IsType<Microsoft.OpenApi.JsonNodeExtension>(schema.Extensions?["x-extra"]).Node.GetValue<string>());
    }

    [Fact]
    public void LoadSchema_WithReference_ReturnsArazzoInputReference()
    {
        var json = JsonNode.Parse(
            """
            {
              "$ref": "#/$defs/shared"
            }
            """)!;

        var context = new global::BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext(
            new global::BinkyLabs.OpenApi.Arazzo.Reader.ArazzoDiagnostic());

        var input = global::BinkyLabs.OpenApi.Arazzo.Reader.V1.ArazzoV1Deserializer.LoadSchema(json, context);

        var reference = Assert.IsType<ArazzoInputReference>(input);
        Assert.Equal("#/$defs/shared", reference.Reference.ReferenceV1);
    }

    [Fact]
    public void LoadSchema_WithReferenceMetadata_CopiesSupportedOverrides()
    {
        var json = JsonNode.Parse(
            """
            {
              "$ref": "#/components/inputs/shared",
              "title": "override",
              "description": "override description",
              "default": "guest",
              "readOnly": true,
              "writeOnly": true,
              "deprecated": true,
              "examples": ["guest"],
              "x-extra": "value"
            }
            """)!;

        var context = new global::BinkyLabs.OpenApi.Arazzo.Reader.ParsingContext(
            new global::BinkyLabs.OpenApi.Arazzo.Reader.ArazzoDiagnostic());

        var input = global::BinkyLabs.OpenApi.Arazzo.Reader.V1.ArazzoV1Deserializer.LoadSchema(json, context);

        var reference = Assert.IsType<ArazzoInputReference>(input);
        Assert.Equal("override", reference.Title);
        Assert.Equal("override description", reference.Description);
        Assert.Equal("guest", reference.Default?.GetValue<string>());
        Assert.True(reference.ReadOnly);
        Assert.True(reference.WriteOnly);
        Assert.True(reference.Deprecated);
        Assert.Equal("guest", reference.Examples?.Single().GetValue<string>());
        Assert.Equal("value", Assert.IsType<JsonNodeExtension>(reference.Extensions?["x-extra"]).Node.GetValue<string>());
    }

    [Fact]
    public void SerializeAsV1_WithNullWriter_Throws()
    {
        var input = new ArazzoInput();

        Assert.Throws<ArgumentNullException>(() => input.SerializeAsV1(null!));
    }

    [Fact]
    public void ImplicitConversions_HandleNullValues()
    {
        ArazzoInput? input = (OpenApiSchema?)null;
        OpenApiSchema? schema = (ArazzoInput?)null;

        Assert.Null(input);
        Assert.Null(schema);
    }

    [Fact]
    public void ConvertFromOpenApiSchema_WithReference_CopiesReferenceMetadata()
    {
        var schemaReference = new OpenApiSchemaReference("shared", null, "external.json")
        {
            Title = "title",
            Description = "description",
            Default = JsonValue.Create("guest"),
            Examples = [JsonValue.Create("example")!],
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["x-extra"] = new Microsoft.OpenApi.JsonNodeExtension(JsonValue.Create("value")!)
            },
            ReadOnly = true,
            WriteOnly = true,
            Deprecated = true
        };

        typeof(BaseOpenApiReference)
            .GetProperty(nameof(BaseOpenApiReference.ReferenceV3))!
            .SetValue(schemaReference.Reference, "https://example.com/external.json#/components/schemas/shared");

        var converted = Assert.IsType<ArazzoInputReference>(ArazzoInput.ConvertFromOpenApiSchema(schemaReference));

        Assert.Equal("title", converted.Title);
        Assert.Equal("description", converted.Description);
        Assert.Equal("guest", converted.Default?.GetValue<string>());
        Assert.Equal("example", converted.Examples?.Single().GetValue<string>());
        Assert.True(converted.ReadOnly);
        Assert.True(converted.WriteOnly);
        Assert.True(converted.Deprecated);
        Assert.Equal("https://example.com/external.json#/components/schemas/shared", converted.Reference.ReferenceV1);
        Assert.Equal("value", Assert.IsType<JsonNodeExtension>(converted.Extensions!["x-extra"]).Node.GetValue<string>());
    }

    [Fact]
    public void ConvertToOpenApiSchema_WithReference_ReturnsSchemaReference()
    {
        var inputReference = new ArazzoInputReference("shared")
        {
            Title = "title",
            Description = "description",
            Default = JsonValue.Create("guest"),
            Examples = [JsonValue.Create("example")!],
            ReadOnly = true,
            WriteOnly = true,
            Deprecated = true,
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-extra"] = new JsonNodeExtension(JsonValue.Create("value")!)
            }
        };
        inputReference.Reference.SetJsonPointerPath("#/components/inputs/shared", "#");

        var converted = Assert.IsType<OpenApiSchemaReference>(ArazzoInput.ConvertToOpenApiSchema(inputReference));

        Assert.Equal("title", converted.Title);
        Assert.Equal("description", converted.Description);
        Assert.Equal("guest", converted.Default?.GetValue<string>());
        Assert.Equal("example", converted.Examples?.Single().GetValue<string>());
        Assert.True(converted.ReadOnly);
        Assert.True(converted.WriteOnly);
        Assert.True(converted.Deprecated);
        Assert.Equal("#/components/inputs/shared", converted.Reference.ReferenceV3);
        Assert.Equal("value", Assert.IsType<Microsoft.OpenApi.JsonNodeExtension>(converted.Extensions!["x-extra"]).Node.GetValue<string>());
    }

    [Fact]
    public void InternalConstructor_AppliesOverridesAndClonesMutableValues()
    {
        var sourceDefault = new JsonObject { ["value"] = "source" };
        var overrideDefault = new JsonObject { ["value"] = "override" };
        var sourceExample = new JsonObject { ["source"] = true };
        var overrideExample = new JsonObject { ["override"] = true };
        var sourceJsonExtension = new JsonNodeExtension(new JsonObject { ["source"] = "value" });
        var overrideJsonExtension = new JsonNodeExtension(new JsonObject { ["override"] = "value" });
        var passthroughExtension = new PassthroughArazzoExtension();

        var source = new ArazzoInput
        {
            Title = "source title",
            Description = "source description",
            Default = sourceDefault,
            Examples = [sourceExample],
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-json"] = sourceJsonExtension,
                ["x-pass"] = passthroughExtension
            },
            Deprecated = false,
            ReadOnly = false,
            WriteOnly = false
        };

        var overrides = new ArazzoInputReference("shared")
        {
            Title = "override title",
            Description = "override description",
            Default = overrideDefault,
            Examples = [overrideExample],
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-json"] = overrideJsonExtension,
                ["x-pass"] = passthroughExtension
            },
            Deprecated = true,
            ReadOnly = true,
            WriteOnly = true
        };

        var result = new ArazzoInput(source, overrides);

        Assert.Equal("override title", result.Title);
        Assert.Equal("override description", result.Description);
        Assert.Equal("override", result.Default?["value"]?.GetValue<string>());
        Assert.True(result.Examples?.Single()["override"]?.GetValue<bool>());
        Assert.True(result.Deprecated);
        Assert.True(result.ReadOnly);
        Assert.True(result.WriteOnly);
        Assert.NotSame(overrideDefault, result.Default);
        Assert.NotSame(overrideExample, result.Examples!.Single());
        Assert.NotSame(overrideJsonExtension, result.Extensions!["x-json"]);
        Assert.Same(passthroughExtension, result.Extensions!["x-pass"]);
    }

    [Fact]
    public void CloneHelpers_HandleNullAndCreateCopies()
    {
        var node = new JsonObject { ["name"] = "value" };
        var list = new List<JsonNode> { node };
        var dependentRequired = new Dictionary<string, HashSet<string>> { ["a"] = ["b"] };
        var extensions = new Dictionary<string, IArazzoExtension>
        {
            ["x-json"] = new JsonNodeExtension(new JsonObject { ["name"] = "value" }),
            ["x-pass"] = new PassthroughArazzoExtension()
        };

        Assert.Null(ArazzoInput.CloneNode(null));
        Assert.Null(ArazzoInput.CloneNodeList(null));
        Assert.Null(ArazzoInput.CloneDependentRequired(null));
        Assert.Null(ArazzoInput.CloneArazzoExtensions(null));

        var clonedNode = ArazzoInput.CloneNode(node);
        var clonedList = ArazzoInput.CloneNodeList(list);
        var clonedDependentRequired = ArazzoInput.CloneDependentRequired(dependentRequired);
        var clonedExtensions = ArazzoInput.CloneArazzoExtensions(extensions);

        Assert.NotSame(node, clonedNode);
        Assert.NotSame(list[0], clonedList![0]);
        Assert.NotSame(dependentRequired["a"], clonedDependentRequired!["a"]);
        Assert.NotSame(extensions["x-json"], clonedExtensions!["x-json"]);
        Assert.Same(extensions["x-pass"], clonedExtensions["x-pass"]);
    }

    [Fact]
    public void ExtensionAdapters_WriteThroughDuringConversion()
    {
        var openApiSchema = new OpenApiSchema
        {
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["x-openapi"] = new PassthroughOpenApiExtension("openapi-value")
            }
        };

        ArazzoInput? input = openApiSchema;
        Assert.NotNull(input);

        using var inputWriterText = new StringWriter();
        var inputWriter = new OpenApiJsonWriter(inputWriterText);
        input.Extensions!["x-openapi"].Write(inputWriter, ArazzoSpecVersion.Arazzo1_0);
        Assert.Equal("\"openapi-value\"", inputWriterText.ToString());

        var arazzoInput = new ArazzoInput
        {
            Extensions = new Dictionary<string, IArazzoExtension>
            {
                ["x-arazzo"] = new PassthroughArazzoExtension("arazzo-value")
            }
        };

        OpenApiSchema? converted = arazzoInput;
        Assert.NotNull(converted);
        using var schemaWriterText = new StringWriter();
        var schemaWriter = new OpenApiJsonWriter(schemaWriterText);
        converted.Extensions!["x-arazzo"].Write(schemaWriter, OpenApiSpecVersion.OpenApi3_2);
        Assert.Equal("\"arazzo-value\"", schemaWriterText.ToString());
    }

    private sealed class PassthroughOpenApiExtension(string value) : IOpenApiExtension
    {
        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
        {
            writer.WriteValue(value);
        }
    }

    private sealed class PassthroughArazzoExtension(string value = "pass") : IArazzoExtension
    {
        public void Write(IOpenApiWriter writer, ArazzoSpecVersion specVersion)
        {
            writer.WriteValue(value);
        }
    }
}