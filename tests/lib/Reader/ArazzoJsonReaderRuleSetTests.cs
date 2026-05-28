// Licensed under the MIT license.

using System.Net;
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoJsonReaderRuleSetTests
{
    private const string ValidJson = """
        {
          "Arazzo": "1.0.0",
          "info": { "title": "T", "version": "1" },
          "sourceDescriptions": [ { "name": "s", "url": "https://example.com", "type": "openapi" } ]
        }
        """;

    [Fact]
    public async Task ReadAsync_WithEmptyRuleSet_BypassesValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidJson));
        var settings = new ArazzoReaderSettings();
        settings.OpenApiSettings.RuleSet = ValidationRuleSet.GetEmptyRuleSet();

        var result = await reader.ReadAsync(stream, new Uri("https://example.com/"), settings, ct);

        Assert.NotNull(result.Document);
    }

    [Fact]
    public async Task ReadAsync_WithDefaultRuleSet_RunsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = new ArazzoJsonReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidJson));
        var settings = new ArazzoReaderSettings();
        settings.OpenApiSettings.RuleSet = ValidationRuleSet.GetDefaultRuleSet();

        var result = await reader.ReadAsync(stream, new Uri("https://example.com/"), settings, ct);

        Assert.NotNull(result.Document);
        Assert.NotNull(result.Diagnostic);
    }
}