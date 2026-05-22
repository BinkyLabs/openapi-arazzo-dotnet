// Licensed under the MIT license.

using System.Text;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoJsonReaderTests
{
    [Fact]
    public async Task ReadAsync_InvalidJson_AddsErrorAndReturnsNullDocument()
    {
        var reader = new ArazzoJsonReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{ not json"));
        var result = await reader.ReadAsync(stream, new Uri("https://example.com/"), new ArazzoReaderSettings());

        Assert.Null(result.Document);
        Assert.NotNull(result.Diagnostic);
        Assert.NotEmpty(result.Diagnostic!.Errors);
    }

    [Fact]
    public async Task ReadAsync_NullStream_Throws()
    {
        var reader = new ArazzoJsonReader();
        await Assert.ThrowsAsync<ArgumentNullException>(() => reader.ReadAsync(null!, new Uri("https://example.com/"), new ArazzoReaderSettings()));
    }

    [Fact]
    public async Task ReadAsync_NullSettings_Throws()
    {
        var reader = new ArazzoJsonReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => reader.ReadAsync(stream, new Uri("https://example.com/"), null!));
    }

    [Fact]
    public async Task ReadAsync_UnsupportedSpecVersion_AddsErrorAndReturnsResult()
    {
        var reader = new ArazzoJsonReader();
        var json = """{"arazzo":"99.0.0","info":{"title":"T","version":"1"}}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await reader.ReadAsync(stream, new Uri("https://example.com/"), new ArazzoReaderSettings());

        Assert.Null(result.Document);
        Assert.NotEmpty(result.Diagnostic!.Errors);
    }

    [Fact]
    public async Task GetJsonNodeFromStreamAsync_ReturnsJsonNode()
    {
        var reader = new ArazzoJsonReader();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{"a":1}"""));
        var node = await reader.GetJsonNodeFromStreamAsync(stream);
        Assert.NotNull(node);
        Assert.Equal(1, node!["a"]!.GetValue<int>());
    }
}
