using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoVersionDeserializerTests
{
    [Fact]
    public void BaseArazzoVersionService_LoadElement_ReturnsLoadedElementWhenTypeMatches()
    {
        var service = new TestVersionService(static () => new ArazzoInfo { Title = "loaded" });

        var element = service.LoadElement<ArazzoInfo>(JsonNode.Parse("""{}""")!, new ParsingContext(new()));

        Assert.NotNull(element);
        Assert.Equal("loaded", element.Title);
    }
    [Fact]
    public void BaseArazzoVersionService_LoadElement_ReturnsNullWhenLoaderMissingOrTypeMismatches()
    {
        var mismatchedService = new TestVersionService(static () => new JsonNodeExtension(JsonValue.Create("value")!));

        Assert.Null(mismatchedService.LoadElement<ArazzoInfo>(JsonNode.Parse("""{}""")!, new ParsingContext(new())));

        var missingLoaderService = new TestVersionService(static () => new ArazzoInfo());

        Assert.Null(missingLoaderService.LoadElement<ArazzoSourceDescription>(JsonNode.Parse("""{}""")!, new ParsingContext(new())));
    }

    [Fact]
    public void LoadDocument_UsesParsingContextBaseUrlWhenPresent()
    {
        var json = JsonNode.Parse(
            """
            {
              "arazzo": "1.0.0",
              "info": {
                "title": "Test",
                "version": "1.0.0"
              },
              "sourceDescriptions": [],
              "workflows": []
            }
            """)!;
        var context = new ParsingContext(new())
        {
            BaseUrl = new Uri("https://example.com/from-context/arazzo.json")
        };

        var document = Arazzo.Reader.V1.ArazzoV1Deserializer.LoadDocument(json, context);

        Assert.Equal(context.BaseUrl, document.BaseUri);
    }

    [Theory]
    [InlineData("#/components/inputs/shared", "shared")]
    [InlineData("#/components/inputs/shared/", "shared")]
    [InlineData("$components.parameters.shared", "shared")]
    [InlineData("external.json#$components.successActions.shared", "shared")]
    [InlineData("external.json/shared", "shared")]
    [InlineData("shared", "shared")]
    public void GetReferenceId_ExtractsExpectedIdentifier(string referenceString, string expectedId)
    {
        var result = global::BinkyLabs.OpenApi.Arazzo.Reader.V1.ArazzoV1Deserializer.GetReferenceId(referenceString);

        Assert.Equal(expectedId, result);
    }

    [Theory]
    [InlineData("external.json#/components/inputs/shared", "external.json")]
    [InlineData("external.json#$components.failureActions.shared", "external.json")]
    [InlineData("#/components/inputs/shared", null)]
    [InlineData("shared", null)]
    public void GetExternalResource_ReturnsExpectedPrefix(string referenceString, string? expectedExternalResource)
    {
        var result = global::BinkyLabs.OpenApi.Arazzo.Reader.V1.ArazzoV1Deserializer.GetExternalResource(referenceString);

        Assert.Equal(expectedExternalResource, result);
    }

    private sealed class TestVersionService(Func<IOpenApiElement> loader) : global::BinkyLabs.OpenApi.Arazzo.Reader.BaseArazzoVersionService
    {
        protected override Dictionary<Type, Func<JsonNode, ParsingContext, object?>> Loaders { get; } = new()
        {
            [typeof(ArazzoInfo)] = (_, _) => loader()
        };

        public override ArazzoDocument LoadDocument(JsonNode jsonNode, Uri location, ParsingContext context)
        {
            return new ArazzoDocument { BaseUri = location };
        }
    }
}