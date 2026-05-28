// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Tests;

public class ArazzoReaderSettingsTests
{
    [Fact]
    public void Default_HttpClient_IsLazyShared()
    {
        var settings = new ArazzoReaderSettings();
        Assert.NotNull(settings.HttpClient);
        // Access twice to hit the cached branch
        Assert.Same(settings.HttpClient, settings.HttpClient);
    }

    [Fact]
    public void HttpClient_CanBeInitialized()
    {
        using var http = new HttpClient();
        var settings = new ArazzoReaderSettings { HttpClient = http };
        Assert.Same(http, settings.HttpClient);
    }

    [Fact]
    public void Readers_DefaultHasJsonAndYaml()
    {
        var settings = new ArazzoReaderSettings();
        Assert.True(settings.Readers.ContainsKey(OpenApiConstants.Json));
        Assert.True(settings.Readers.ContainsKey(OpenApiConstants.Yaml));
    }

    [Fact]
    public void Readers_Init_CopiesNonOrdinalIgnoreCaseDictionary()
    {
        var dict = new Dictionary<string, IArazzoReader>(StringComparer.Ordinal)
        {
            { "json", new ArazzoJsonReader() }
        };
        var settings = new ArazzoReaderSettings { Readers = dict };

        Assert.True(settings.Readers.ContainsKey("JSON"));
    }

    [Fact]
    public void Readers_Init_KeepsOrdinalIgnoreCaseDictionary()
    {
        var dict = new Dictionary<string, IArazzoReader>(StringComparer.OrdinalIgnoreCase)
        {
            { "json", new ArazzoJsonReader() }
        };
        var settings = new ArazzoReaderSettings { Readers = dict };

        Assert.Same(dict, settings.Readers);
    }

    [Fact]
    public void Readers_Init_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ArazzoReaderSettings { Readers = null! });
    }

    [Fact]
    public void TryAddReader_AddsNewFormat()
    {
        var settings = new ArazzoReaderSettings();
        var added = settings.TryAddReader("xml", new ArazzoJsonReader());
        Assert.True(added);
        Assert.True(settings.Readers.ContainsKey("xml"));
    }

    [Fact]
    public void TryAddReader_ReturnsFalseWhenExists()
    {
        var settings = new ArazzoReaderSettings();
        var added = settings.TryAddReader(OpenApiConstants.Json, new ArazzoJsonReader());
        Assert.False(added);
    }

    [Fact]
    public void TryAddReader_ThrowsOnNullArgs()
    {
        var settings = new ArazzoReaderSettings();
        Assert.Throws<ArgumentException>(() => settings.TryAddReader("", new ArazzoJsonReader()));
        Assert.Throws<ArgumentNullException>(() => settings.TryAddReader("xml", null!));
    }

    [Fact]
    public void AddJsonReader_NoOpWhenAlreadyPresent()
    {
        var settings = new ArazzoReaderSettings();
        settings.AddJsonReader();
        Assert.True(settings.Readers.ContainsKey(OpenApiConstants.Json));
    }

    [Fact]
    public void GetReader_ReturnsRegisteredReader()
    {
        var settings = new ArazzoReaderSettings();
        var reader = settings.GetReader(OpenApiConstants.Json);
        Assert.NotNull(reader);
    }

    [Fact]
    public void GetReader_ThrowsForUnknownFormat()
    {
        var settings = new ArazzoReaderSettings();
        Assert.Throws<NotSupportedException>(() => settings.GetReader("unknown-format"));
    }

    [Fact]
    public void GetReader_ThrowsOnEmptyFormat()
    {
        var settings = new ArazzoReaderSettings();
        Assert.Throws<ArgumentException>(() => settings.GetReader(""));
    }

    [Fact]
    public void ExtensionParsers_CanBeAssigned()
    {
        var settings = new ArazzoReaderSettings
        {
            ExtensionParsers = new Dictionary<string, Func<JsonNode, ArazzoSpecVersion, IArazzoExtension>>
            {
                ["x-foo"] = (n, _) => new JsonNodeExtension(n)
            }
        };
        Assert.NotNull(settings.ExtensionParsers);
        Assert.True(settings.ExtensionParsers!.ContainsKey("x-foo"));
    }

    [Fact]
    public void OpenApiSettings_IsSettable()
    {
        var settings = new ArazzoReaderSettings { OpenApiSettings = new OpenApiReaderSettings { BaseUrl = new Uri("https://example.com/") } };
        Assert.Equal(new Uri("https://example.com/"), settings.OpenApiSettings.BaseUrl);
    }
}