
// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Interface to a version specific parsing implementations.
/// </summary>
internal interface IArazzoVersionService
{
    /// <summary>
    /// Loads an OpenAPI Element from a document fragment
    /// </summary>
    /// <typeparam name="T">Type of element to load</typeparam>
    /// <param name="node">document fragment node</param>
    /// <param name="context">The current parsing context.</param>
    /// <returns>Instance of OpenAPIElement</returns>
    T? LoadElement<T>(JsonNode node, ParsingContext context) where T : IOpenApiElement;

    /// <summary>
    /// Converts a generic JsonNode instance into a strongly typed ArazzoDocument
    /// </summary>
    /// <param name="jsonNode">JsonNode containing the information to be converted into an OpenAPI Document</param>
    /// <param name="location">Location of where the document that is getting loaded is saved</param>
    /// <param name="context">The current parsing context.</param>
    /// <returns>Instance of ArazzoDocument populated with data from rootNode</returns>
    ArazzoDocument LoadDocument(JsonNode jsonNode, Uri location, ParsingContext context);
}