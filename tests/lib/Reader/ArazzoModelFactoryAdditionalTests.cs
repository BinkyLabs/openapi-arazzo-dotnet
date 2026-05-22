// Licensed under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Tests.Reader;

public class ArazzoModelFactoryAdditionalTests
{
    private const string DocumentJson =
        """
        {
          "Arazzo": "1.0.0",
          "info": { "title": "T", "version": "1" },
          "sourceDescriptions": []
        }
        """;

    [Fact]
    public async Task LoadFormUrlAsync_EmptyUrl_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => ArazzoModelFactory.LoadFormUrlAsync(""));
    }

    [Fact]
    public async Task LoadFormUrlAsync_NonExistentFile_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ArazzoModelFactory.LoadFormUrlAsync("nonexistent-file-9f81ab.arazzo.json"));
    }

    [Fact]
    public async Task LoadFormUrlAsync_LocalJsonFile_LoadsDocument()
    {
        var path = Path.Combine(Path.GetTempPath(), $"arazzo-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, DocumentJson);
        try
        {
            var result = await ArazzoModelFactory.LoadFormUrlAsync(path);
            Assert.NotNull(result.Document);
            Assert.Equal("T", result.Document!.Info!.Title);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadFormUrlAsync_HttpUrl_UsesHttpClient()
    {
        var handler = new StubHttpHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(DocumentJson, Encoding.UTF8, "application/json")
        });
        var settings = new ArazzoReaderSettings { HttpClient = new HttpClient(handler) };

        var result = await ArazzoModelFactory.LoadFormUrlAsync("https://example.com/spec.json", settings);

        Assert.NotNull(result.Document);
        Assert.Equal("T", result.Document!.Info!.Title);
    }

    [Fact]
    public async Task ParseAsync_EmptyInput_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => ArazzoModelFactory.ParseAsync(""));
    }

    [Fact]
    public async Task ParseAsync_YamlInput_DetectsYamlFormat()
    {
        var yaml = """
            Arazzo: 1.0.0
            info:
              title: T
              version: '1'
            sourceDescriptions: []
            """;
        var result = await ArazzoModelFactory.ParseAsync(yaml);
        Assert.NotNull(result.Document);
    }

    [Fact]
    public async Task LoadFromStreamAsync_NullStream_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => ArazzoModelFactory.LoadFromStreamAsync(null!));
    }

    [Fact]
    public async Task LoadFromStreamAsync_ExplicitFormat_BypassesDetection()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(DocumentJson));
        var result = await ArazzoModelFactory.LoadFromStreamAsync(ms, format: OpenApiConstants.Json);
        Assert.NotNull(result.Document);
    }

    [Fact]
    public async Task LoadFromStreamAsync_FromFileStream_UsesFileUri()
    {
        var path = Path.Combine(Path.GetTempPath(), $"arazzo-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, DocumentJson);
        try
        {
            using var fs = File.OpenRead(path);
            var result = await ArazzoModelFactory.LoadFromStreamAsync(fs);
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
