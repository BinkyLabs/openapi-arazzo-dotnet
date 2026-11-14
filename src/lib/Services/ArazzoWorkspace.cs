using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Contains a set of Arazzo documents and document fragments that reference each other.
/// </summary>
internal class ArazzoWorkspace
{
    private readonly Dictionary<string, Uri> _documentsIdRegistry = new(StringComparer.Ordinal);
    private readonly Dictionary<Uri, IArazzoReferenceable> _componentRegistry = new(new UriWithFragmentEqualityComparer());
    private readonly Dictionary<Uri, IArazzoInput> _inputRegistry = new(new UriWithFragmentEqualityComparer());
    private readonly Dictionary<string, SourceDescriptionRegistration> _sourceDescriptionRegistry = new(StringComparer.Ordinal);
    private readonly Dictionary<Uri, HashSet<string>> _openApiOperationPointers = new(new UriWithFragmentEqualityComparer());
    private readonly Dictionary<Uri, Dictionary<string, int>> _openApiOperationIdCounts = new(new UriWithFragmentEqualityComparer());
    private readonly Dictionary<Uri, Dictionary<string, OpenApiOperationMetadata>> _openApiOperations = new(new UriWithFragmentEqualityComparer());
    private readonly Dictionary<Uri, ArazzoDocument> _arazzoDocuments = new(new UriWithFragmentEqualityComparer());
    private readonly Dictionary<BaseArazzoReference, Uri> _inputReferenceBaseUris = new();

    internal readonly record struct SourceDescriptionRegistration(Uri Uri, ArazzoDescriptionType? Type);
    private sealed record OpenApiOperationMetadata(string? OperationId, IReadOnlyList<IOpenApiParameter> Parameters);

    private sealed class UriWithFragmentEqualityComparer : IEqualityComparer<Uri>
    {
        public bool Equals(Uri? x, Uri? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            if (x is null || y is null)
            {
                return false;
            }

            return StringComparer.Ordinal.Equals(x.AbsoluteUri, y.AbsoluteUri);
        }

        public int GetHashCode(Uri obj)
        {
            return StringComparer.Ordinal.GetHashCode(obj.AbsoluteUri);
        }
    }

    /// <summary>
    /// The base location from where all relative references are resolved.
    /// </summary>
    public Uri? BaseUrl { get; }

    /// <summary>
    /// Initializes a workspace with a base URL.
    /// </summary>
    /// <param name="baseUrl">The base URL.</param>
    public ArazzoWorkspace(Uri baseUrl)
    {
        BaseUrl = baseUrl;
    }

    /// <summary>
    /// Initializes a workspace using the default registry location.
    /// </summary>
    public ArazzoWorkspace()
    {
        BaseUrl = new Uri(OpenApiConstants.BaseRegistryUri);
    }

    /// <summary>
    /// Initializes a copy of an <see cref="ArazzoWorkspace"/> object.
    /// </summary>
    /// <param name="workspace">The workspace to copy.</param>
    public ArazzoWorkspace(ArazzoWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        BaseUrl = workspace.BaseUrl;

        foreach (var pair in workspace._documentsIdRegistry)
        {
            _documentsIdRegistry[pair.Key] = pair.Value;
        }

        foreach (var pair in workspace._componentRegistry)
        {
            _componentRegistry[pair.Key] = pair.Value;
        }

        foreach (var pair in workspace._inputRegistry)
        {
            _inputRegistry[pair.Key] = pair.Value;
        }

        foreach (var pair in workspace._sourceDescriptionRegistry)
        {
            _sourceDescriptionRegistry[pair.Key] = pair.Value;
        }

        foreach (var pair in workspace._openApiOperationPointers)
        {
            _openApiOperationPointers[pair.Key] = new HashSet<string>(pair.Value, StringComparer.Ordinal);
        }

        foreach (var pair in workspace._openApiOperationIdCounts)
        {
            _openApiOperationIdCounts[pair.Key] = new Dictionary<string, int>(pair.Value, StringComparer.Ordinal);
        }
        foreach (var pair in workspace._openApiOperations)
            _openApiOperations[pair.Key] = new Dictionary<string, OpenApiOperationMetadata>(pair.Value, StringComparer.Ordinal);

        foreach (var pair in workspace._arazzoDocuments)
        {
            _arazzoDocuments[pair.Key] = pair.Value;
        }
        foreach (var pair in workspace._inputReferenceBaseUris)
        {
            _inputReferenceBaseUris[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// Returns the total count of all registered components.
    /// </summary>
    public int ComponentsCount()
    {
        return _componentRegistry.Count + _inputRegistry.Count;
    }

    /// <summary>
    /// Registers a document's input components into the workspace.
    /// </summary>
    /// <param name="document">The document whose components should be registered.</param>
    public void RegisterComponents(ArazzoDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        RegisterArazzoDocument(document.BaseUri, document);
        RegisterSourceDescriptions(document);

        if (document.Workflows is not null)
        {
            for (var index = 0; index < document.Workflows.Count; index++)
            {
                RegisterNestedWorkflowInput(document.Workflows[index], $"{document.BaseUri}#/workflows/{index}/inputs");
            }
        }

        if (document.Components is null)
        {
            return;
        }

        if (document.Components.Parameters is not null)
        {
            var parametersBaseUri = $"{document.BaseUri}#/components/parameters/";
            foreach (var item in document.Components.Parameters)
            {
                RegisterComponent(parametersBaseUri + item.Key, item.Value);
            }
        }

        if (document.Components.SuccessActions is not null)
        {
            var successActionsBaseUri = $"{document.BaseUri}#/components/successActions/";
            foreach (var item in document.Components.SuccessActions)
            {
                RegisterComponent(successActionsBaseUri + item.Key, item.Value);
            }
        }

        if (document.Components.FailureActions is not null)
        {
            var failureActionsBaseUri = $"{document.BaseUri}#/components/failureActions/";
            foreach (var item in document.Components.FailureActions)
            {
                RegisterComponent(failureActionsBaseUri + item.Key, item.Value);
            }
        }

        if (document.Components.Inputs is null)
        {
            return;
        }

        var inputsBaseUri = $"{document.BaseUri}#/components/inputs/";
        foreach (var item in document.Components.Inputs)
        {
            RegisterInputTree(item.Value, inputsBaseUri + item.Key);
        }
    }

    internal void RegisterOpenApiDocument(Uri documentUri, OpenApiDocument document)
    {
        ArgumentNullException.ThrowIfNull(documentUri);
        ArgumentNullException.ThrowIfNull(document);

        var documentUriWithoutFragment = WithoutFragment(documentUri);
        AddDocumentId(documentUriWithoutFragment.AbsoluteUri, documentUriWithoutFragment);
        var operationPointers = _openApiOperationPointers.GetValueOrDefault(documentUriWithoutFragment);
        if (operationPointers is null)
        {
            operationPointers = new HashSet<string>(StringComparer.Ordinal);
            _openApiOperationPointers[documentUriWithoutFragment] = operationPointers;
        }

        var operationIdCounts = _openApiOperationIdCounts.GetValueOrDefault(documentUriWithoutFragment);
        if (operationIdCounts is null)
        {
            operationIdCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            _openApiOperationIdCounts[documentUriWithoutFragment] = operationIdCounts;
        }

        foreach (var path in document.Paths ?? [])
        {
            foreach (var operation in path.Value.Operations ?? [])
            {
                var pointer = ArazzoStep.BuildOperationPointer(path.Key, operation.Key.Method);
                operationPointers.Add(pointer);
                if (!_openApiOperations.TryGetValue(documentUriWithoutFragment, out var metadata))
                {
                    metadata = new Dictionary<string, OpenApiOperationMetadata>(StringComparer.Ordinal);
                    _openApiOperations.Add(documentUriWithoutFragment, metadata);
                }
                var parameters = (path.Value.Parameters ?? []).Concat(operation.Value.Parameters ?? [])
                    .GroupBy(static parameter => (parameter.Name, parameter.In))
                    .Select(static group => group.Last()).ToArray();
                metadata[pointer] = new OpenApiOperationMetadata(operation.Value.OperationId, parameters);
                if (!string.IsNullOrEmpty(operation.Value.OperationId))
                {
                    operationIdCounts[operation.Value.OperationId] = operationIdCounts.GetValueOrDefault(operation.Value.OperationId) + 1;
                }
            }
        }
    }

    internal bool IsOpenApiDocumentLoaded(Uri documentUri)
    {
        return _openApiOperationPointers.ContainsKey(WithoutFragment(documentUri));
    }

    internal IReadOnlyList<IOpenApiParameter>? GetOpenApiOperationParameters(Uri documentUri, string identifier, bool isPointer)
    {
        if (!_openApiOperations.TryGetValue(WithoutFragment(documentUri), out var operations))
            return null;
        if (isPointer)
            return operations.TryGetValue(NormalizeFragment(identifier), out var operation) ? operation.Parameters : null;
        var matches = operations.Values.Where(operation => operation.OperationId == identifier).Take(2).ToArray();
        return matches.Length == 1 ? matches[0].Parameters : null;
    }

    internal bool ContainsOpenApiOperationPointer(Uri documentUri, string pointer)
    {
        return _openApiOperationPointers.TryGetValue(WithoutFragment(documentUri), out var operationPointers) &&
            operationPointers.Contains(NormalizeFragment(pointer));
    }

    internal int CountOpenApiOperationId(Uri documentUri, string operationId)
    {
        return _openApiOperationIdCounts.TryGetValue(WithoutFragment(documentUri), out var operationIdCounts) &&
            operationIdCounts.TryGetValue(operationId, out var count)
                ? count
                : 0;
    }

    internal void RegisterSourceDescriptions(ArazzoDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        foreach (var sourceDescription in document.SourceDescriptions ?? [])
        {
            if (string.IsNullOrEmpty(sourceDescription.Name) || sourceDescription.Url is null)
            {
                continue;
            }

            var sourceDescriptionUri = new Uri(document.BaseUri, sourceDescription.Url);
            _sourceDescriptionRegistry[sourceDescription.Name] = new SourceDescriptionRegistration(sourceDescriptionUri, sourceDescription.Type);
            AddDocumentId(sourceDescription.Url.ToString(), sourceDescriptionUri);
        }
    }

    internal bool TryGetSourceDescription(string name, out SourceDescriptionRegistration registration)
    {
        return _sourceDescriptionRegistry.TryGetValue(name, out registration);
    }

    internal bool TryGetSourceDescription(ArazzoDocument document, string name, out SourceDescriptionRegistration registration)
    {
        var source = document.SourceDescriptions?.FirstOrDefault(source => source.Name == name && source.Url is not null);
        if (source?.Url is null)
        {
            registration = default;
            return false;
        }

        registration = new SourceDescriptionRegistration(new Uri(document.BaseUri, source.Url), source.Type);
        return true;
    }

    internal IEnumerable<SourceDescriptionRegistration> GetSourceDescriptions(ArazzoDocument document)
    {
        return (document.SourceDescriptions ?? [])
            .Where(static source => !string.IsNullOrEmpty(source.Name) && source.Url is not null)
            .Select(source => new SourceDescriptionRegistration(new Uri(document.BaseUri, source.Url!), source.Type));
    }

    internal void RegisterArazzoDocument(Uri documentUri, ArazzoDocument document)
    {
        ArgumentNullException.ThrowIfNull(documentUri);
        ArgumentNullException.ThrowIfNull(document);

        var retrievalUri = WithoutFragment(documentUri);
        var canonicalUri = WithoutFragment(document.BaseUri);
        _arazzoDocuments[retrievalUri] = document;
        _arazzoDocuments[canonicalUri] = document;

        if (!StringComparer.Ordinal.Equals(retrievalUri.AbsoluteUri, canonicalUri.AbsoluteUri))
        {
            foreach (var key in _documentsIdRegistry
                .Where(pair => StringComparer.Ordinal.Equals(pair.Value.AbsoluteUri, retrievalUri.AbsoluteUri))
                .Select(static pair => pair.Key).ToArray())
            {
                _documentsIdRegistry[key] = canonicalUri;
            }
        }
        _documentsIdRegistry[retrievalUri.AbsoluteUri] = canonicalUri;
    }

    internal bool TryGetArazzoDocument(Uri documentUri, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ArazzoDocument? document)
    {
        return _arazzoDocuments.TryGetValue(WithoutFragment(documentUri), out document);
    }

    internal IEnumerable<ArazzoDocument> GetArazzoDocuments()
    {
        return _arazzoDocuments.Values.Distinct();
    }

    internal Uri? GetInputReferenceBaseUri(BaseArazzoReference reference)
    {
        return _inputReferenceBaseUris.GetValueOrDefault(reference);
    }

    /// <summary>
    /// Registers a component for a document.
    /// </summary>
    /// <param name="document">The document that owns the component.</param>
    /// <param name="componentToRegister">The component to register.</param>
    /// <param name="id">The identifier or URI.</param>
    public bool RegisterComponentForDocument(ArazzoDocument document, IArazzoReferenceable componentToRegister, string id)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(componentToRegister);
        ArgumentException.ThrowIfNullOrEmpty(id);

        string location;
        if (Uri.TryCreate(id, UriKind.Absolute, out var absoluteIdentifier))
        {
            location = absoluteIdentifier.AbsoluteUri;
        }
        else
        {
            location = new Uri(document.BaseUri, id).AbsoluteUri;
        }

        return RegisterComponent(location, componentToRegister);
    }

    /// <summary>
    /// Registers a component in the component registry.
    /// </summary>
    /// <param name="location">The component location.</param>
    /// <param name="component">The component instance.</param>
    public bool RegisterComponent(string location, IArazzoReferenceable component)
    {
        ArgumentException.ThrowIfNullOrEmpty(location);
        ArgumentNullException.ThrowIfNull(component);

        var uri = ToLocationUrl(location);
        if (uri is null)
        {
            return false;
        }

        if (_componentRegistry.ContainsKey(uri))
        {
            return false;
        }

        _componentRegistry[uri] = component;
        if (component is IArazzoInput input)
        {
            _inputRegistry[uri] = input;
        }
        return true;
    }

    internal void RegisterInputSchema(string location, IArazzoInput input)
    {
        ArgumentException.ThrowIfNullOrEmpty(location);
        ArgumentNullException.ThrowIfNull(input);

        RegisterInputTree(input, location);
        var uri = ToLocationUrl(location);
        if (uri is not null && string.IsNullOrEmpty(uri.Fragment) &&
            input is not ArazzoInputReference && !string.IsNullOrEmpty(input.Id))
        {
            _documentsIdRegistry[uri.AbsoluteUri] = new Uri(uri, input.Id);
        }
    }

    /// <summary>
    /// Adds a document identifier to the registry.
    /// </summary>
    public void AddDocumentId(string? key, Uri? value)
    {
        if (!string.IsNullOrEmpty(key) && value is not null && !_documentsIdRegistry.ContainsKey(key))
        {
            _documentsIdRegistry[key] = value;
        }
    }

    /// <summary>
    /// Retrieves the registered document base URI for a key.
    /// </summary>
    public Uri? GetDocumentId(string? key)
    {
        if (key is not null && _documentsIdRegistry.TryGetValue(key, out var id))
        {
            return id;
        }

        return null;
    }

    /// <summary>
    /// Verifies whether the workspace contains a component or document for the location.
    /// </summary>
    public bool Contains(string location)
    {
        if (_documentsIdRegistry.ContainsKey(location))
        {
            return true;
        }

        var key = ToLocationUrl(location);
        return key is not null && _componentRegistry.ContainsKey(key);
    }

    /// <summary>
    /// Resolves a registered input reference.
    /// </summary>
    public T? ResolveReference<T>(string location)
    {
        if (string.IsNullOrEmpty(location))
        {
            return default;
        }

        var uri = ToLocationUrl(location);
        if (uri is not null && _componentRegistry.TryGetValue(uri, out var referenceableValue) && referenceableValue is T referenceable)
        {
            return referenceable;
        }

        return default;
    }

    /// <summary>
    /// Recursively resolves an input from a URI fragment.
    /// </summary>
    internal IArazzoInput? ResolveJsonSchemaReference(string location)
    {
        if (string.IsNullOrEmpty(location) || ToLocationUrl(location) is not Uri uri)
        {
            return default;
        }

        if (_inputRegistry.TryGetValue(uri, out var registeredInput))
        {
            return registeredInput;
        }

        var decodedFragment = Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
        if (decodedFragment.Length > 0 && !decodedFragment.StartsWith("/", StringComparison.Ordinal))
        {
            var anchorUri = new UriBuilder(uri) { Fragment = decodedFragment }.Uri;
            return _inputRegistry.GetValueOrDefault(anchorUri);
        }

        var pathSegments = uri.Fragment.TrimStart('#').Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length == 0)
        {
            return uri.Fragment is "" or "#" ? _inputRegistry.GetValueOrDefault(WithoutFragment(uri)) : null;
        }

        if (_inputRegistry.TryGetValue(WithoutFragment(uri), out var resource))
        {
            return ResolveSubSchema(resource, pathSegments, []);
        }

        if (pathSegments.Length >= 3 &&
            pathSegments[0].Equals("components", StringComparison.OrdinalIgnoreCase) &&
            (pathSegments[1].Equals(ArazzoConstants.ArazzoComponentInputs, StringComparison.OrdinalIgnoreCase) ||
             // External OpenAPI descriptions can contribute reusable schemas under components.schemas.
             pathSegments[1].Equals("schemas", StringComparison.OrdinalIgnoreCase)))
        {
            var fragment = $"components/{pathSegments[1]}/{pathSegments[2]}";
            var uriBuilder = new UriBuilder(uri) { Fragment = fragment };

            if (_inputRegistry.TryGetValue(uriBuilder.Uri, out var targetInput))
            {
                return ResolveSubSchema(targetInput, [.. pathSegments.Skip(3)], []);
            }

            return default;
        }

        return null;
    }

    internal static IArazzoInput? ResolveSubSchema(IArazzoInput schema, string[] pathSegments, Stack<IArazzoInput> visitedSchemas)
    {
        if (visitedSchemas.Contains(schema))
        {
            if (schema is ArazzoInputReference reference)
            {
                throw new InvalidOperationException($"Circular reference detected while resolving schema: {reference.Reference.ReferenceV1}");
            }

            throw new InvalidOperationException("Circular reference detected while resolving schema.");
        }

        visitedSchemas.Push(schema);
        if (pathSegments.Length == 0)
        {
            return schema;
        }

        var currentSegment = pathSegments[0];
        var remainingSegments = pathSegments[1..];

        switch (currentSegment)
        {
            case "$defs":
                if (remainingSegments.Length > 0 &&
                    schema.Definitions is not null &&
                    schema.Definitions.TryGetValue(remainingSegments[0], out var definitionSchema))
                {
                    return ResolveSubSchema(definitionSchema, remainingSegments[1..], visitedSchemas);
                }
                break;
            case OpenApiConstants.Properties:
                if (remainingSegments.Length > 0 &&
                    schema.Properties is not null &&
                    schema.Properties.TryGetValue(remainingSegments[0], out var propertySchema))
                {
                    return ResolveSubSchema(propertySchema, remainingSegments[1..], visitedSchemas);
                }
                break;
            case OpenApiConstants.Items:
                if (schema.Items is not null)
                {
                    return ResolveSubSchema(schema.Items, remainingSegments, visitedSchemas);
                }
                break;
            case OpenApiConstants.AdditionalProperties:
                if (schema.AdditionalProperties is not null)
                {
                    return ResolveSubSchema(schema.AdditionalProperties, remainingSegments, visitedSchemas);
                }
                break;
            case OpenApiConstants.UnevaluatedProperties:
                if (schema.UnevaluatedPropertiesSchema is not null)
                {
                    return ResolveSubSchema(schema.UnevaluatedPropertiesSchema, remainingSegments, visitedSchemas);
                }
                break;
            case OpenApiConstants.AllOf:
            case OpenApiConstants.AnyOf:
            case OpenApiConstants.OneOf:
                if (remainingSegments.Length == 0 || !int.TryParse(remainingSegments[0], out var index))
                {
                    return null;
                }

                var list = currentSegment switch
                {
                    OpenApiConstants.AllOf => schema.AllOf,
                    OpenApiConstants.AnyOf => schema.AnyOf,
                    OpenApiConstants.OneOf => schema.OneOf,
                    _ => null
                };

                if (list is not null && index < list.Count)
                {
                    return ResolveSubSchema(list[index], remainingSegments[1..], visitedSchemas);
                }
                break;
            case OpenApiConstants.Not:
                if (schema.Not is not null)
                {
                    return ResolveSubSchema(schema.Not, remainingSegments, visitedSchemas);
                }
                break;
        }

        return null;
    }

    private void RegisterInputTree(IArazzoInput input, string location, Uri? baseUri = null)
    {
        RegisterComponent(location, input);
        baseUri ??= WithoutFragment(ToLocationUrl(location)!);

        if (input is ArazzoInputReference reference)
        {
            _inputReferenceBaseUris[reference.Reference] = baseUri;
            return;
        }

        if (!string.IsNullOrEmpty(input.Id))
        {
            baseUri = new Uri(baseUri, input.Id);
            RegisterComponent(baseUri.AbsoluteUri, input);
        }
        if (!string.IsNullOrEmpty(input.Anchor))
        {
            RegisterComponent(new UriBuilder(baseUri) { Fragment = input.Anchor }.Uri.AbsoluteUri, input);
        }

        RegisterNestedInputs(input, location.Contains('#', StringComparison.Ordinal) ? location : $"{location}#", baseUri);
    }

    private void RegisterNestedInputs(IArazzoInput input, string location, Uri baseUri)
    {
        if (input.Definitions is not null)
        {
            foreach (var pair in input.Definitions)
            {
                RegisterInputTree(pair.Value, $"{location}/$defs/{pair.Key}", baseUri);
            }
        }

        if (input.Properties is not null)
        {
            foreach (var pair in input.Properties)
            {
                RegisterInputTree(pair.Value, $"{location}/{OpenApiConstants.Properties}/{pair.Key}", baseUri);
            }
        }

        if (input.PatternProperties is not null)
        {
            foreach (var pair in input.PatternProperties)
            {
                RegisterInputTree(pair.Value, $"{location}/{OpenApiConstants.PatternProperties}/{pair.Key}", baseUri);
            }
        }

        foreach (var pair in input.DependentSchemas ?? Enumerable.Empty<KeyValuePair<string, IArazzoInput>>())
        {
            RegisterInputTree(pair.Value, $"{location}/dependentSchemas/{pair.Key}", baseUri);
        }

        RegisterNestedInput(input.Items, $"{location}/{OpenApiConstants.Items}", baseUri);
        RegisterNestedInput(input.AdditionalProperties, $"{location}/{OpenApiConstants.AdditionalProperties}", baseUri);
        RegisterNestedInput(input.UnevaluatedPropertiesSchema, $"{location}/{OpenApiConstants.UnevaluatedProperties}", baseUri);
        RegisterNestedInput(input.Not, $"{location}/{OpenApiConstants.Not}", baseUri);
        RegisterNestedInput(input.Contains, $"{location}/contains", baseUri);
        RegisterNestedInput(input.ContentSchema, $"{location}/contentSchema", baseUri);
        RegisterNestedInput(input.PropertyNames, $"{location}/propertyNames", baseUri);
        RegisterNestedInput(input.If, $"{location}/if", baseUri);
        RegisterNestedInput(input.Then, $"{location}/then", baseUri);
        RegisterNestedInput(input.Else, $"{location}/else", baseUri);
        RegisterNestedInputList(input.AllOf, $"{location}/{OpenApiConstants.AllOf}", baseUri);
        RegisterNestedInputList(input.AnyOf, $"{location}/{OpenApiConstants.AnyOf}", baseUri);
        RegisterNestedInputList(input.OneOf, $"{location}/{OpenApiConstants.OneOf}", baseUri);
    }

    private void RegisterNestedWorkflowInput(ArazzoWorkflow workflow, string location)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        if (workflow.Inputs is not null)
        {
            RegisterInputTree(workflow.Inputs, location);
        }
    }

    private void RegisterNestedInput(IArazzoInput? input, string location, Uri baseUri)
    {
        if (input is not null)
        {
            RegisterInputTree(input, location, baseUri);
        }
    }

    private void RegisterNestedInputList(IList<IArazzoInput>? inputs, string location, Uri baseUri)
    {
        if (inputs is null)
        {
            return;
        }

        for (var index = 0; index < inputs.Count; index++)
        {
            RegisterInputTree(inputs[index], $"{location}/{index}", baseUri);
        }
    }

    private Uri? ToLocationUrl(string location)
    {
        if (Uri.TryCreate(location, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        if (BaseUrl is not null)
        {
            return new Uri(BaseUrl, location);
        }

        return null;
    }

    private static Uri WithoutFragment(Uri uri)
    {
        return string.IsNullOrEmpty(uri.Fragment)
            ? uri
            : new UriBuilder(uri) { Fragment = string.Empty }.Uri;
    }

    private static string NormalizeFragment(string fragment)
    {
        return fragment.StartsWith("#", StringComparison.Ordinal) ? fragment : $"#{fragment}";
    }

}