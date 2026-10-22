using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoParameterUnionTests
{
    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void WorkflowParameters_RoundTripBothBranches(ArazzoSpecVersion version)
    {
        var parameter = new ArazzoParameter { Name = "id", Value = "literal" };
        var reference = new ArazzoParameterReference("shared") { Value = JsonValue.Create("override") };
        var workflow = new ArazzoWorkflow
        {
            WorkflowId = "test",
            Steps = [new() { StepId = "s", WorkflowId = "other" }],
            Parameters = [parameter, reference]
        };
        var json = Serialize(workflow, version);
        Assert.Equal("id", json["parameters"]?[0]?["name"]?.GetValue<string>());
        Assert.Equal("$components.parameters.shared", json["parameters"]?[1]?["reference"]?.GetValue<string>());
        Assert.Null(json["parameters"]?[0]?["Parameter"]);
        var context = new ParsingContext(new());
        var parsed = context.ParseFragment<ArazzoWorkflow>(json, version);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Parameters);
        Assert.NotNull(parsed.Parameters[0].Parameter);
        Assert.NotNull(parsed.Parameters[1].Reference);
        Assert.Equal("override", parsed.Parameters[1].Reference?.Value?.GetValue<string>());
        Assert.Empty(context.Diagnostic.Errors);
        Assert.True(JsonNode.DeepEquals(json, Serialize(parsed, version)));
    }

    [Fact]
    public void Conversions_PreserveIdentityAndRejectInvalidStates()
    {
        var parameter = new ArazzoParameter { Name = "id", Value = "value" };
        var reference = new ArazzoParameterReference("shared");
        ArazzoParameterUnion value = parameter;
        ArazzoParameter actual = value;
        Assert.Same(parameter, actual);
        Assert.Throws<InvalidCastException>(() => (ArazzoParameterReference)value);
        value = reference;
        ArazzoParameterReference actualReference = value;
        Assert.Same(reference, actualReference);
        Assert.Throws<InvalidCastException>(() => (ArazzoParameter)value);
        value.Parameter = parameter;
        Assert.Throws<InvalidCastException>(() => (ArazzoParameter)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoParameterReference)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_1));
        value = new();
        Assert.Throws<InvalidCastException>(() => (ArazzoParameter)value);
        Assert.Throws<InvalidCastException>(() => (ArazzoParameterReference)value);
        Assert.Throws<ArazzoSerializationException>(() => Serialize(value, ArazzoSpecVersion.Arazzo1_0));
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameterUnion)(ArazzoParameter)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameterUnion)(ArazzoParameterReference)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameter)(ArazzoParameterUnion)null!);
        Assert.Throws<ArgumentNullException>(() => (ArazzoParameterReference)(ArazzoParameterUnion)null!);
    }

    [Theory]
    [InlineData(ArazzoSpecVersion.Arazzo1_0)]
    [InlineData(ArazzoSpecVersion.Arazzo1_1)]
    public void Reader_RejectsConflictingBranches(ArazzoSpecVersion version)
    {
        var json = JsonNode.Parse("""{"reference":"$components.parameters.shared","name":"id","value":"override"}""");
        Assert.NotNull(json);
        Assert.Throws<ArazzoReaderException>(() => new ParsingContext(new()).ParseFragment<ArazzoParameterUnion>(json, version));
    }

    private static JsonNode Serialize(IArazzoSerializable value, ArazzoSpecVersion version)
    {
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text);
        if (version is ArazzoSpecVersion.Arazzo1_0) value.SerializeAsV1(writer);
        else value.SerializeAsV1_1(writer);
        var node = JsonNode.Parse(text.ToString());
        Assert.NotNull(node);
        return node;
    }
}