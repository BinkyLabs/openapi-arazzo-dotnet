// Licensed under the MIT license.

using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader;

/// <summary>
/// Base class for Arazzo version services providing common functionality.
/// </summary>
internal abstract class BaseArazzoVersionService : IArazzoVersionService
{
    /// <summary>
    /// Dictionary of type loaders for different Arazzo elements.
    /// </summary>
    protected abstract Dictionary<Type, Func<JsonNode, ParsingContext, object?>> Loaders { get; }

    /// <summary>
    /// Loads an OpenAPI Element from a document fragment.
    /// </summary>
    /// <typeparam name="T">Type of element to load.</typeparam>
    /// <param name="node">Document fragment node.</param>
    /// <param name="context">The current parsing context.</param>
    /// <returns>Instance of OpenAPIElement.</returns>
    public T? LoadElement<T>(JsonNode node, ParsingContext context) where T : IOpenApiElement
    {
        if (Loaders.TryGetValue(typeof(T), out var loader) && loader(node, context) is T result)
        {
            return result;
        }

        return default;
    }

    /// <summary>
    /// Converts a generic JsonNode instance into a strongly typed ArazzoDocument.
    /// </summary>
    /// <param name="jsonNode">JsonNode containing the information to be converted into an OpenAPI Document.</param>
    /// <param name="location">Location of where the document that is getting loaded is saved.</param>
    /// <param name="context">The current parsing context.</param>
    /// <returns>Instance of ArazzoDocument populated with data from jsonNode.</returns>
    public abstract ArazzoDocument LoadDocument(JsonNode jsonNode, Uri location, ParsingContext context);
}