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
        string? format = null,
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
            var result = await ArazzoModelFactory.LoadFromStreamAsync(
                stream,
                format,
                _readerSettings,
                cancellationToken,
                resolvedUri).ConfigureAwait(false);

            if (result.Document is not null)
            {
                await LoadAsync(remoteReference, result.Document, format, cancellationToken).ConfigureAwait(false);
            }
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