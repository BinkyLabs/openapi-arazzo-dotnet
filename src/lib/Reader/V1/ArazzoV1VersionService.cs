
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

/// <summary>
/// The version service for the Arazzo 1.0 specification.
/// </summary>
internal class ArazzoV1VersionService : IArazzoVersionService
{

    /// <summary>
    /// Create Parsing Context
    /// </summary>
    /// <param name="diagnostic">Provide instance for diagnostic object for collecting and accessing information about the parsing.</param>
    public ArazzoV1VersionService(ArazzoDiagnostic diagnostic)
    {
    }

    private readonly Dictionary<Type, Func<ParseNode, object?>> _loaders = new Dictionary<Type, Func<ParseNode, object?>>
    {
        [typeof(JsonNodeExtension)] = ArazzoV1Deserializer.LoadAny,
        [typeof(ArazzoCriterion)] = ArazzoV1Deserializer.LoadCriterion,
        [typeof(ArazzoCriterionExpressionType)] = ArazzoV1Deserializer.LoadCriterionExpressionType,
        [typeof(ArazzoDocument)] = ArazzoV1Deserializer.LoadDocument,
        [typeof(ArazzoInfo)] = ArazzoV1Deserializer.LoadInfo,
        [typeof(ArazzoParameter)] = ArazzoV1Deserializer.LoadParameter,
        [typeof(ArazzoPayloadReplacement)] = ArazzoV1Deserializer.LoadPayloadReplacement,
        [typeof(ArazzoRequestBody)] = ArazzoV1Deserializer.LoadRequestBody,
        [typeof(ArazzoSourceDescription)] = ArazzoV1Deserializer.LoadSourceDescription,
    };

    public ArazzoDocument LoadDocument(RootNode rootNode, Uri location)
    {
        return ArazzoV1Deserializer.LoadArazzoDocument(rootNode, location);
    }

    public T? LoadElement<T>(ParseNode node) where T : IOpenApiElement
    {
        if (Loaders.TryGetValue(typeof(T), out var loader) && loader(node) is T result)
        {
            return result;
        }
        return default;
    }

    internal Dictionary<Type, Func<ParseNode, object?>> Loaders => _loaders;
}