using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents an Arazzo Document as defined in the OpenAPI Arazzo specification.
/// </summary>
public class ArazzoDocument : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Registers document components into the workspace.
    /// </summary>
    internal void RegisterComponents()
    {
        Workspace?.RegisterComponents(this);
    }

    /// <summary>
    /// Related workspace containing components referenced by the document.
    /// </summary>
    internal ArazzoWorkspace? Workspace { get; set; }

    /// <summary>
    /// Gets or sets the Arazzo version. Default is "1.0.1".
    /// </summary>
    public string? Arazzo { get; internal set; } = "1.0.1";

    /// <summary>
    /// Gets or sets the Arazzo info object.
    /// </summary>
    public ArazzoInfo? Info { get; set; }

    /// <summary>
    /// Gets or sets the source descriptions list.
    /// </summary>
    public IList<ArazzoSourceDescription>? SourceDescriptions { get; set; }

    /// <summary>
    /// Gets or sets the workflows list.
    /// </summary>
    public IList<ArazzoWorkflow>? Workflows { get; set; }

    /// <summary>
    /// Gets or sets the components object.
    /// </summary>
    public ArazzoComponent? Components { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Absolute location of the document or a generated placeholder if location is not given.
    /// </summary>
    internal Uri BaseUri { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArazzoDocument"/> class.
    /// </summary>
    public ArazzoDocument()
    {
        Workspace = new ArazzoWorkspace();
        BaseUri = new Uri(OpenApiConstants.BaseRegistryUri + Guid.NewGuid());
    }

    /// <summary>
    /// Serializes the Arazzo document as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    /// <exception cref="ArazzoSerializationException">Thrown when validation fails.</exception>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        // Validate required fields
        if (Info is null)
        {
            throw new ArazzoSerializationException("Info is required for ArazzoDocument serialization.");
        }

        if (SourceDescriptions is not { Count: > 0 })
        {
            throw new ArazzoSerializationException("SourceDescriptions is required and must contain at least one element for ArazzoDocument serialization.");
        }

        if (Workflows is not { Count: > 0 })
        {
            throw new ArazzoSerializationException("Workflows is required and must contain at least one element for ArazzoDocument serialization.");
        }

        writer.WriteStartObject();
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoDocumentArazzo, "1.0.1");
        writer.WriteRequiredObject(ArazzoConstants.ArazzoDocumentInfo, Info, (w, obj) => obj.SerializeAsV1(w));

        writer.WriteRequiredCollection(ArazzoConstants.ArazzoDocumentSourceDescriptions, SourceDescriptions, static (w, s) => s.SerializeAsV1(w));

        writer.WriteRequiredCollection(ArazzoConstants.ArazzoDocumentWorkflows, Workflows, static (w, wf) => wf.SerializeAsV1(w));

        writer.WriteOptionalObject(ArazzoConstants.ArazzoDocumentComponents, Components, static (w, c) => c.SerializeAsV1(w));

        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses a local file path or Url into an Open API document.
    /// </summary>
    /// <param name="url"> The path to the OpenAPI file.</param>
    /// <param name="settings">The OpenApi reader settings.</param>
    /// <param name="token">The cancellation token</param>
    /// <returns></returns>
    public static async Task<ReadResult> LoadFromUrlAsync(string url, ArazzoReaderSettings? settings = null, CancellationToken token = default)
    {
        return await ArazzoModelFactory.LoadFormUrlAsync(url, settings, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the stream input and parses it into an Open API document.
    /// </summary>
    /// <param name="stream">Stream containing OpenAPI description to parse.</param>
    /// <param name="format">The OpenAPI format to use during parsing.</param>
    /// <param name="settings">The OpenApi reader settings.</param>
    /// <param name="cancellationToken">Propagates information about operation cancelling.</param>
    /// <returns></returns>
    public static async Task<ReadResult> LoadFromStreamAsync(Stream stream, string? format = null, ArazzoReaderSettings? settings = null, CancellationToken cancellationToken = default)
    {
        return await ArazzoModelFactory.LoadFromStreamAsync(stream, format, settings, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Parses a string into a <see cref="OpenApiDocument"/> object.
    /// </summary>
    /// <param name="input"> The string input.</param>
    /// <param name="format"></param>
    /// <param name="settings"></param>
    /// <param name="cancellationToken">Propagates notification that operations should be cancelled.</param>
    /// <returns></returns>
    public static Task<ReadResult> ParseAsync(string input,
                                   string? format = null,
                                   ArazzoReaderSettings? settings = null,
                                   CancellationToken cancellationToken = default)
    {
        return ArazzoModelFactory.ParseAsync(input, format, settings, cancellationToken);
    }

    internal T? ResolveReferenceTo<T>(BaseArazzoReference reference, IArazzoInput? parentInput) where T : IArazzoReferenceable
    {
        if (ResolveReference(reference, reference.IsExternal, parentInput) is T result)
        {
            return result;
        }

        return default;
    }

    internal IArazzoReferenceable? ResolveReference(BaseArazzoReference? reference, bool useExternal, IArazzoInput? parentInput)
    {
        if (reference is null)
        {
            return null;
        }

        string uriLocation;
        if (!string.IsNullOrEmpty(reference.ReferenceV1) && reference.ReferenceV1!.Contains('/'))
        {
            uriLocation = reference.ReferenceV1!;
        }
        else
        {
            var relativePath = !string.IsNullOrEmpty(reference.ReferenceV1)
                ? reference.ReferenceV1!
                : $"#/components/{reference.Type.GetDisplayName()}/{reference.Id}";

            var externalResourceUri = useExternal ? Workspace?.GetDocumentId(reference.ExternalResource) : null;
            uriLocation = useExternal && externalResourceUri is not null
                ? externalResourceUri.AbsoluteUri + (relativePath.StartsWith("#", StringComparison.OrdinalIgnoreCase) ? relativePath : $"#{relativePath}")
                : BaseUri + relativePath;
        }

        var absoluteUri = uriLocation.StartsWith("#", StringComparison.OrdinalIgnoreCase)
            ? new Uri(BaseUri, uriLocation).AbsoluteUri
            : new Uri(uriLocation).AbsoluteUri;

        if (reference.Type is ReferenceType.Input && absoluteUri.Contains('#') && parentInput is not null)
        {
            return Workspace?.ResolveJsonSchemaReference(absoluteUri, parentInput);
        }

        return Workspace?.ResolveReference<IArazzoReferenceable>(absoluteUri);
    }
}