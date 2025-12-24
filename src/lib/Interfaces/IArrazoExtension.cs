
// Licensed under the MIT license.

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Interface required for implementing any custom extension
/// </summary>
public interface IArazzoExtension
{
    /// <summary>
    /// Write out contents of custom extension
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="specVersion">Version of the Arazzo specification that that will be output.</param>
    void Write(IOpenApiWriter writer, ArazzoSpecVersion specVersion);
}