using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Contains a set of Arazzo documents and document fragments that reference each other.
/// </summary>
internal class ArazzoWorkspace
{
    private readonly Dictionary<string, Uri> _documentsIdRegistry = new(StringComparer.Ordinal);
    private readonly Dictionary<Uri, IArazzoInput> _inputRegistry = new(new UriWithFragmentEqualityComparer());

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

        foreach (var pair in workspace._inputRegistry)
        {
            _inputRegistry[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    /// Returns the total count of all registered components.
    /// </summary>
    public int ComponentsCount()
    {
        return _inputRegistry.Count;
    }

    /// <summary>
    /// Registers a document's input components into the workspace.
    /// </summary>
    /// <param name="document">The document whose components should be registered.</param>
    public void RegisterComponents(ArazzoDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Components?.Inputs is null)
        {
            return;
        }

        var baseUri = $"{document.BaseUri}#/components/inputs/";
        foreach (var item in document.Components.Inputs)
        {
            RegisterInputTree(item.Value, baseUri + item.Key);
        }
    }

    /// <summary>
    /// Registers a component for a document.
    /// </summary>
    /// <param name="document">The document that owns the component.</param>
    /// <param name="componentToRegister">The component to register.</param>
    /// <param name="id">The identifier or URI.</param>
    public bool RegisterComponentForDocument(ArazzoDocument document, IArazzoInput componentToRegister, string id)
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
    public bool RegisterComponent(string location, IArazzoInput component)
    {
        ArgumentException.ThrowIfNullOrEmpty(location);
        ArgumentNullException.ThrowIfNull(component);

        var uri = ToLocationUrl(location);
        if (uri is null)
        {
            return false;
        }

        if (_inputRegistry.ContainsKey(uri))
        {
            return false;
        }

        _inputRegistry[uri] = component;
        return true;
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
        return key is not null && _inputRegistry.ContainsKey(key);
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
        if (uri is not null && _inputRegistry.TryGetValue(uri, out var referenceableValue) && referenceableValue is T referenceable)
        {
            return referenceable;
        }

        return default;
    }

    /// <summary>
    /// Recursively resolves an input from a URI fragment.
    /// </summary>
    internal IArazzoInput? ResolveJsonSchemaReference(string location, IArazzoInput parentInput)
    {
        if (string.IsNullOrEmpty(location) || ToLocationUrl(location) is not Uri uri)
        {
            return default;
        }

        var pathSegments = uri.Fragment.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length == 0)
        {
            return default;
        }

        if (pathSegments.Length >= 3 &&
            pathSegments[0].Equals("components", StringComparison.OrdinalIgnoreCase) &&
            pathSegments[1].Equals(ArazzoConstants.ArazzoComponentInputs, StringComparison.OrdinalIgnoreCase))
        {
            var fragment = $"components/{ArazzoConstants.ArazzoComponentInputs}/{pathSegments[2]}";
            var uriBuilder = new UriBuilder(uri) { Fragment = fragment };

            if (_inputRegistry.TryGetValue(uriBuilder.Uri, out var targetInput))
            {
                return ResolveSubSchema(targetInput, [.. pathSegments.Skip(3)], []);
            }

            return default;
        }

        return ResolveSubSchema(parentInput, pathSegments, []);
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

    private void RegisterInputTree(IArazzoInput input, string location)
    {
        RegisterComponent(location, input);

        if (!string.IsNullOrEmpty(input.Id))
        {
            RegisterComponent(location: ResolveIdentifierLocation(location, input.Id!), component: input);
        }

        RegisterNestedInputs(input, location);
    }

    private void RegisterNestedInputs(IArazzoInput input, string location)
    {
        if (input.Definitions is not null)
        {
            foreach (var pair in input.Definitions)
            {
                RegisterInputTree(pair.Value, $"{location}/$defs/{pair.Key}");
            }
        }

        if (input.Properties is not null)
        {
            foreach (var pair in input.Properties)
            {
                RegisterInputTree(pair.Value, $"{location}/{OpenApiConstants.Properties}/{pair.Key}");
            }
        }

        if (input.PatternProperties is not null)
        {
            foreach (var pair in input.PatternProperties)
            {
                RegisterInputTree(pair.Value, $"{location}/{OpenApiConstants.PatternProperties}/{pair.Key}");
            }
        }

        RegisterNestedInput(input.Items, $"{location}/{OpenApiConstants.Items}");
        RegisterNestedInput(input.AdditionalProperties, $"{location}/{OpenApiConstants.AdditionalProperties}");
        RegisterNestedInput(input.UnevaluatedPropertiesSchema, $"{location}/{OpenApiConstants.UnevaluatedProperties}");
        RegisterNestedInput(input.Not, $"{location}/{OpenApiConstants.Not}");
        RegisterNestedInputList(input.AllOf, $"{location}/{OpenApiConstants.AllOf}");
        RegisterNestedInputList(input.AnyOf, $"{location}/{OpenApiConstants.AnyOf}");
        RegisterNestedInputList(input.OneOf, $"{location}/{OpenApiConstants.OneOf}");
    }

    private void RegisterNestedInput(IArazzoInput? input, string location)
    {
        if (input is not null)
        {
            RegisterInputTree(input, location);
        }
    }

    private void RegisterNestedInputList(IList<IArazzoInput>? inputs, string location)
    {
        if (inputs is null)
        {
            return;
        }

        for (var index = 0; index < inputs.Count; index++)
        {
            RegisterInputTree(inputs[index], $"{location}/{index}");
        }
    }

    private string ResolveIdentifierLocation(string currentLocation, string identifier)
    {
        if (Uri.TryCreate(identifier, UriKind.Absolute, out var absoluteIdentifier))
        {
            return absoluteIdentifier.AbsoluteUri;
        }

        if (BaseUrl is not null && Uri.TryCreate(BaseUrl, identifier, out var relativeIdentifier))
        {
            return relativeIdentifier.AbsoluteUri;
        }

        return new Uri(ToLocationUrl(currentLocation)!, identifier).AbsoluteUri;
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
}