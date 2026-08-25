// Licensed under the MIT license.

using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader.V1;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1_1;

internal static partial class ArazzoV1_1Deserializer
{
    public static readonly FixedFieldMap<ArazzoDocument> DocumentFixedFields = new(ArazzoV1Deserializer.DocumentFixedFields);
    public static readonly PatternFieldMap<ArazzoDocument> DocumentPatternFields = ArazzoV1Deserializer.GetDocumentPatternFields();

    public static ArazzoDocument LoadArazzoDocument(JsonNode node, Uri location, ParsingContext context)
    {
        context.Diagnostic.SpecificationVersion = ArazzoSpecVersion.Arazzo1_1;
        return ArazzoV1Deserializer.LoadArazzoDocumentInternal(node, location, context, DocumentFixedFields, DocumentPatternFields);
    }

    public static ArazzoDocument LoadDocument(JsonNode node, ParsingContext context)
    {
        return LoadArazzoDocument(node, context.BaseUrl ?? new Uri(OpenApiConstants.BaseRegistryUri + Guid.NewGuid()), context);
    }

    public static ArazzoInfo LoadInfo(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadInfo);

    public static ArazzoSourceDescription LoadSourceDescription(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadSourceDescription);

    public static ArazzoComponent LoadComponent(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadComponent);

    public static ArazzoWorkflow LoadWorkflow(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadWorkflow);

    public static ArazzoStep LoadStep(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadStep);

    public static ArazzoRequestBody LoadRequestBody(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadRequestBody);

    public static ArazzoPayloadReplacement LoadPayloadReplacement(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadPayloadReplacement);

    public static ArazzoCriterion LoadCriterion(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadCriterion);

    public static ArazzoCriterionExpressionType LoadCriterionExpressionType(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadCriterionExpressionType);

    public static IArazzoParameter LoadParameter(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadParameter);

    public static ArazzoParameter LoadParameterObject(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadParameterObject);

    public static IArazzoSuccessAction LoadSuccessAction(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadSuccessAction);

    public static ArazzoSuccessAction LoadSuccessActionObject(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadSuccessActionObject);

    public static IArazzoFailureAction LoadFailureAction(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadFailureAction);

    public static ArazzoFailureAction LoadFailureActionObject(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadFailureActionObject);

    private static T LoadWithArazzo1_1<T>(JsonNode node, ParsingContext context, Func<JsonNode, ParsingContext, T> load)
    {
        context.Diagnostic.SpecificationVersion = ArazzoSpecVersion.Arazzo1_1;
        return load(node, context);
    }
}