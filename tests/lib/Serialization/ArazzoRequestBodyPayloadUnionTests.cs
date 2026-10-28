using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoRequestBodyPayloadUnionTests
{
    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"literal\"")]
    [InlineData("\"prefix {$inputs.id}\"")]
    [InlineData("""{"Any":1,"Expression":"$inputs.id","Selector":{}}""")]
    [InlineData("""[1,null,{"id":"$inputs.id"}]""")]
    public void Literals_RoundTripBothVersionsWithoutWrapper(string json)
    {
        foreach (var version in new[] { ArazzoSpecVersion.Arazzo1_0, ArazzoSpecVersion.Arazzo1_1 })
        {
            var input = JsonNode.Parse($$"""{"payload":{{json}}}""");
            Assert.NotNull(input);
            var context = new ParsingContext(new());

            var body = context.ParseFragment<ArazzoRequestBody>(input, version);

            Assert.NotNull(body);
            Assert.NotNull(body.Payload);
            Assert.NotNull(body.Payload.Any);
            Assert.Null(body.Payload.Expression);
            Assert.Null(body.Payload.Selector);
            Assert.Empty(context.Diagnostic.Errors);
            Assert.True(JsonNode.DeepEquals(input, JsonNode.Parse(Serialize(body, version))));
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(Serialize(body.Payload, version))));
            if (json == "null")
            {
                Assert.True(body.Payload.Any.IsJsonNullSentinel());
            }
            else
            {
                var fragment = context.ParseFragment<ArazzoRequestBodyPayloadUnion>(body.Payload.Any, version);
                Assert.NotNull(fragment);
                Assert.NotNull(fragment.Any);
                Assert.Null(fragment.Expression);
                Assert.Null(fragment.Selector);
                Assert.Empty(context.Diagnostic.Errors);
            }
        }
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$inputs.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$inputs.id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$request.body#/id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$message.payload#/id")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$self")]
    public void Expressions_RoundTripAsExclusiveExpressionMembers(ArazzoSpecVersion version, string expression)
    {
        var input = JsonValue.Create(expression);
        var context = new ParsingContext(new());

        var payload = context.ParseFragment<ArazzoRequestBodyPayloadUnion>(input, version);

        Assert.NotNull(payload);
        Assert.NotNull(payload.Expression);
        Assert.Equal(expression, payload.Expression.Expression);
        Assert.Null(payload.Any);
        Assert.Null(payload.Selector);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(input, JsonNode.Parse(Serialize(payload, version))));
        var bodyJson = JsonNode.Parse(Serialize(new ArazzoRequestBody { Payload = payload }, version));
        Assert.NotNull(bodyJson);
        var roundTrip = context.ParseFragment<ArazzoRequestBody>(bodyJson, version);
        Assert.NotNull(roundTrip);
        Assert.NotNull(roundTrip.Payload);
        Assert.NotNull(roundTrip.Payload.Expression);
        Assert.Equal(expression, roundTrip.Payload.Expression.Expression);
        Assert.Empty(context.Diagnostic.Errors);
    }

    [Fact]
    public void Selector_RoundTripsInlineWithExpressionTypeAndExtensions()
    {
        var input = JsonNode.Parse("""
            {"payload":{"context":"$message.payload","selector":"$.id","type":{"type":"jsonpath","version":"rfc9535"},"x-note":true}}
            """);
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var body = context.ParseFragment<ArazzoRequestBody>(input, ArazzoSpecVersion.Arazzo1_1);

        Assert.NotNull(body);
        Assert.NotNull(body.Payload);
        Assert.Null(body.Payload.Any);
        Assert.Null(body.Payload.Expression);
        var selector = body.Payload.Selector;
        Assert.NotNull(selector);
        Assert.NotNull(selector.Context);
        Assert.Equal("$message.payload", selector.Context.Expression);
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(input, JsonNode.Parse(Serialize(body, ArazzoSpecVersion.Arazzo1_1))));
    }

    [Theory]
    [InlineData("""{"context":"literal","selector":"$.id","type":"jsonpath"}""", "must be a valid runtime expression")]
    [InlineData("""{"context":"$response.body","type":"jsonpath"}""", "ArazzoSelector.Selector is a REQUIRED field")]
    [InlineData("""{"context":"$response.body","selector":"$.id"}""", "ArazzoSelector.Type is a REQUIRED field")]
    public void Selector_UsesExistingValidationWithoutFallingBackToLiteral(string json, string message)
    {
        var input = JsonNode.Parse(json);
        Assert.NotNull(input);
        var context = new ParsingContext(new());

        var payload = context.ParseFragment<ArazzoRequestBodyPayloadUnion>(input, ArazzoSpecVersion.Arazzo1_1);

        Assert.NotNull(payload);
        Assert.NotNull(payload.Selector);
        Assert.Null(payload.Any);
        Assert.Null(payload.Expression);
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains(message, StringComparison.Ordinal));
    }

    [Fact]
    public void Selector_IsExplicitlyRejectedInV1ForReadingAndWriting()
    {
        ArazzoRequestBodyPayloadUnion payload = CreateSelector();
        var exception = Assert.Throws<ArazzoSerializationException>(() => Serialize(payload, ArazzoSpecVersion.Arazzo1_0));
        Assert.Contains("Selector request body payloads are not supported in Arazzo 1.0", exception.Message, StringComparison.Ordinal);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(new ArazzoRequestBody { Payload = payload }, ArazzoSpecVersion.Arazzo1_0));
        var input = JsonNode.Parse(Serialize(payload, ArazzoSpecVersion.Arazzo1_1));
        Assert.NotNull(input);
        var context = new ParsingContext(new());
        Assert.Throws<ArazzoReaderException>(() => context.ParseFragment<ArazzoRequestBodyPayloadUnion>(input, ArazzoSpecVersion.Arazzo1_0));

        var body = context.ParseFragment<ArazzoRequestBody>(new JsonObject { ["payload"] = input }, ArazzoSpecVersion.Arazzo1_0);

        Assert.NotNull(body);
        Assert.Null(body.Payload);
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains("not supported in Arazzo 1.0", StringComparison.Ordinal));
        Assert.Equal(ArazzoSpecVersion.Arazzo1_0, context.Diagnostic.SpecificationVersion);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0, "$message.payload")]
    [InlineData(ArazzoSpecVersion.Arazzo1_1, "$workflows.lookup")]
    public void Expressions_UseTheSelectedVersionGrammar(ArazzoSpecVersion version, string expression)
    {
        ArazzoRequestBodyPayloadUnion payload = new ArazzoRuntimeExpression(expression);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(payload, version));
        var context = new ParsingContext(new());

        var parsed = context.ParseFragment<ArazzoRequestBodyPayloadUnion>(JsonValue.Create(expression), version);

        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Any);
        Assert.Null(parsed.Expression);
        Assert.Contains(context.Diagnostic.Errors, error => error.Message.Contains("contains an invalid runtime expression", StringComparison.Ordinal));
    }

    [Fact]
    public void Conversions_PreserveAssignedMembersInBothDirections()
    {
        JsonNode node = new JsonObject { ["id"] = 42 };
        ArazzoRequestBodyPayloadUnion payload = node;
        JsonNode actualNode = payload;
        Assert.Same(node, actualNode);
        var expression = new ArazzoRuntimeExpression("$inputs.id");
        payload = expression;
        ArazzoRuntimeExpression actualExpression = payload;
        Assert.Same(expression, actualExpression);
        var selector = CreateSelector();
        payload = selector;
        ArazzoSelector actualSelector = payload;
        Assert.Same(selector, actualSelector);
        payload = "$inputs.id";
        Assert.Equal("$inputs.id", payload.Any?.GetValue<string>());
        Assert.Null(payload.Expression);
        payload = JsonValue.Create("$inputs.id");
        Assert.NotNull(payload.Any);
        Assert.Null(payload.Expression);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void ConversionsAndSerialization_RejectInactiveOrConflictingMembers(int branches)
    {
        var payload = new ArazzoRequestBodyPayloadUnion
        {
            Any = (branches & 1) != 0 ? JsonValue.Create(42) : null,
            Expression = (branches & 2) != 0 ? new("$inputs.id") : null,
            Selector = (branches & 4) != 0 ? CreateSelector() : null
        };
        if (branches != 1) Assert.Throws<InvalidCastException>(() => (JsonNode)payload);
        if (branches != 2) Assert.Throws<InvalidCastException>(() => (ArazzoRuntimeExpression)payload);
        if (branches != 4) Assert.Throws<InvalidCastException>(() => (ArazzoSelector)payload);
        if (branches is not (1 or 2 or 4))
        {
            Assert.Throws<ArazzoSerializationException>(() => Serialize(payload, ArazzoSpecVersion.Arazzo1_0));
            Assert.Throws<ArazzoSerializationException>(() => Serialize(payload, ArazzoSpecVersion.Arazzo1_1));
        }
    }

    [Fact]
    public void ConversionsAndSerialization_RejectNullModelsAndWriters()
    {
        Assert.Throws<ArgumentNullException>(() => (ArazzoRequestBodyPayloadUnion)(ArazzoRuntimeExpression)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoRequestBodyPayloadUnion)(ArazzoSelector)null!);
        Assert.Throws<ArgumentNullException>(() => (JsonNode)(ArazzoRequestBodyPayloadUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoRuntimeExpression)(ArazzoRequestBodyPayloadUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoSelector)(ArazzoRequestBodyPayloadUnion)null!);
        ArazzoRequestBodyPayloadUnion payload = new ArazzoRuntimeExpression("$inputs.id");
        Assert.Throws<ArgumentNullException>(() => payload.SerializeAsV1(null!));
        Assert.Throws<ArgumentNullException>(() => payload.SerializeAsV1_1(null!));
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void NullConversions_DistinguishExplicitNullFromOmittedPayload(ArazzoSpecVersion version)
    {
        foreach (var payload in new ArazzoRequestBodyPayloadUnion[] { (JsonNode?)null, (string?)null })
        {
            JsonNode literal = payload;
            Assert.True(literal.IsJsonNullSentinel());
            Assert.True(JsonNode.DeepEquals(
                JsonNode.Parse("""{"payload":null}"""),
                JsonNode.Parse(Serialize(new ArazzoRequestBody { Payload = payload }, version))));
        }
        var context = new ParsingContext(new());
        var body = context.ParseFragment<ArazzoRequestBody>(new JsonObject(), version);
        Assert.NotNull(body);
        Assert.Null(body.Payload);
        var output = JsonNode.Parse(Serialize(body, version));
        Assert.NotNull(output);
        Assert.Empty(output.AsObject());
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Yaml_SerializesPayloadInline(ArazzoSpecVersion version)
    {
        var body = new ArazzoRequestBody
        {
            Payload = version is ArazzoSpecVersion.Arazzo1_0
                ? new ArazzoRequestBodyPayloadUnion { Expression = new("$inputs.id") }
                : new ArazzoRequestBodyPayloadUnion { Selector = CreateSelector() }
        };
        using var text = new StringWriter();
        var writer = new OpenApiYamlWriter(text);
        if (version is ArazzoSpecVersion.Arazzo1_0) body.SerializeAsV1(writer);
        else body.SerializeAsV1_1(writer);
        var yaml = text.ToString().ReplaceLineEndings("\n");
        Assert.Contains(version is ArazzoSpecVersion.Arazzo1_0 ? "payload: $inputs.id" : "payload:\n  context: $response.body\n  selector: $.id\n  type: jsonpath", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Any:", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Expression:", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Selector:", yaml, StringComparison.Ordinal);
    }

    private static ArazzoSelector CreateSelector() => new()
    {
        Context = new("$response.body"),
        Selector = "$.id",
        Type = ArazzoPayloadRequirementTargetSelectorType.JsonPath
    };

    private static string Serialize(IArazzoSerializable value, ArazzoSpecVersion version)
    {
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text);
        if (version is ArazzoSpecVersion.Arazzo1_0) value.SerializeAsV1(writer);
        else value.SerializeAsV1_1(writer);
        return text.ToString();
    }
}