// Licensed under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public sealed partial class ArazzoModelFactoryTests
{
    [Fact]
    public async Task LoadFormUrlAsync_EmptyUrl_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<ArgumentException>(() => ArazzoModelFactory.LoadFormUrlAsync("", token: ct));
    }

    [Fact]
    public async Task LoadFormUrlAsync_NonExistentFile_ThrowsInvalidOperation()
    {
        var ct = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ArazzoModelFactory.LoadFormUrlAsync("nonexistent-file-9f81ab.arazzo.json", token: ct));
    }

    [Fact]
    public async Task LoadFormUrlAsync_LocalJsonFile_LoadsDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        var path = Path.Combine(Path.GetTempPath(), $"arazzo-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, documentJson, ct);
        try
        {
            var result = await ArazzoModelFactory.LoadFormUrlAsync(path, token: ct);
            Assert.NotNull(result.Document);
            Assert.Equal("Sample Arazzo", result.Document!.Info!.Title);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadFormUrlAsync_HttpUrl_UsesHttpClient()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new StubHttpHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(documentJson, Encoding.UTF8, "application/json")
        });
        var settings = new ArazzoReaderSettings { HttpClient = new HttpClient(handler) };

        var result = await ArazzoModelFactory.LoadFormUrlAsync("https://example.com/spec.json", settings, ct);

        Assert.NotNull(result.Document);
        Assert.Equal("Sample Arazzo", result.Document!.Info!.Title);
    }

    [Fact]
    public async Task ParseAsync_EmptyInput_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<ArgumentException>(() => ArazzoModelFactory.ParseAsync("", cancellationToken: ct));
    }

    [Fact]
    public async Task ParseAsync_YamlInput_DetectsYamlFormat()
    {
        var ct = TestContext.Current.CancellationToken;
        var yaml = """
            arazzo: 1.0.0
            info:
              title: T
              version: '1'
            sourceDescriptions:
              - name: source1
                url: https://example.com/api
                type: openapi
            workflows:
              - workflowId: workflow1
                steps:
                  - stepId: step1
            """;
        var result = await ArazzoModelFactory.ParseAsync(yaml, cancellationToken: ct);
        Assert.NotNull(result.Document);
    }

    [Fact]
    public async Task LoadFromStreamAsync_NullStream_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        await Assert.ThrowsAsync<ArgumentNullException>(() => ArazzoModelFactory.LoadFromStreamAsync(null!, cancellationToken: ct));
    }

    [Fact]
    public async Task LoadFromStreamAsync_ExplicitFormat_BypassesDetection()
    {
        var ct = TestContext.Current.CancellationToken;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(documentJson));
        var result = await ArazzoModelFactory.LoadFromStreamAsync(ms, format: OpenApiConstants.Json, cancellationToken: ct);
        Assert.NotNull(result.Document);
    }

    [Fact]
    public async Task LoadFromStreamAsync_FromFileStream_UsesFileUri()
    {
        var ct = TestContext.Current.CancellationToken;
        var path = Path.Combine(Path.GetTempPath(), $"arazzo-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, documentJson, ct);
        try
        {
            using var fs = File.OpenRead(path);
            var result = await ArazzoModelFactory.LoadFromStreamAsync(fs, cancellationToken: ct);
            Assert.NotNull(result.Document);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}