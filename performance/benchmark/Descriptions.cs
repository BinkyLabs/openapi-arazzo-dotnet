using System.Reflection;

using BenchmarkDotNet.Attributes;

using BinkyLabs.OpenApi.Arazzo;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace performance;

[MemoryDiagnoser]
[JsonExporter]
[ShortRunJob]
public class Descriptions
{
    private readonly Dictionary<string, MemoryStream> _streams = new(StringComparer.OrdinalIgnoreCase);
    private ArazzoReaderSettings _readerSettings = null!;

    [Benchmark]
    public async Task<ArazzoDocument?> BnplYaml()
    {
        return await ParseDocumentAsync(BnplYamlPath).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<ArazzoDocument?> BnplJson()
    {
        return await ParseDocumentAsync(BnplJsonPath, OpenApiConstants.Json).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<ArazzoDocument?> FormalBnplYaml()
    {
        return await ParseDocumentAsync(FormalBnplYamlPath).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<ArazzoDocument?> FormalBnplJson()
    {
        return await ParseDocumentAsync(FormalBnplJsonPath, OpenApiConstants.Json).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<ArazzoDocument?> FapiParYaml()
    {
        return await ParseDocumentAsync(FapiParYamlPath).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<ArazzoDocument?> FapiParJson()
    {
        return await ParseDocumentAsync(FapiParJsonPath, OpenApiConstants.Json).ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<ArazzoDocument?> MinimalJson()
    {
        return await ParseDocumentAsync(MinimalJsonPath, OpenApiConstants.Json).ConfigureAwait(false);
    }

    [GlobalSetup]
    public async Task GetAllDescriptions()
    {
        _readerSettings = new ArazzoReaderSettings();
        _readerSettings.OpenApiSettings.RuleSet = ValidationRuleSet.GetEmptyRuleSet();
        _readerSettings.OpenApiSettings.LeaveStreamOpen = true;

        await LoadDocumentFromAssemblyIntoStreamsAsync(BnplYamlPath).ConfigureAwait(false);
        await LoadDocumentFromAssemblyIntoStreamsAsync(BnplJsonPath).ConfigureAwait(false);
        await LoadDocumentFromAssemblyIntoStreamsAsync(FormalBnplYamlPath).ConfigureAwait(false);
        await LoadDocumentFromAssemblyIntoStreamsAsync(FormalBnplJsonPath).ConfigureAwait(false);
        await LoadDocumentFromAssemblyIntoStreamsAsync(FapiParYamlPath).ConfigureAwait(false);
        await LoadDocumentFromAssemblyIntoStreamsAsync(FapiParJsonPath).ConfigureAwait(false);
        await LoadDocumentFromAssemblyIntoStreamsAsync(MinimalJsonPath).ConfigureAwait(false);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var stream in _streams.Values)
        {
            stream.Dispose();
        }

        _streams.Clear();
    }

    private const string BnplYamlPath = "bnpl-arazzo.yaml";
    private const string BnplJsonPath = "bnpl-arazzo.json";
    private const string FormalBnplYamlPath = "formal-bnpl.arazzo.yaml";
    private const string FormalBnplJsonPath = "formal-bnpl.arazzo.json";
    private const string FapiParYamlPath = "FAPI-PAR.arazzo.yaml";
    private const string FapiParJsonPath = "FAPI-PAR.arazzo.json";
    private const string MinimalJsonPath = "minimal-arazzo.json";

    private async Task<ArazzoDocument?> ParseDocumentAsync(string fileName, string format = OpenApiConstants.Yaml)
    {
        var stream = _streams[fileName];
        stream.Seek(0, SeekOrigin.Begin);

        var result = await ArazzoDocument.LoadFromStreamAsync(stream, format, _readerSettings).ConfigureAwait(false);
        return result.Document;
    }

    private static readonly Assembly Assembly = typeof(Descriptions).GetTypeInfo().Assembly;

    private async Task LoadDocumentFromAssemblyIntoStreamsAsync(string fileName)
    {
        await using var resource = Assembly.GetManifestResourceStream(fileName)
            ?? throw new InvalidOperationException($"Embedded benchmark resource '{fileName}' was not found.");
        var stream = new MemoryStream();
        await resource.CopyToAsync(stream).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
        _streams.Add(fileName, stream);
    }
}