
// Licensed under the MIT license.

using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

/// <summary>
/// The version service for the Arazzo 1.0 specification.
/// </summary>
internal class ArazzoV1VersionService : BaseArazzoVersionService
{

    /// <summary>
    /// Create Parsing Context
    /// </summary>
    /// <param name="diagnostic">Provide instance for diagnostic object for collecting and accessing information about the parsing.</param>
    public ArazzoV1VersionService(ArazzoDiagnostic diagnostic)
    {
    }

    private static readonly Dictionary<Type, Func<JsonNode, ParsingContext, object?>> _loaders = new()
    {
        [typeof(JsonNodeExtension)] = static (node, _) => new JsonNodeExtension(node),
        [typeof(ArazzoCriterion)] = ArazzoV1Deserializer.LoadCriterion,
        [typeof(ArazzoCriterionExpressionType)] = ArazzoV1Deserializer.LoadCriterionExpressionType,
        [typeof(ArazzoDocument)] = ArazzoV1Deserializer.LoadDocument,
        [typeof(ArazzoInfo)] = ArazzoV1Deserializer.LoadInfo,
        [typeof(ArazzoParameter)] = ArazzoV1Deserializer.LoadParameterObject,
        [typeof(ArazzoPayloadReplacement)] = ArazzoV1Deserializer.LoadPayloadReplacement,
        [typeof(ArazzoRequestBody)] = ArazzoV1Deserializer.LoadRequestBody,
        [typeof(ArazzoComponent)] = ArazzoV1Deserializer.LoadComponent,
        [typeof(ArazzoSourceDescription)] = ArazzoV1Deserializer.LoadSourceDescription,
        [typeof(ArazzoStep)] = ArazzoV1Deserializer.LoadStep,
        [typeof(ArazzoSuccessAction)] = ArazzoV1Deserializer.LoadSuccessActionObject,
        [typeof(ArazzoFailureAction)] = ArazzoV1Deserializer.LoadFailureActionObject,
        [typeof(ArazzoWorkflow)] = ArazzoV1Deserializer.LoadWorkflow,
    };

    public override ArazzoDocument LoadDocument(JsonNode jsonNode, Uri location, ParsingContext context)
    {
        return ArazzoV1Deserializer.LoadArazzoDocument(jsonNode, location, context);
    }

    protected override Dictionary<Type, Func<JsonNode, ParsingContext, object?>> Loaders => _loaders;
}