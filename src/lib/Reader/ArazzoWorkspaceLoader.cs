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

    public ArazzoWorkspaceLoader(ArazzoWorkspace workspace, IStreamLoader loader, ArazzoReaderSettings readerSettings)
    {
        _workspace = workspace;
        _loader = loader;
        _readerSettings = readerSettings;
    }
    internal async Task LoadAsync(
        BaseArazzoReference reference,
        ArazzoDocument? document,
        CancellationToken cancellationToken = default)
    {
        _workspace.AddDocumentId(reference.ExternalResource, document?.BaseUri);
        if (document is not null)
        {
            _workspace.RegisterComponents(document);
            document.Workspace = _workspace;
        }

        foreach (var remoteReference in CollectRemoteReferences(document))
        {
            if (remoteReference.ExternalResource is null || _workspace.Contains(remoteReference.ExternalResource))
            {
                continue;
            }

            var inputUri = new Uri(remoteReference.ExternalResource, UriKind.RelativeOrAbsolute);
            await using var stream = await _loader.LoadAsync(remoteReference.HostDocument!.BaseUri, inputUri, cancellationToken).ConfigureAwait(false);
            var resolvedUri = new Uri(remoteReference.HostDocument.BaseUri, inputUri);
            await using var bufferedStream = new MemoryStream();
            await stream.CopyToAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
            bufferedStream.Position = 0;
            var result = await ArazzoModelFactory.LoadFromStreamAsync(
                bufferedStream,
                null,
                _readerSettings,
                cancellationToken,
                resolvedUri).ConfigureAwait(false);

            if (result.Document is not null)
            {
                await LoadAsync(remoteReference, result.Document, cancellationToken).ConfigureAwait(false);
                continue;
            }

            bufferedStream.Position = 0;
            if (await TryLoadExternalSchemaAsync(bufferedStream, resolvedUri, cancellationToken).ConfigureAwait(false) is { } externalSchema)
            {
                _workspace.AddDocumentId(remoteReference.ExternalResource, resolvedUri);
                _workspace.RegisterInputSchema(resolvedUri.AbsoluteUri, externalSchema);
            }
        }
    }

    private async Task<IArazzoInput?> TryLoadExternalSchemaAsync(Stream stream, Uri resolvedUri, CancellationToken cancellationToken)
    {
        try
        {
            var node = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (node is null)
            {
                return null;
            }

            var schema = new OpenApiJsonReader().ReadFragment<OpenApiSchema>(node, OpenApiSpecVersion.OpenApi3_2, new(), out var _);
            if (schema is not OpenApiSchema openApiSchema)
            {
                return null;
            }

            var hostDocument = new ArazzoDocument
            {
                BaseUri = resolvedUri,
                Workspace = _workspace
            };

            return ArazzoInput.ConvertFromOpenApiSchema(openApiSchema, hostDocument);
        }
        catch (JsonException)
        {
            return null;
        }
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
        if (input is ArazzoInputReference reference && reference.Reference.IsExternal)
        {
            yield return reference.Reference;
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