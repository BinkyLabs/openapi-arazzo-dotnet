// Licensed under the MIT license.

using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1_1;

/// <summary>
/// The version service for the Arazzo 1.1 specification.
/// </summary>
internal class ArazzoV1_1VersionService : BaseArazzoVersionService
{
    /// <summary>
    /// Create Parsing Context
    /// </summary>
    /// <param name="diagnostic">Provide instance for diagnostic object for collecting and accessing information about the parsing.</param>
    public ArazzoV1_1VersionService(ArazzoDiagnostic diagnostic)
    {
    }

    private static readonly Dictionary<Type, Func<JsonNode, ParsingContext, object?>> _loaders = new()
    {
        [typeof(JsonNodeExtension)] = static (node, _) => new JsonNodeExtension(node),
        [typeof(ArazzoCriterion)] = ArazzoV1_1Deserializer.LoadCriterion,
        [typeof(ArazzoCriterionExpressionType)] = ArazzoV1_1Deserializer.LoadCriterionExpressionType,
        [typeof(ArazzoSelector)] = ArazzoV1_1Deserializer.LoadSelector,
        [typeof(ArazzoDocument)] = ArazzoV1_1Deserializer.LoadDocument,
        [typeof(ArazzoInfo)] = ArazzoV1_1Deserializer.LoadInfo,
        [typeof(ArazzoParameter)] = ArazzoV1_1Deserializer.LoadParameterObject,
        [typeof(ArazzoPayloadReplacement)] = ArazzoV1_1Deserializer.LoadPayloadReplacement,
        [typeof(ArazzoRequestBody)] = ArazzoV1_1Deserializer.LoadRequestBody,
        [typeof(ArazzoComponent)] = ArazzoV1_1Deserializer.LoadComponent,
        [typeof(ArazzoSourceDescription)] = ArazzoV1_1Deserializer.LoadSourceDescription,
        [typeof(ArazzoStep)] = ArazzoV1_1Deserializer.LoadStep,
        [typeof(ArazzoSuccessAction)] = ArazzoV1_1Deserializer.LoadSuccessActionObject,
        [typeof(ArazzoFailureAction)] = ArazzoV1_1Deserializer.LoadFailureActionObject,
        [typeof(ArazzoWorkflow)] = ArazzoV1_1Deserializer.LoadWorkflow,
    };

    public override ArazzoDocument LoadDocument(JsonNode jsonNode, Uri location, ParsingContext context)
    {
        return ArazzoV1_1Deserializer.LoadArazzoDocument(jsonNode, location, context);
    }

    protected override Dictionary<Type, Func<JsonNode, ParsingContext, object?>> Loaders => _loaders;
}