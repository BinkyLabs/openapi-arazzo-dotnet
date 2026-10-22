using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoPayloadReplacementValueUnionTests
{
    public static TheoryData<ArazzoSpecVersion, string> LiteralValues => new()
    {
        { ArazzoSpecVersion.Arazzo1_0, "42" },
        { ArazzoSpecVersion.Arazzo1_1, "42" },
        { ArazzoSpecVersion.Arazzo1_0, "true" },
        { ArazzoSpecVersion.Arazzo1_1, "false" },
        { ArazzoSpecVersion.Arazzo1_0, "\"\"" },
        { ArazzoSpecVersion.Arazzo1_1, "\"literal\"" },
        { ArazzoSpecVersion.Arazzo1_0, "\"$response.statusCode\"" },
        { ArazzoSpecVersion.Arazzo1_1, "\"prefix {$invalid}\"" },
        { ArazzoSpecVersion.Arazzo1_0, "[1,null,\"$inputs.id\",{\"nested\":true}]" },
        { ArazzoSpecVersion.Arazzo1_1, "[1,null,\"$inputs.id\",{\"nested\":true}]" },
        { ArazzoSpecVersion.Arazzo1_0, """{"selector":"$.id","type":"jsonpath","nested":{"context":"literal"}}""" },
        { ArazzoSpecVersion.Arazzo1_1, """{"selector":"$.id","type":"jsonpath","nested":{"context":"literal"}}""" },
        { ArazzoSpecVersion.Arazzo1_0, """{"Any":1,"Expression":"$inputs.id","Selector":{}}""" },
        { ArazzoSpecVersion.Arazzo1_1, "{}" },
        { ArazzoSpecVersion.Arazzo1_0, "[]" },
        { ArazzoSpecVersion.Arazzo1_1, "null" },
        { ArazzoSpecVersion.Arazzo1_0, "null" },
    };

    [Theory]
    [MemberData(nameof(LiteralValues))]
    public void Literal_RoundTripsWithoutWrapperOrReclassification(ArazzoSpecVersion version, string json)
    {
        var input = JsonNode.Parse($$"""{"target":"/id","value":{{json}}}""");
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var replacement = context.ParseFragment<ArazzoPayloadReplacement>(input, version);

        Assert.NotNull(replacement);
        Assert.NotNull(replacement.Value);
        Assert.NotNull(replacement.Value.Any);
        Assert.Null(replacement.Value.Expression);
        Assert.Null(replacement.Value.Selector);
        Assert.Empty(context.Diagnostic.Errors);
        if (json == "null")
        {
            Assert.True(replacement.Value.Any.IsJsonNullSentinel());
        }
        else
        {
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), replacement.Value.Any));
            var fragmentContext = new ParsingContext(new());
            var fragment = fragmentContext.ParseFragment<ArazzoPayloadReplacementValueUnion>(replacement.Value.Any, version);
            Assert.NotNull(fragment);
            Assert.NotNull(fragment.Any);
            Assert.Null(fragment.Expression);
            Assert.Null(fragment.Selector);
            Assert.Empty(fragmentContext.Diagnostic.Errors);
        }

        var output = JsonNode.Parse(Serialize(replacement, version));
        Assert.True(JsonNode.DeepEquals(input, output));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(Serialize(replacement.Value, version))));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$url")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$statusCode")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$request.body#/id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$response.body#/id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$inputs.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$inputs.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$steps.lookup.outputs.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$workflows.lookup.outputs.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$message.payload#/id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$request.payload.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$self")]
    public void Expression_DeserializesExclusivelyAndRoundTrips(ArazzoSpecVersion version, string expression)
    {
        var input = JsonValue.Create(expression);
        var context = new ParsingContext(new());

        var value = context.ParseFragment<ArazzoPayloadReplacementValueUnion>(input, version);

        Assert.NotNull(value);
        Assert.NotNull(value.Expression);
        Assert.Equal(expression, value.Expression.Expression);
        Assert.Null(value.Any);
        Assert.Null(value.Selector);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(input, JsonNode.Parse(Serialize(value, version))));

        var replacement = new ArazzoPayloadReplacement { Target = "/id", Value = value };
        var replacementJson = JsonNode.Parse(Serialize(replacement, version));
        Assert.NotNull(replacementJson);
        Assert.Equal(expression, replacementJson["value"]?.GetValue<string>());
        var roundTrip = new ParsingContext(new()).ParseFragment<ArazzoPayloadReplacement>(replacementJson, version);
        Assert.NotNull(roundTrip);
        Assert.NotNull(roundTrip.Value);
        Assert.NotNull(roundTrip.Value.Expression);
        Assert.Equal(expression, roundTrip.Value.Expression.Expression);
    }

    [Theory]
    [InlineData("$self")]
    [InlineData("$message.payload#/id")]
    [InlineData("$request.payload.id")]
    public void Expression_ClassificationAndSerializationUseVersionGrammar(string expression)
    {
        var input = JsonValue.Create(expression);
        var v1Context = new ParsingContext(new());
        var v1Value = v1Context.ParseFragment<ArazzoPayloadReplacementValueUnion>(input, ArazzoSpecVersion.Arazzo1_0);
        Assert.NotNull(v1Value);
        Assert.NotNull(v1Value.Any);
        Assert.Null(v1Value.Expression);
        Assert.Empty(v1Context.Diagnostic.Errors);

        var v1_1Context = new ParsingContext(new());
        var v1_1Value = v1_1Context.ParseFragment<ArazzoPayloadReplacementValueUnion>(input, ArazzoSpecVersion.Arazzo1_1);
        Assert.NotNull(v1_1Value);
        Assert.NotNull(v1_1Value.Expression);

        var exception = Assert.Throws<ArazzoSerializationException>(() => Serialize(v1_1Value, ArazzoSpecVersion.Arazzo1_0));
        Assert.Contains("must be a valid runtime expression", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Expression_V1OnlyWorkflowReferenceCannotBeSerializedAsV1_1()
    {
        ArazzoPayloadReplacementValueUnion value = new ArazzoRuntimeExpression("$workflows.lookup");

        Assert.Equal("\"$workflows.lookup\"", Serialize(value, ArazzoSpecVersion.Arazzo1_0));
        var exception = Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        Assert.Contains("must be a valid runtime expression", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ImplicitConversions_PreserveAssignedInstancesAndChooseOnlyOneBranch()
    {
        JsonNode node = new JsonObject { ["id"] = 42 };
        var expression = new ArazzoRuntimeExpression("$inputs.id");
        var selector = CreateSelector();

        var literalReplacement = new ArazzoPayloadReplacement { Value = node };
        var expressionReplacement = new ArazzoPayloadReplacement { Value = expression };
        var selectorReplacement = new ArazzoPayloadReplacement { Value = selector };

        Assert.NotNull(literalReplacement.Value);
        Assert.Same(node, literalReplacement.Value.Any);
        Assert.Null(literalReplacement.Value.Expression);
        Assert.Null(literalReplacement.Value.Selector);
        Assert.NotNull(expressionReplacement.Value);
        Assert.Same(expression, expressionReplacement.Value.Expression);
        Assert.Null(expressionReplacement.Value.Any);
        Assert.Null(expressionReplacement.Value.Selector);
        Assert.NotNull(selectorReplacement.Value);
        Assert.Same(selector, selectorReplacement.Value.Selector);
        Assert.Null(selectorReplacement.Value.Any);
        Assert.Null(selectorReplacement.Value.Expression);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void ImplicitJsonNullConversion_WritesExplicitNull(ArazzoSpecVersion version)
    {
        JsonNode? node = null;
        var replacement = new ArazzoPayloadReplacement { Target = "/id", Value = node };

        Assert.NotNull(replacement.Value);
        Assert.NotNull(replacement.Value.Any);
        Assert.True(replacement.Value.Any.IsJsonNullSentinel());
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{"target":"/id","value":null}"""),
            JsonNode.Parse(Serialize(replacement, version))));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$response.body#/id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$response.body#/id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$inputs.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$inputs.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$message.payload#/id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$self")]
    public void ImplicitExpressionConversion_WritesExpressionString(ArazzoSpecVersion version, string expression)
    {
        var replacement = new ArazzoPayloadReplacement
        {
            Target = "/id",
            Value = new ArazzoRuntimeExpression(expression)
        };

        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse($$"""{"target":"/id","value":"{{expression}}"}"""),
            JsonNode.Parse(Serialize(replacement, version))));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void JsonNodeConversion_DoesNotReclassifyExpressionStrings(ArazzoSpecVersion version)
    {
        ArazzoPayloadReplacementValueUnion value = JsonValue.Create("$inputs.id");

        Assert.NotNull(value.Any);
        Assert.Null(value.Expression);
        Assert.Equal("\"$inputs.id\"", Serialize(value, version));
    }

    [Fact]
    public void ImplicitModelConversions_RejectNull()
    {
        ArazzoRuntimeExpression? expression = null;
        ArazzoSelector? selector = null;

        Assert.Throws<ArgumentNullException>(() => (ArazzoPayloadReplacementValueUnion)expression!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoPayloadReplacementValueUnion)selector!);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, 0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, 3)]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, 5)]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, 6)]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, 7)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, 0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, 3)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, 5)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, 6)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, 7)]
    public void Serialize_RejectsEmptyAndConflictingBranches(ArazzoSpecVersion version, int branches)
    {
        var value = new ArazzoPayloadReplacementValueUnion
        {
            Any = (branches & 1) != 0 ? JsonValue.Create(42) : null,
            Expression = (branches & 2) != 0 ? new ArazzoRuntimeExpression("$url") : null,
            Selector = (branches & 4) != 0 ? CreateSelector() : null
        };

        var exception = Assert.Throws<ArazzoSerializationException>(() => Serialize(value, version));

        Assert.Contains("exactly one of Any, Expression, or Selector", exception.Message, StringComparison.Ordinal);
        var replacement = new ArazzoPayloadReplacement { Target = "/id", Value = value };
        Assert.Throws<ArazzoSerializationException>(() => Serialize(replacement, version));
    }

    [Fact]
    public void Serialize_RejectsNullWriter()
    {
        ArazzoPayloadReplacementValueUnion value = new ArazzoRuntimeExpression("$inputs.id");
        Assert.Throws<ArgumentNullException>(() => value.SerializeAsV1(null!));
        Assert.Throws<ArgumentNullException>(() => value.SerializeAsV1_1(null!));
    }

    [Fact]
    public void Selector_RoundTripsInlineWithExtensions()
    {
        var input = JsonNode.Parse("""
            {"target":"/id","value":{"context":"$response.body","selector":"$.id","type":"jsonpath","x-note":"kept"}}
            """);
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var replacement = context.ParseFragment<ArazzoPayloadReplacement>(input, ArazzoSpecVersion.Arazzo1_1);

        Assert.NotNull(replacement);
        Assert.NotNull(replacement.Value);
        Assert.Null(replacement.Value.Any);
        Assert.Null(replacement.Value.Expression);
        var selector = replacement.Value.Selector;
        Assert.NotNull(selector);
        Assert.Equal("$response.body", selector.Context);
        Assert.Equal("$.id", selector.Selector);
        Assert.NotNull(selector.Type);
        Assert.Equal(ArazzoPayloadRequirementTargetSelectorType.JsonPath, selector.Type.Type);
        Assert.NotNull(selector.Extensions);
        Assert.Contains("x-note", selector.Extensions.Keys);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(input, JsonNode.Parse(Serialize(replacement, ArazzoSpecVersion.Arazzo1_1))));
    }

    [Fact]
    public void SelectorConversion_SerializesInline()
    {
        var replacement = new ArazzoPayloadReplacement { Target = "/id", Value = CreateSelector() };

        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse("""{"target":"/id","value":{"context":"$response.body","selector":"$.id","type":"jsonpath"}}"""),
            JsonNode.Parse(Serialize(replacement, ArazzoSpecVersion.Arazzo1_1))));
    }

    [Fact]
    public void Selector_V1SerializationThrowsExplicitUnsupportedException()
    {
        ArazzoPayloadReplacementValueUnion value = CreateSelector();

        var exception = Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_0));
        Assert.Contains("Selector payload replacement values are not supported in Arazzo 1.0", exception.Message, StringComparison.Ordinal);
        var replacement = new ArazzoPayloadReplacement { Target = "/id", Value = value };
        Assert.Throws<ArazzoSerializationException>(() => Serialize(replacement, ArazzoSpecVersion.Arazzo1_0));
    }

    [Fact]
    public void Selector_SerializationValidatesRequiredFieldsAndContext()
    {
        var selector = CreateSelector();
        ArazzoPayloadReplacementValueUnion value = selector;
        selector.Context = "$invalid";
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        selector.Context = "$response.body";
        selector.Selector = null;
        Assert.Throws<ArgumentNullException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        selector.Selector = "$.id";
        selector.Type = null;
        Assert.Throws<ArgumentNullException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
    }

    [Fact]
    public void Document_RoundTripsAllValueBranchesInV1_1()
    {
        var input = JsonNode.Parse("""
            {
              "arazzo":"1.1.0",
              "info":{"title":"Replacements","version":"1"},
              "sourceDescriptions":[{"name":"api","url":"https://example.com/openapi.json","type":"openapi"}],
              "workflows":[{
                "workflowId":"update",
                "steps":[{
                  "stepId":"replace",
                  "operationId":"updateItem",
                  "requestBody":{"replacements":[
                    {"target":"/literal","value":{"id":42}},
                    {"target":"/expression","value":"$response.body#/id"},
                    {"target":"/selector","value":{"context":"$response.body","selector":"/id","type":{"type":"jsonpointer","version":"rfc6901"}}},
                    {"target":"/nullable","value":null}
                  ]}
                }]
              }]
            }
            """);
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var document = context.Parse(input, new Uri("https://example.com/arazzo.json"));

        Assert.Empty(context.Diagnostic.Errors);
        var serialized = JsonNode.Parse(Serialize(document, ArazzoSpecVersion.Arazzo1_1));
        Assert.NotNull(serialized);
        var originalReplacements = input["workflows"]?[0]?["steps"]?[0]?["requestBody"]?["replacements"];
        Assert.NotNull(originalReplacements);
        var serializedReplacements = serialized["workflows"]?[0]?["steps"]?[0]?["requestBody"]?["replacements"];
        Assert.True(JsonNode.DeepEquals(originalReplacements, serializedReplacements));
        var roundTripContext = new ParsingContext(new());
        var roundTrip = roundTripContext.Parse(serialized, new Uri("https://example.com/arazzo.json"));
        Assert.Empty(roundTripContext.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(serialized, JsonNode.Parse(Serialize(roundTrip, ArazzoSpecVersion.Arazzo1_1))));
    }

    [Theory]
    [InlineData("""{"context":"$response.body"}""", "ArazzoSelector.Selector is a REQUIRED field")]
    [InlineData("""{"context":"$response.body","selector":"$.id"}""", "ArazzoSelector.Type is a REQUIRED field")]
    [InlineData("""{"context":null,"selector":"$.id","type":"jsonpath"}""", "ArazzoSelector.Context is a REQUIRED field")]
    [InlineData("""{"context":"literal","selector":"$.id","type":"jsonpath"}""", "must be a valid runtime expression")]
    public void Deserialize_ObjectWithContextAlwaysUsesSelectorValidation(string json, string expectedMessage)
    {
        var input = JsonNode.Parse(json);
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var value = context.ParseFragment<ArazzoPayloadReplacementValueUnion>(input, ArazzoSpecVersion.Arazzo1_1);

        Assert.NotNull(value);
        Assert.NotNull(value.Selector);
        Assert.Null(value.Any);
        Assert.Null(value.Expression);
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains(expectedMessage, StringComparison.Ordinal));
    }

    [Fact]
    public void Deserialize_V1SelectorReportsUnsupportedWithoutChangingVersionForFollowingValues()
    {
        var input = JsonNode.Parse("""
            {"replacements":[
                {"target":"/a","value":{"context":"$response.body","selector":"$.id","type":"jsonpath"}},
                {"target":"/b","value":"$self"}
            ]}
            """);
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var body = context.ParseFragment<ArazzoRequestBody>(input, ArazzoSpecVersion.Arazzo1_0);

        Assert.NotNull(body);
        Assert.NotNull(body.Replacements);
        Assert.Equal(2, body.Replacements.Count);
        var selectorValue = body.Replacements[0].Value;
        Assert.NotNull(selectorValue);
        Assert.NotNull(selectorValue.Selector);
        Assert.Null(selectorValue.Any);
        var literalValue = body.Replacements[1].Value;
        Assert.NotNull(literalValue);
        Assert.NotNull(literalValue.Any);
        Assert.Equal("$self", literalValue.Any.GetValue<string>());
        Assert.Null(literalValue.Expression);
        Assert.Equal(ArazzoSpecVersion.Arazzo1_0, context.Diagnostic.SpecificationVersion);
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains("not supported in Arazzo 1.0", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void SerializeYaml_WritesValuesWithoutWrapperNodes(ArazzoSpecVersion version)
    {
        ArazzoPayloadReplacementValueUnion literal = new JsonObject { ["id"] = 42 };
        ArazzoPayloadReplacementValueUnion expression = new ArazzoRuntimeExpression("$url");
        var body = new ArazzoRequestBody
        {
            Replacements = new List<ArazzoPayloadReplacement>
            {
                new() { Target = "/a", Value = literal },
                new() { Target = "/b", Value = expression }
            }
        };
        if (version is ArazzoSpecVersion.Arazzo1_1)
        {
            body.Replacements.Add(new() { Target = "/c", Value = CreateSelector() });
        }
        using var textWriter = new StringWriter();
        var writer = new OpenApiYamlWriter(textWriter);
        if (version is ArazzoSpecVersion.Arazzo1_0)
        {
            body.SerializeAsV1(writer);
        }
        else
        {
            body.SerializeAsV1_1(writer);
        }

        var yaml = textWriter.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Contains("value:\n      id: 42", yaml, StringComparison.Ordinal);
        Assert.Contains("value: $url", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Any:", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Expression:", yaml, StringComparison.Ordinal);
        if (version is ArazzoSpecVersion.Arazzo1_1)
        {
            Assert.Contains("value:\n      context: $response.body\n      selector: $.id\n      type: jsonpath", yaml, StringComparison.Ordinal);
        }
    }

    private static ArazzoSelector CreateSelector() => new()
    {
        Context = "$response.body",
        Selector = "$.id",
        Type = ArazzoPayloadRequirementTargetSelectorType.JsonPath
    };

    private static string Serialize(IArazzoSerializable value, ArazzoSpecVersion version)
    {
        using var textWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(textWriter);
        if (version is ArazzoSpecVersion.Arazzo1_0)
        {
            value.SerializeAsV1(writer);
        }
        else
        {
            value.SerializeAsV1_1(writer);
        }

        return textWriter.ToString();
    }
}