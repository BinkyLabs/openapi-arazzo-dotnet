using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Models;

public class ArazzoReferenceWorkspaceTests
{
    [Fact]
    public void RegisterComponents_RegistersNestedInputsAndResolvesSubSchemas()
    {
        var definitionsInput = new ArazzoInput { Type = JsonSchemaType.String };
        var propertyInput = new ArazzoInput { Type = JsonSchemaType.Integer };
        var patternPropertyInput = new ArazzoInput { Type = JsonSchemaType.Boolean };
        var itemsInput = new ArazzoInput { Type = JsonSchemaType.Number };
        var additionalPropertiesInput = new ArazzoInput { Type = JsonSchemaType.String };
        var unevaluatedPropertiesInput = new ArazzoInput { Type = JsonSchemaType.Integer };
        var notInput = new ArazzoInput { Type = JsonSchemaType.Null };
        var allOfInput = new ArazzoInput { Type = JsonSchemaType.Array };
        var anyOfInput = new ArazzoInput { Type = JsonSchemaType.Object };
        var oneOfInput = new ArazzoInput { Type = JsonSchemaType.String };

        var rootInput = new ArazzoInput
        {
            Id = "schemas/root",
            Definitions = new Dictionary<string, IArazzoInput> { ["shared"] = definitionsInput },
            Properties = new Dictionary<string, IArazzoInput> { ["count"] = propertyInput },
            PatternProperties = new Dictionary<string, IArazzoInput> { ["^x-"] = patternPropertyInput },
            Items = itemsInput,
            AdditionalProperties = additionalPropertiesInput,
            UnevaluatedPropertiesSchema = unevaluatedPropertiesInput,
            Not = notInput,
            AllOf = [allOfInput],
            AnyOf = [anyOfInput],
            OneOf = [oneOfInput]
        };

        var document = new ArazzoDocument
        {
            BaseUri = new Uri("https://example.com/arazzo.json"),
            Components = new ArazzoComponent
            {
                Inputs = new Dictionary<string, IArazzoInput> { ["root"] = rootInput }
            }
        };
        var workspace = new ArazzoWorkspace(document.BaseUri);

        workspace.RegisterComponents(document);

        Assert.True(workspace.ComponentsCount() >= 10);
        Assert.Same(rootInput, workspace.ResolveReference<IArazzoInput>("https://example.com/arazzo.json#/components/inputs/root"));
        Assert.Same(rootInput, workspace.ResolveReference<IArazzoInput>("https://example.com/schemas/root"));
        Assert.Same(definitionsInput, ArazzoWorkspace.ResolveSubSchema(rootInput, ["$defs", "shared"], []));
        Assert.Same(propertyInput, ArazzoWorkspace.ResolveSubSchema(rootInput, [OpenApiConstants.Properties, "count"], []));
        Assert.Same(itemsInput, ArazzoWorkspace.ResolveSubSchema(rootInput, [OpenApiConstants.Items], []));
        Assert.Same(additionalPropertiesInput, ArazzoWorkspace.ResolveSubSchema(rootInput, [OpenApiConstants.AdditionalProperties], []));
        Assert.Same(unevaluatedPropertiesInput, ArazzoWorkspace.ResolveSubSchema(rootInput, [OpenApiConstants.UnevaluatedProperties], []));
        Assert.Same(allOfInput, ArazzoWorkspace.ResolveSubSchema(rootInput, [OpenApiConstants.AllOf, "0"], []));
        Assert.Same(anyOfInput, ArazzoWorkspace.ResolveSubSchema(rootInput, [OpenApiConstants.AnyOf, "0"], []));
        Assert.Same(oneOfInput, ArazzoWorkspace.ResolveSubSchema(rootInput, [OpenApiConstants.OneOf, "0"], []));
        Assert.Same(notInput, ArazzoWorkspace.ResolveSubSchema(rootInput, [OpenApiConstants.Not], []));
        Assert.Null(ArazzoWorkspace.ResolveSubSchema(rootInput, [OpenApiConstants.AllOf, "not-an-index"], []));
    }

    [Fact]
    public void Workspace_ManagesComponentRegistrationCopiesAndDocumentIds()
    {
        var document = new ArazzoDocument { BaseUri = new Uri("https://example.com/arazzo.json") };
        var workspace = new ArazzoWorkspace(new Uri("https://example.com/"));
        var component = new ArazzoInput { Type = JsonSchemaType.String };

        Assert.True(workspace.RegisterComponentForDocument(document, component, "components/inputs/from-relative"));
        Assert.True(workspace.Contains("https://example.com/components/inputs/from-relative"));
        Assert.False(workspace.RegisterComponent("https://example.com/components/inputs/from-relative", component));

        workspace.AddDocumentId("external.json", new Uri("https://example.com/external.json"));
        workspace.AddDocumentId("external.json", new Uri("https://example.com/ignored.json"));

        var copy = new ArazzoWorkspace(workspace);

        Assert.True(copy.Contains("external.json"));
        Assert.Equal(new Uri("https://example.com/external.json"), copy.GetDocumentId("external.json"));
        Assert.True(copy.RegisterComponentForDocument(document, new ArazzoInput { Type = JsonSchemaType.Integer }, "https://example.com/components/inputs/absolute"));
        Assert.NotNull(copy.ResolveReference<IArazzoInput>("https://example.com/components/inputs/absolute"));
    }

    [Fact]
    public void ResolveSubSchema_ThrowsForCircularArazzoInputReference()
    {
        var reference = new ArazzoInputReference("loop");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ArazzoWorkspace.ResolveSubSchema(reference, [], new Stack<IArazzoInput>([reference])));

        Assert.Contains("Circular reference detected while resolving schema", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveSubSchema_ThrowsForCircularInlineSchema()
    {
        var input = new ArazzoInput();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ArazzoWorkspace.ResolveSubSchema(input, [], new Stack<IArazzoInput>([input])));

        Assert.Equal("Circular reference detected while resolving schema.", exception.Message);
    }

    [Fact]
    public void ArazzoInputReference_ResolvesTargetAndRecursiveTarget()
    {
        var terminal = new ArazzoInput
        {
            Title = "terminal",
            Schema = new Uri("https://json-schema.org/draft/2020-12/schema"),
            Id = "urn:terminal",
            Comment = "comment",
            Vocabulary = new Dictionary<string, bool> { ["core"] = true },
            DynamicRef = "#dynamic",
            DynamicAnchor = "dynamic",
            Definitions = new Dictionary<string, IArazzoInput> { ["def"] = new ArazzoInput { Type = JsonSchemaType.String } },
            ExclusiveMaximum = "10",
            ExclusiveMinimum = "1",
            Type = JsonSchemaType.Object,
            Const = "fixed",
            Format = "date-time",
            Description = "description",
            Maximum = "9",
            Minimum = "2",
            MaxLength = 10,
            MinLength = 1,
            Pattern = "^[a-z]+$",
            MultipleOf = 2,
            Default = JsonValue.Create("default"),
            ReadOnly = true,
            WriteOnly = true,
            AllOf = [new ArazzoInput { Type = JsonSchemaType.String }],
            OneOf = [new ArazzoInput { Type = JsonSchemaType.Number }],
            AnyOf = [new ArazzoInput { Type = JsonSchemaType.Integer }],
            Not = new ArazzoInput { Type = JsonSchemaType.Null },
            Required = new HashSet<string> { "id" },
            Items = new ArazzoInput { Type = JsonSchemaType.String },
            MaxItems = 3,
            MinItems = 1,
            UniqueItems = true,
            Properties = new Dictionary<string, IArazzoInput> { ["value"] = new ArazzoInput { Type = JsonSchemaType.String } },
            PatternProperties = new Dictionary<string, IArazzoInput> { ["^x-"] = new ArazzoInput { Type = JsonSchemaType.Boolean } },
            MaxProperties = 5,
            MinProperties = 1,
            AdditionalPropertiesAllowed = false,
            AdditionalProperties = new ArazzoInput { Type = JsonSchemaType.String },
            Examples = [JsonValue.Create("example")!],
            Enum = [JsonValue.Create("A")!],
            UnevaluatedProperties = false,
            UnevaluatedPropertiesSchema = new ArazzoInput { Type = JsonSchemaType.Integer },
            Deprecated = true,
            DependentRequired = new Dictionary<string, HashSet<string>> { ["a"] = ["b"] },
            Extensions = new Dictionary<string, IArazzoExtension> { ["x-extra"] = new JsonNodeExtension(JsonValue.Create("value")!) }
        };

        var reference = new ArazzoInputReference("terminal")
        {
            Title = "override",
            Description = "override description",
            Default = JsonValue.Create("override-default"),
            ReadOnly = true,
            WriteOnly = true,
            Deprecated = true,
            Examples = [JsonValue.Create("override-example")!],
            Extensions = new Dictionary<string, IArazzoExtension> { ["x-override"] = new JsonNodeExtension(JsonValue.Create("override")!) }
        };
        reference.Reference.SetJsonPointerPath("#/components/inputs/terminal", "#");

        Assert.True(reference.UnresolvedReference);
        Assert.Null(reference.Target);
        Assert.Null(reference.RecursiveTarget);
        Assert.Equal("override", reference.Title);
        Assert.Null(reference.Schema);
        Assert.Null(reference.Id);
        Assert.Null(reference.Comment);
        Assert.Null(reference.DynamicRef);
        Assert.Null(reference.DynamicAnchor);
        Assert.Equal("override description", reference.Description);
        Assert.Null(reference.ExclusiveMaximum);
        Assert.Null(reference.ExclusiveMinimum);
        Assert.Null(reference.Type);
        Assert.Null(reference.Const);
        Assert.Null(reference.Format);
        Assert.Null(reference.Maximum);
        Assert.Null(reference.Minimum);
        Assert.Null(reference.MaxLength);
        Assert.Null(reference.MinLength);
        Assert.Null(reference.Pattern);
        Assert.Null(reference.MultipleOf);
        Assert.Equal("override-default", reference.Default?.GetValue<string>());
        Assert.True(reference.ReadOnly);
        Assert.True(reference.WriteOnly);
        Assert.Null(reference.AllOf);
        Assert.Null(reference.OneOf);
        Assert.Null(reference.AnyOf);
        Assert.Null(reference.Not);
        Assert.Null(reference.Required);
        Assert.Null(reference.Items);
        Assert.Null(reference.MaxItems);
        Assert.Null(reference.MinItems);
        Assert.Null(reference.UniqueItems);
        Assert.Null(reference.Properties);
        Assert.Null(reference.PatternProperties);
        Assert.Null(reference.MaxProperties);
        Assert.Null(reference.MinProperties);
        Assert.True(reference.AdditionalPropertiesAllowed);
        Assert.Null(reference.AdditionalProperties);
        Assert.Equal("override-example", reference.Examples?.Single().GetValue<string>());
        Assert.Null(reference.Enum);
        Assert.True(reference.UnevaluatedProperties);
        Assert.Null(reference.UnevaluatedPropertiesSchema);
        Assert.True(reference.Deprecated);
        Assert.Null(reference.DependentRequired);
        Assert.Equal("override", Assert.IsType<JsonNodeExtension>(reference.Extensions!["x-override"]).Node.GetValue<string>());
    }

    [Fact]
    public void ArazzoInputReference_FallsBackToResolvedTargetValues()
    {
        var terminal = new ArazzoInput
        {
            Title = "terminal",
            Schema = new Uri("https://json-schema.org/draft/2020-12/schema"),
            Id = "urn:terminal",
            Comment = "comment",
            Vocabulary = new Dictionary<string, bool> { ["core"] = true },
            DynamicRef = "#dynamic",
            DynamicAnchor = "dynamic",
            Definitions = new Dictionary<string, IArazzoInput> { ["def"] = new ArazzoInput { Type = JsonSchemaType.String } },
            ExclusiveMaximum = "10",
            ExclusiveMinimum = "1",
            Type = JsonSchemaType.Object,
            Const = "fixed",
            Format = "date-time",
            Description = "description",
            Maximum = "9",
            Minimum = "2",
            MaxLength = 10,
            MinLength = 1,
            Pattern = "^[a-z]+$",
            MultipleOf = 2,
            Default = JsonValue.Create("default"),
            ReadOnly = true,
            WriteOnly = true,
            AllOf = [new ArazzoInput { Type = JsonSchemaType.String }],
            OneOf = [new ArazzoInput { Type = JsonSchemaType.Number }],
            AnyOf = [new ArazzoInput { Type = JsonSchemaType.Integer }],
            Not = new ArazzoInput { Type = JsonSchemaType.Null },
            Required = new HashSet<string> { "id" },
            Items = new ArazzoInput { Type = JsonSchemaType.String },
            MaxItems = 3,
            MinItems = 1,
            UniqueItems = true,
            Properties = new Dictionary<string, IArazzoInput> { ["value"] = new ArazzoInput { Type = JsonSchemaType.String } },
            PatternProperties = new Dictionary<string, IArazzoInput> { ["^x-"] = new ArazzoInput { Type = JsonSchemaType.Boolean } },
            MaxProperties = 5,
            MinProperties = 1,
            AdditionalPropertiesAllowed = false,
            AdditionalProperties = new ArazzoInput { Type = JsonSchemaType.String },
            Examples = [JsonValue.Create("example")!],
            Enum = [JsonValue.Create("A")!],
            UnevaluatedProperties = false,
            UnevaluatedPropertiesSchema = new ArazzoInput { Type = JsonSchemaType.Integer },
            Deprecated = true,
            DependentRequired = new Dictionary<string, HashSet<string>> { ["a"] = ["b"] },
            Extensions = new Dictionary<string, IArazzoExtension> { ["x-extra"] = new JsonNodeExtension(JsonValue.Create("value")!) }
        };
        var reference = new TestArazzoInputReference("terminal")
        {
            TargetOverride = terminal
        };

        Assert.False(reference.UnresolvedReference);
        Assert.Same(terminal, reference.TargetOverride);
        Assert.Equal("terminal", reference.Title);
        Assert.Equal(new Uri("https://json-schema.org/draft/2020-12/schema"), reference.Schema);
        Assert.Equal("urn:terminal", reference.Id);
        Assert.Equal("comment", reference.Comment);
        Assert.True(reference.Vocabulary!["core"]);
        Assert.Equal("#dynamic", reference.DynamicRef);
        Assert.Equal("dynamic", reference.DynamicAnchor);
        Assert.Same(terminal.Definitions, reference.Definitions);
        Assert.Equal("10", reference.ExclusiveMaximum);
        Assert.Equal("1", reference.ExclusiveMinimum);
        Assert.Equal(JsonSchemaType.Object, reference.Type);
        Assert.Equal("fixed", reference.Const);
        Assert.Equal("date-time", reference.Format);
        Assert.Equal("description", reference.Description);
        Assert.Equal("9", reference.Maximum);
        Assert.Equal("2", reference.Minimum);
        Assert.Equal(10, reference.MaxLength);
        Assert.Equal(1, reference.MinLength);
        Assert.Equal("^[a-z]+$", reference.Pattern);
        Assert.Equal(2, reference.MultipleOf);
        Assert.Equal("default", reference.Default?.GetValue<string>());
        Assert.True(reference.ReadOnly);
        Assert.True(reference.WriteOnly);
        Assert.Same(terminal.AllOf, reference.AllOf);
        Assert.Same(terminal.OneOf, reference.OneOf);
        Assert.Same(terminal.AnyOf, reference.AnyOf);
        Assert.Same(terminal.Not, reference.Not);
        Assert.Same(terminal.Required, reference.Required);
        Assert.Same(terminal.Items, reference.Items);
        Assert.Equal(3, reference.MaxItems);
        Assert.Equal(1, reference.MinItems);
        Assert.True(reference.UniqueItems);
        Assert.Same(terminal.Properties, reference.Properties);
        Assert.Same(terminal.PatternProperties, reference.PatternProperties);
        Assert.Equal(5, reference.MaxProperties);
        Assert.Equal(1, reference.MinProperties);
        Assert.False(reference.AdditionalPropertiesAllowed);
        Assert.Same(terminal.AdditionalProperties, reference.AdditionalProperties);
        Assert.Equal("example", reference.Examples?.Single().GetValue<string>());
        Assert.Same(terminal.Enum, reference.Enum);
        Assert.False(reference.UnevaluatedProperties);
        Assert.Same(terminal.UnevaluatedPropertiesSchema, reference.UnevaluatedPropertiesSchema);
        Assert.True(reference.Deprecated);
        Assert.Same(terminal.DependentRequired, reference.DependentRequired);
        Assert.Equal("value", Assert.IsType<JsonNodeExtension>(reference.Extensions!["x-extra"]).Node.GetValue<string>());
    }

    [Fact]
    public void BaseArazzoReferenceHolder_RecursiveTarget_ThrowsForCircularReferences()
    {
        var first = new TestReferenceHolder("first");
        var second = new TestReferenceHolder("second");
        first.TargetOverride = second;
        second.TargetOverride = first;

        var exception = Assert.Throws<InvalidOperationException>(() => _ = first.RecursiveTarget);

        Assert.Contains("Circular reference detected while resolving reference", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BaseArazzoReferenceHolder_RecursiveTarget_ReturnsConcreteTarget()
    {
        var terminal = new TestReferenceable();
        var intermediate = new TestReferenceHolder("intermediate") { TargetOverride = terminal };
        var root = new TestReferenceHolder("root") { TargetOverride = intermediate };

        Assert.False(root.UnresolvedReference);
        Assert.Same(terminal, root.RecursiveTarget);
    }

    [Fact]
    public void ArazzoInputReference_UnsupportedSettersThrow()
    {
        var reference = new ArazzoInputReference("shared");
        var setters = new Action[]
        {
            () => reference.Schema = new Uri("https://example.com/schema"),
            () => reference.Id = "id",
            () => reference.Comment = "comment",
            () => reference.Vocabulary = new Dictionary<string, bool>(),
            () => reference.DynamicRef = "#dynamic",
            () => reference.DynamicAnchor = "dynamic",
            () => reference.Definitions = new Dictionary<string, IArazzoInput>(),
            () => reference.ExclusiveMaximum = "10",
            () => reference.ExclusiveMinimum = "1",
            () => reference.Type = JsonSchemaType.String,
            () => reference.Const = "fixed",
            () => reference.Format = "date-time",
            () => reference.Maximum = "9",
            () => reference.Minimum = "2",
            () => reference.MaxLength = 10,
            () => reference.MinLength = 1,
            () => reference.Pattern = "^[a-z]+$",
            () => reference.MultipleOf = 2,
            () => reference.AllOf = [new ArazzoInput()],
            () => reference.OneOf = [new ArazzoInput()],
            () => reference.AnyOf = [new ArazzoInput()],
            () => reference.Not = new ArazzoInput(),
            () => reference.Required = new HashSet<string> { "id" },
            () => reference.Items = new ArazzoInput(),
            () => reference.MaxItems = 3,
            () => reference.MinItems = 1,
            () => reference.UniqueItems = true,
            () => reference.Properties = new Dictionary<string, IArazzoInput>(),
            () => reference.PatternProperties = new Dictionary<string, IArazzoInput>(),
            () => reference.MaxProperties = 5,
            () => reference.MinProperties = 1,
            () => reference.AdditionalPropertiesAllowed = false,
            () => reference.AdditionalProperties = new ArazzoInput(),
            () => reference.Enum = [JsonValue.Create("A")!],
            () => reference.UnevaluatedProperties = false,
            () => reference.UnevaluatedPropertiesSchema = new ArazzoInput(),
            () => reference.DependentRequired = new Dictionary<string, HashSet<string>>()
        };

        foreach (var exception in setters.Select(setter => Assert.Throws<NotSupportedException>(setter)))
        {
            Assert.Contains("cannot be overridden", exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BaseArazzoReferenceHolder_TargetIsNullWithoutHostDocument()
    {
        var reference = new ArazzoInputReference("shared");

        Assert.Null(reference.Target);
        Assert.True(reference.UnresolvedReference);
    }

    [Fact]
    public void BaseArazzoReferenceHolder_CopyConstructor_ClonesReference()
    {
        var original = new TestReferenceHolder("shared");
        var copy = new TestReferenceHolder(original);

        Assert.NotSame(original.Reference, copy.Reference);
        Assert.Equal(original.Reference.Id, copy.Reference.Id);
        Assert.Equal(original.Reference.Type, copy.Reference.Type);
    }

    [Fact]
    public void BaseArazzoReferenceHolder_SerializeAsV1_WritesReferenceObject()
    {
        var holder = new TestReferenceHolder("shared");
        using var writerText = new StringWriter();
        var writer = new OpenApiJsonWriter(writerText);

        holder.SerializeAsV1(writer);

        Assert.Contains("\"$ref\": \"$components.inputs.shared\"", writerText.ToString());
    }

    [Fact]
    public void BaseArazzoReferenceHolder_SerializeAsV1_WithNullWriter_Throws()
    {
        var holder = new TestReferenceHolder("shared");

        Assert.Throws<ArgumentNullException>(() => holder.SerializeAsV1(null!));
    }

    [Fact]
    public void ArazzoDocument_ResolveReference_ReturnsNullForNullReference()
    {
        var document = new ArazzoDocument();

        Assert.Null(document.ResolveReference(null, useExternal: false, parentInput: null));
    }

    [Fact]
    public void ArazzoDocument_ResolveReferenceTo_ResolvesLocalButNotExternalInput()
    {
        var local = new ArazzoInput { Type = JsonSchemaType.String };
        var external = new ArazzoInput { Type = JsonSchemaType.Integer };
        var document = new ArazzoDocument
        {
            BaseUri = new Uri("https://example.com/root/arazzo.json"),
            Components = new ArazzoComponent
            {
                Inputs = new Dictionary<string, IArazzoInput>
                {
                    ["local"] = local
                }
            }
        };
        document.RegisterComponents();
        document.Workspace!.AddDocumentId("external.json", new Uri("https://example.com/root/external.json"));
        document.Workspace.RegisterComponent("https://example.com/root/external.json#/components/inputs/shared", external);

        var localReference = new BaseArazzoReference
        {
            Type = ReferenceType.Input,
            Id = "local",
            HostDocument = document
        };
        var externalReference = new BaseArazzoReference
        {
            Type = ReferenceType.Input,
            Id = "shared",
            HostDocument = document,
            ExternalResource = "external.json"
        };
        localReference.SetJsonPointerPath("#/components/inputs/local", "#");
        externalReference.SetJsonPointerPath("#/components/inputs/shared", "#");

        Assert.Same(local, document.ResolveReferenceTo<IArazzoReferenceable>(localReference, null));
        Assert.Null(document.ResolveReferenceTo<IArazzoReferenceable>(externalReference, null));
        Assert.Null(document.ResolveReferenceTo<TestReferenceable>(localReference, null));
    }

    [Fact]
    public void ArazzoDocument_ResolveReferenceTo_ReturnsNullWhenNestedParentLookupDoesNotResolveInput()
    {
        var nested = new ArazzoInput { Type = JsonSchemaType.Boolean };
        var parent = new ArazzoInput
        {
            Definitions = new Dictionary<string, IArazzoInput>
            {
                ["flag"] = nested
            }
        };
        var document = new ArazzoDocument
        {
            BaseUri = new Uri("https://example.com/root/arazzo.json")
        };

        var reference = new BaseArazzoReference
        {
            Type = ReferenceType.Input,
            HostDocument = document
        };
        reference.SetJsonPointerPath("#/$defs/flag", "#");

        Assert.Null(document.ResolveReferenceTo<IArazzoReferenceable>(reference, parent));
    }

    [Fact]
    public void ArazzoDocument_ResolveReference_UsesDefaultRelativePathsWhenReferenceStringHasNoSlash()
    {
        var local = new ArazzoInput { Type = JsonSchemaType.String };
        var document = new ArazzoDocument
        {
            BaseUri = new Uri("https://example.com/root/arazzo.json"),
            Components = new ArazzoComponent
            {
                Inputs = new Dictionary<string, IArazzoInput>
                {
                    ["local"] = local
                }
            }
        };
        document.RegisterComponents();
        document.Workspace!.AddDocumentId("external.json", new Uri("https://example.com/root/external.json"));

        var localReference = new BaseArazzoReference
        {
            Type = ReferenceType.Input,
            Id = "local",
            HostDocument = document
        };
        var externalReference = new BaseArazzoReference
        {
            Type = ReferenceType.Input,
            Id = "shared",
            HostDocument = document,
            ExternalResource = "external.json"
        };

        Assert.Null(document.ResolveReference(localReference, useExternal: false, parentInput: null));
        Assert.Throws<UriFormatException>(() => document.ResolveReference(externalReference, useExternal: true, parentInput: null));
    }

    private sealed class TestReferenceable : IArazzoReferenceable
    {
        public void SerializeAsV1(IOpenApiWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
        }
    }

    private sealed class TestReferenceHolder : BaseArazzoReferenceHolder<TestReferenceable, IArazzoReferenceable, BaseArazzoReference>, IArazzoReferenceable
    {
        public TestReferenceHolder(string referenceId)
            : base(referenceId, null, ReferenceType.Input, null)
        {
        }

        public TestReferenceHolder(TestReferenceHolder source)
            : base(source)
        {
        }

        public IArazzoReferenceable? TargetOverride { get; set; }

        public override IArazzoReferenceable? Target => TargetOverride;

        public override void SerializeAsV1(IOpenApiWriter writer)
        {
            base.SerializeAsV1(writer);
        }

        public override IArazzoReferenceable CopyReferenceAsTargetElementWithOverrides(IArazzoReferenceable source)
        {
            return source;
        }

        protected override BaseArazzoReference CopyReference(BaseArazzoReference sourceReference)
        {
            return new BaseArazzoReference(sourceReference);
        }
    }

    private sealed class TestArazzoInputReference(string referenceId) : ArazzoInputReference(referenceId)
    {
        public IArazzoInput? TargetOverride { get; set; }

        public override IArazzoInput? Target => TargetOverride;
    }
}