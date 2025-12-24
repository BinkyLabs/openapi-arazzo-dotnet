
// Licensed under the MIT license.

using System.Collections.Generic;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents an Extensible Open API element.
/// </summary>
public interface IArazzoExtensible : IOpenApiElement
{
    /// <summary>
    /// Specification extensions.
    /// </summary>
    IDictionary<string, IArazzoExtension>? Extensions { get; set; }
}