
// Licensed under the MIT license.

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
    /// <returns>Instance of OpenAPIElement</returns>
    T? LoadElement<T>(ParseNode node) where T : IOpenApiElement;

    /// <summary>
    /// Converts a generic RootNode instance into a strongly typed ArazzoDocument
    /// </summary>
    /// <param name="rootNode">RootNode containing the information to be converted into an OpenAPI Document</param>
    /// <param name="location">Location of where the document that is getting loaded is saved</param>
    /// <returns>Instance of ArazzoDocument populated with data from rootNode</returns>
    ArazzoDocument LoadDocument(RootNode rootNode, Uri location);
}