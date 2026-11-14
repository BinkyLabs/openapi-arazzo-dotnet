using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Reader;

internal sealed class ArazzoWorkspaceLoader
{
    private readonly ArazzoWorkspace _workspace;
    private readonly IStreamLoader _loader;
    private readonly ArazzoReaderSettings _readerSettings;
    private readonly ArazzoReaderSettings _leaveOpenReaderSettings;
    private readonly ArazzoReaderSettings _nonRecursiveReaderSettings;

    internal ArazzoDiagnostic Diagnostic { get; } = new();

    public ArazzoWorkspaceLoader(ArazzoWorkspace workspace, IStreamLoader loader, ArazzoReaderSettings readerSettings)
    {
        _workspace = workspace;
        _loader = loader;
        _readerSettings = readerSettings;
        _leaveOpenReaderSettings = CloneReaderSettingsWithLeaveOpen(readerSettings);
        _nonRecursiveReaderSettings = CloneReaderSettingsWithLeaveOpen(readerSettings);
        _nonRecursiveReaderSettings.OpenApiSettings.LoadExternalRefs = false;
    }
    internal async Task LoadAsync(
        BaseArazzoReference reference,
        ArazzoDocument? document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _workspace.AddDocumentId(reference.ExternalResource, document?.BaseUri);
        if (document is not null)
        {
            _workspace.RegisterComponents(document);
            document.Workspace = _workspace;
        }

        await LoadSourceDescriptionsAsync(document, cancellationToken).ConfigureAwait(false);

        foreach (var remoteReference in CollectRemoteReferences(document))
        {
            if (remoteReference.ExternalResource is null)
            {
                continue;
            }

            var inputUri = new Uri(remoteReference.ExternalResource, UriKind.RelativeOrAbsolute);
            var referenceBaseUri = _workspace.GetInputReferenceBaseUri(remoteReference) ?? remoteReference.HostDocument!.BaseUri;
            var resolvedUri = new Uri(referenceBaseUri, inputUri);
            if (_workspace.TryGetArazzoDocument(resolvedUri, out var loadedDocument))
            {
                _workspace.AddDocumentId(remoteReference.ExternalResource, loadedDocument.BaseUri);
                continue;
            }
            if (_workspace.Contains(resolvedUri.AbsoluteUri) || _workspace.IsOpenApiDocumentLoaded(resolvedUri))
            {
                continue;
            }

            await using var stream = await _loader.LoadAsync(referenceBaseUri, inputUri, cancellationToken).ConfigureAwait(false);
            await using var bufferedStream = new MemoryStream();
            await stream.CopyToAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
            ResetStream(bufferedStream);
            var result = await ArazzoModelFactory.LoadFromStreamAsync(
                bufferedStream,
                null,
                _nonRecursiveReaderSettings,
                cancellationToken,
                resolvedUri).ConfigureAwait(false);

            if (result.Document is not null)
            {
                _workspace.RegisterArazzoDocument(resolvedUri, result.Document);
                await LoadAsync(remoteReference, result.Document, cancellationToken).ConfigureAwait(false);
                continue;
            }

            ResetStream(bufferedStream);
            if (await TryLoadExternalOpenApiDocumentAsync(bufferedStream, resolvedUri, cancellationToken).ConfigureAwait(false))
            {
                _workspace.AddDocumentId(remoteReference.ExternalResource, resolvedUri);
                _workspace.AddDocumentId(resolvedUri.AbsoluteUri, resolvedUri);
                continue;
            }

            ResetStream(bufferedStream);
            if (await TryLoadExternalSchemaAsync(bufferedStream, resolvedUri, cancellationToken).ConfigureAwait(false) is { } externalSchema)
            {
                _workspace.AddDocumentId(remoteReference.ExternalResource, resolvedUri);
                _workspace.AddDocumentId(resolvedUri.AbsoluteUri, resolvedUri);
                _workspace.RegisterInputSchema(resolvedUri.AbsoluteUri, externalSchema);
            }
        }
    }

    private async Task<IArazzoInput?> TryLoadExternalSchemaAsync(MemoryStream stream, Uri resolvedUri, CancellationToken cancellationToken)
    {
        try
        {
            return await ReadExternalSchemaAsync(stream, resolvedUri, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<bool> TryLoadExternalOpenApiDocumentAsync(MemoryStream stream, Uri resolvedUri, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ReadExternalOpenApiDocumentAsync(stream, resolvedUri, cancellationToken).ConfigureAwait(false);
            if (result.Document is null)
            {
                return false;
            }

            _workspace.RegisterOpenApiDocument(resolvedUri, result.Document);

            if (result.Document.Components?.Schemas is null)
            {
                return true;
            }

            var hostDocument = new ArazzoDocument
            {
                BaseUri = resolvedUri,
                Workspace = _workspace
            };
            var sourceNode = await ReadSourceNodeAsync(stream, cancellationToken).ConfigureAwait(false);
            var sourceComponents = (sourceNode as JsonObject)?["components"] as JsonObject;
            var sourceSchemas = sourceComponents?["schemas"] as JsonObject;

            foreach (var schema in result.Document.Components.Schemas)
            {
                _workspace.RegisterInputSchema(
                    $"{resolvedUri.AbsoluteUri}#/components/schemas/{schema.Key}",
                    ArazzoInput.ConvertFromOpenApiSchema(schema.Value, hostDocument, sourceSchemas?[schema.Key]));
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task LoadSourceDescriptionsAsync(ArazzoDocument? document, CancellationToken cancellationToken)
    {
        if (document?.SourceDescriptions is null)
        {
            return;
        }

        foreach (var sourceDescription in document.SourceDescriptions)
        {
            if (sourceDescription.Url is null ||
                string.IsNullOrEmpty(sourceDescription.Name))
            {
                continue;
            }

            var resolvedUri = new Uri(document.BaseUri, sourceDescription.Url);
            if (sourceDescription.Type is ArazzoDescriptionType.Arazzo)
            {
                await LoadArazzoSourceDescriptionAsync(document, sourceDescription, resolvedUri, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (_workspace.IsOpenApiDocumentLoaded(resolvedUri) || !ShouldTryLoadSourceDescription(sourceDescription.Url))
            {
                continue;
            }

            try
            {
                await using var stream = await _loader.LoadAsync(document.BaseUri, sourceDescription.Url, cancellationToken).ConfigureAwait(false);
                await using var bufferedStream = new MemoryStream();
                await stream.CopyToAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
                ResetStream(bufferedStream);
                await TryLoadExternalOpenApiDocumentAsync(bufferedStream, resolvedUri, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Source descriptions that are not locally available are intentionally left unresolved.
            }
        }
    }

    private async Task LoadArazzoSourceDescriptionAsync(
        ArazzoDocument hostDocument,
        ArazzoSourceDescription sourceDescription,
        Uri resolvedUri,
        CancellationToken cancellationToken)
    {
        if (_workspace.TryGetArazzoDocument(resolvedUri, out _))
        {
            return;
        }

        try
        {
            await using var stream = await _loader.LoadAsync(hostDocument.BaseUri, resolvedUri, cancellationToken).ConfigureAwait(false);
            var result = await ArazzoModelFactory.LoadFromStreamAsync(
                stream, null, _nonRecursiveReaderSettings, cancellationToken, resolvedUri).ConfigureAwait(false);
            var elementName = $"Arazzo sourceDescription '{sourceDescription.Name}' at '{resolvedUri}'";
            foreach (var error in result.Diagnostic?.Errors ?? [])
            {
                Diagnostic.Errors.Add(new OpenApiError(error.Pointer, $"{elementName}: {error.Message}"));
            }
            foreach (var warning in result.Diagnostic?.Warnings ?? [])
            {
                Diagnostic.Warnings.Add(new OpenApiError(warning.Pointer, $"{elementName}: {warning.Message}"));
            }

            if (result.Document is null)
            {
                if (result.Diagnostic?.Errors.Count is not > 0)
                {
                    Diagnostic.Errors.Add(new OpenApiError(string.Empty, $"{elementName} could not be parsed as an Arazzo document."));
                }
                return;
            }

            // Register retrieval and $self URIs before following sources so cycles reuse the loaded document.
            _workspace.RegisterArazzoDocument(resolvedUri, result.Document);
            await LoadAsync(new BaseArazzoReference { ExternalResource = resolvedUri.AbsoluteUri },
                result.Document, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or OpenApiException or SharpYaml.YamlException)
        {
            Diagnostic.Errors.Add(new OpenApiError(string.Empty,
                $"Arazzo sourceDescription '{sourceDescription.Name}' at '{resolvedUri}' could not be loaded: {ex.Message}"));
        }
    }

    private bool ShouldTryLoadSourceDescription(Uri sourceDescriptionUrl)
    {
        if (!sourceDescriptionUrl.IsAbsoluteUri || sourceDescriptionUrl.IsFile)
        {
            return true;
        }

        return _readerSettings.OpenApiSettings.CustomExternalLoader is not null &&
            !sourceDescriptionUrl.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !sourceDescriptionUrl.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IArazzoInput?> ReadExternalSchemaAsync(MemoryStream stream, Uri resolvedUri, CancellationToken cancellationToken)
    {
        var hostDocument = new ArazzoDocument
        {
            BaseUri = resolvedUri,
            Workspace = _workspace
        };

        var schema = await OpenApiModelFactory.LoadAsync<OpenApiSchema>(stream, OpenApiSpecVersion.OpenApi3_2, new(), settings: _leaveOpenReaderSettings.OpenApiSettings, token: cancellationToken).ConfigureAwait(false);

        if (schema is null)
        {
            return null;
        }
        var sourceNode = await ReadSourceNodeAsync(stream, cancellationToken).ConfigureAwait(false);
        return ArazzoInput.ConvertFromOpenApiSchema(schema, hostDocument, sourceNode);
    }

    private async Task<Microsoft.OpenApi.Reader.ReadResult> ReadExternalOpenApiDocumentAsync(MemoryStream stream, Uri resolvedUri, CancellationToken cancellationToken)
    {
        ResetStream(stream);
        return await OpenApiDocument.LoadAsync(
            stream,
            null,
            _leaveOpenReaderSettings.OpenApiSettings,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<JsonNode?> TryParseJsonNodeAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            ResetStream(stream);
            return null;
        }
    }

    private static async Task<JsonNode?> ReadSourceNodeAsync(MemoryStream stream, CancellationToken cancellationToken)
    {
        ResetStream(stream);
        if (await TryParseJsonNodeAsync(stream, cancellationToken).ConfigureAwait(false) is { } jsonNode)
        {
            return jsonNode;
        }
        using var copy = new MemoryStream(stream.ToArray());
        return await new ArazzoYamlReader().GetJsonNodeFromStreamAsync(copy, cancellationToken).ConfigureAwait(false);
    }

    private static void ResetStream(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }
    }


    private static ArazzoReaderSettings CloneReaderSettingsWithLeaveOpen(ArazzoReaderSettings settings)
    {
        return new ArazzoReaderSettings
        {
            HttpClient = settings.HttpClient,
            Readers = settings.Readers,
            OpenApiSettings = CloneOpenApiReaderSettingsWithLeaveOpen(settings.OpenApiSettings),
            ExtensionParsers = settings.ExtensionParsers
        };
    }

    private static OpenApiReaderSettings CloneOpenApiReaderSettingsWithLeaveOpen(OpenApiReaderSettings settings)
    {
        var clone = new OpenApiReaderSettings
        {
            Readers = settings.Readers,
            LoadExternalRefs = settings.LoadExternalRefs,
            ExtensionParsers = settings.ExtensionParsers,
            RuleSet = settings.RuleSet,
            BaseUrl = settings.BaseUrl,
            DefaultContentType = settings.DefaultContentType,
            CustomExternalLoader = settings.CustomExternalLoader,
            LeaveStreamOpen = true
        };

        OpenApiReaderSettingsExtensions.AddYamlReader(clone);
        return clone;
    }

    private static IEnumerable<BaseArazzoReference> CollectRemoteReferences(ArazzoDocument? document)
    {
        if (document is null)
        {
            yield break;
        }

        if (document.Components?.Inputs is not null)
        {
            foreach (var input in document.Components.Inputs.Values)
            {
                foreach (var reference in CollectRemoteReferences(input))
                {
                    yield return reference;
                }
            }
        }

        if (document.Workflows is not null)
        {
            foreach (var workflow in document.Workflows)
            {
                if (workflow.Inputs is null)
                {
                    continue;
                }

                foreach (var reference in CollectRemoteReferences(workflow.Inputs))
                {
                    yield return reference;
                }
            }
        }
    }

    private static IEnumerable<BaseArazzoReference> CollectRemoteReferences(IArazzoInput input)
    {
        if (input is ArazzoInputReference reference)
        {
            if (reference.Reference.IsExternal)
            {
                yield return reference.Reference;
            }
            yield break;
        }

        if (input.Definitions is not null)
        {
            foreach (var child in input.Definitions.Values.SelectMany(CollectRemoteReferences))
            {
                yield return child;
            }
        }

        if (input.AllOf is not null)
        {
            foreach (var child in input.AllOf.SelectMany(CollectRemoteReferences))
            {
                yield return child;
            }
        }

        if (input.OneOf is not null)
        {
            foreach (var child in input.OneOf.SelectMany(CollectRemoteReferences))
            {
                yield return child;
            }
        }

        if (input.AnyOf is not null)
        {
            foreach (var child in input.AnyOf.SelectMany(CollectRemoteReferences))
            {
                yield return child;
            }
        }

        if (input.Not is not null)
        {
            foreach (var child in CollectRemoteReferences(input.Not))
            {
                yield return child;
            }
        }

        if (input.Items is not null)
        {
            foreach (var child in CollectRemoteReferences(input.Items))
            {
                yield return child;
            }
        }

        if (input.Properties is not null)
        {
            foreach (var child in input.Properties.Values.SelectMany(CollectRemoteReferences))
            {
                yield return child;
            }
        }

        if (input.PatternProperties is not null)
        {
            foreach (var child in input.PatternProperties.Values.SelectMany(CollectRemoteReferences))
            {
                yield return child;
            }
        }

        if (input.AdditionalProperties is not null)
        {
            foreach (var child in CollectRemoteReferences(input.AdditionalProperties))
            {
                yield return child;
            }
        }

        if (input.UnevaluatedPropertiesSchema is not null)
        {
            foreach (var child in CollectRemoteReferences(input.UnevaluatedPropertiesSchema))
            {
                yield return child;
            }
        }
    }
}