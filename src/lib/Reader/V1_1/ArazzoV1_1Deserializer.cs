// Licensed under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Reader.V1;
using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1_1;

internal static partial class ArazzoV1_1Deserializer
{
    public static readonly FixedFieldMap<ArazzoDocument> DocumentFixedFields = CreateDocumentFixedFields();
    public static readonly PatternFieldMap<ArazzoDocument> DocumentPatternFields = ArazzoV1Deserializer.GetDocumentPatternFields();
    public static readonly FixedFieldMap<ArazzoStep> StepFixedFields = CreateStepFixedFields();
    public static readonly PatternFieldMap<ArazzoStep> StepPatternFields = ArazzoV1Deserializer.GetStepPatternFields();
    public static readonly FixedFieldMap<ArazzoPayloadReplacement> PayloadReplacementFixedFields = CreatePayloadReplacementFixedFields();
    public static readonly PatternFieldMap<ArazzoPayloadReplacement> PayloadReplacementPatternFields = ArazzoV1Deserializer.GetPayloadReplacementPatternFields();
    public static readonly FixedFieldMap<ArazzoRequestBody> RequestBodyFixedFields = CreateRequestBodyFixedFields();
    public static readonly PatternFieldMap<ArazzoRequestBody> RequestBodyPatternFields = ArazzoV1Deserializer.GetRequestBodyPatternFields();
    public static readonly FixedFieldMap<ArazzoSuccessAction> SuccessActionFixedFields = CreateSuccessActionFixedFields();
    public static readonly PatternFieldMap<ArazzoSuccessAction> SuccessActionPatternFields = ArazzoV1Deserializer.GetSuccessActionPatternFields();
    public static readonly FixedFieldMap<ArazzoFailureAction> FailureActionFixedFields = CreateFailureActionFixedFields();
    public static readonly PatternFieldMap<ArazzoFailureAction> FailureActionPatternFields = ArazzoV1Deserializer.GetFailureActionPatternFields();
    public static readonly FixedFieldMap<ArazzoWorkflow> WorkflowFixedFields = CreateWorkflowFixedFields();
    public static readonly PatternFieldMap<ArazzoWorkflow> WorkflowPatternFields = ArazzoV1Deserializer.GetWorkflowPatternFields();
    public static readonly FixedFieldMap<ArazzoComponent> ComponentFixedFields = CreateComponentFixedFields();
    public static readonly PatternFieldMap<ArazzoComponent> ComponentPatternFields = ArazzoV1Deserializer.GetComponentPatternFields();

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
        LoadWithArazzo1_1(node, context, static (n, c) => ArazzoV1Deserializer.LoadComponentInternal(n, c, ComponentFixedFields, ComponentPatternFields));

    public static ArazzoWorkflow LoadWorkflow(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, static (n, c) => ArazzoV1Deserializer.LoadWorkflowInternal(n, c, WorkflowFixedFields, WorkflowPatternFields));

    public static ArazzoStep LoadStep(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, static (n, c) => ArazzoV1Deserializer.LoadStepInternal(n, c, StepFixedFields, StepPatternFields));

    public static ArazzoRequestBody LoadRequestBody(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, static (n, c) => ArazzoV1Deserializer.LoadRequestBodyInternal(n, c, RequestBodyFixedFields, RequestBodyPatternFields));

    public static ArazzoPayloadReplacement LoadPayloadReplacement(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, static (n, c) => ArazzoV1Deserializer.LoadPayloadReplacementInternal(n, c, PayloadReplacementFixedFields, PayloadReplacementPatternFields));

    public static ArazzoCriterion LoadCriterion(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadCriterion);

    public static ArazzoCriterionExpressionType LoadCriterionExpressionType(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadCriterionExpressionType);

    public static IArazzoParameter LoadParameter(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadParameter);

    public static ArazzoParameter LoadParameterObject(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, ArazzoV1Deserializer.LoadParameterObject);

    public static IArazzoSuccessAction LoadSuccessAction(JsonNode node, ParsingContext context)
    {
        if (ArazzoV1Deserializer.TryGetReferenceObject(node, out _, out var referenceString))
        {
            return ArazzoV1Deserializer.CreateLocalReusableReference(
                referenceString,
                context,
                ReferenceType.SuccessAction,
                "Success action",
                static (referenceId, hostDocument) => new ArazzoSuccessActionReference(referenceId, hostDocument));
        }

        return LoadSuccessActionObject(node, context);
    }

    public static ArazzoSuccessAction LoadSuccessActionObject(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, static (n, c) => ArazzoV1Deserializer.LoadSuccessActionObjectInternal(n, c, SuccessActionFixedFields, SuccessActionPatternFields));

    public static IArazzoFailureAction LoadFailureAction(JsonNode node, ParsingContext context)
    {
        if (ArazzoV1Deserializer.TryGetReferenceObject(node, out _, out var referenceString))
        {
            return ArazzoV1Deserializer.CreateLocalReusableReference(
                referenceString,
                context,
                ReferenceType.FailureAction,
                "Failure action",
                static (referenceId, hostDocument) => new ArazzoFailureActionReference(referenceId, hostDocument));
        }

        return LoadFailureActionObject(node, context);
    }

    public static ArazzoFailureAction LoadFailureActionObject(JsonNode node, ParsingContext context) =>
        LoadWithArazzo1_1(node, context, static (n, c) => ArazzoV1Deserializer.LoadFailureActionObjectInternal(n, c, FailureActionFixedFields, FailureActionPatternFields));

    private static T LoadWithArazzo1_1<T>(JsonNode node, ParsingContext context, Func<JsonNode, ParsingContext, T> load)
    {
        context.Diagnostic.SpecificationVersion = ArazzoSpecVersion.Arazzo1_1;
        return load(node, context);
    }

    private static FixedFieldMap<ArazzoDocument> CreateDocumentFixedFields()
    {
        return new FixedFieldMap<ArazzoDocument>(
            ArazzoV1Deserializer.DocumentFixedFields,
            new HashSet<string>(StringComparer.Ordinal) { "x-$self" })
        {
            [ArazzoConstants.ArazzoDocumentSelf] = static (o, v, c) => o.Self = v.GetScalarValue()
        };
    }

    private static FixedFieldMap<ArazzoStep> CreateStepFixedFields()
    {
        return new FixedFieldMap<ArazzoStep>(
            ArazzoV1Deserializer.StepFixedFields,
            new HashSet<string>(StringComparer.Ordinal)
            {
                "x-channelPath",
                "x-action",
                "x-correlationId",
                "x-timeout",
                "x-dependsOn"
            })
        {
            [ArazzoConstants.ArazzoStepChannelPath] = static (o, v, c) => o.ChannelPath = v.GetScalarValue(),
            [ArazzoConstants.ArazzoStepAction] = static (o, v, c) =>
            {
                if (v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoStepAction>(c, out var action))
                {
                    o.Action = action;
                }
            },
            [ArazzoConstants.ArazzoStepCorrelationId] = static (o, v, c) => o.CorrelationId = v.GetScalarValue(),
            [ArazzoConstants.ArazzoStepTimeout] = static (o, v, c) =>
            {
                var value = v.GetScalarValue();
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout))
                {
                    o.Timeout = timeout;
                }
                else
                {
                    c.Diagnostic.Errors.Add(new OpenApiError($"{c.GetLocation()}/{ArazzoConstants.ArazzoStepTimeout}", $"{nameof(ArazzoStep)} timeout must be an integer. Invalid value: '{value}'."));
                }
            },
            [ArazzoConstants.ArazzoStepDependsOn] = static (o, v, c) => o.DependsOn = v.CreateSimpleList(static n => n.GetScalarValue()!, c).ToHashSet(StringComparer.Ordinal),
            [ArazzoConstants.ArazzoStepRequestBody] = static (o, v, c) => o.RequestBody = LoadRequestBody(v, c),
            [ArazzoConstants.ArazzoStepOnSuccess] = static (o, v, c) =>
            {
                o.OnSuccess = v.CreateList<IArazzoSuccessAction>(LoadSuccessAction, c);
                ArazzoActionListValidator.ValidateDeserialization(o.OnSuccess, c, $"{nameof(ArazzoStep)} onSuccess");
            },
            [ArazzoConstants.ArazzoStepOnFailure] = static (o, v, c) =>
            {
                o.OnFailure = v.CreateList<IArazzoFailureAction>(LoadFailureAction, c);
                ArazzoActionListValidator.ValidateDeserialization(o.OnFailure, c, $"{nameof(ArazzoStep)} onFailure");
            },
        };
    }

    private static FixedFieldMap<ArazzoPayloadReplacement> CreatePayloadReplacementFixedFields()
    {
        return new FixedFieldMap<ArazzoPayloadReplacement>(
            ArazzoV1Deserializer.PayloadReplacementFixedFields,
            new HashSet<string>(StringComparer.Ordinal) { "x-targetSelectorType" })
        {
            [ArazzoConstants.ArazzoPayloadReplacementTargetSelectorType] = static (o, v, c) => o.TargetSelectorType = v
        };
    }

    private static FixedFieldMap<ArazzoRequestBody> CreateRequestBodyFixedFields()
    {
        return new FixedFieldMap<ArazzoRequestBody>(ArazzoV1Deserializer.RequestBodyFixedFields)
        {
            [ArazzoConstants.ArazzoRequestBodyReplacements] = static (o, v, c) => o.Replacements = v.CreateList(LoadPayloadReplacement, c)
        };
    }

    private static FixedFieldMap<ArazzoSuccessAction> CreateSuccessActionFixedFields()
    {
        return new FixedFieldMap<ArazzoSuccessAction>(
            ArazzoV1Deserializer.SuccessActionFixedFields,
            new HashSet<string>(StringComparer.Ordinal) { "x-parameters" })
        {
            [ArazzoConstants.ArazzoResultActionParameters] = static (o, v, c) => o.Parameters = v.CreateList<IArazzoParameter>(LoadParameter, c)
        };
    }

    private static FixedFieldMap<ArazzoFailureAction> CreateFailureActionFixedFields()
    {
        return new FixedFieldMap<ArazzoFailureAction>(
            ArazzoV1Deserializer.FailureActionFixedFields,
            new HashSet<string>(StringComparer.Ordinal) { "x-parameters" })
        {
            [ArazzoConstants.ArazzoResultActionParameters] = static (o, v, c) => o.Parameters = v.CreateList<IArazzoParameter>(LoadParameter, c)
        };
    }

    private static FixedFieldMap<ArazzoWorkflow> CreateWorkflowFixedFields()
    {
        return new FixedFieldMap<ArazzoWorkflow>(ArazzoV1Deserializer.WorkflowFixedFields)
        {
            [ArazzoConstants.ArazzoWorkflowSteps] = static (o, v, c) => o.Steps = v.CreateList(LoadStep, c),
            [ArazzoConstants.ArazzoWorkflowSuccessActions] = static (o, v, c) =>
            {
                o.SuccessActions = v.CreateList<IArazzoSuccessAction>(LoadSuccessAction, c);
                ArazzoActionListValidator.ValidateDeserialization(o.SuccessActions, c, $"{nameof(ArazzoWorkflow)} successActions");
            },
            [ArazzoConstants.ArazzoWorkflowFailureActions] = static (o, v, c) =>
            {
                o.FailureActions = v.CreateList<IArazzoFailureAction>(LoadFailureAction, c);
                ArazzoActionListValidator.ValidateDeserialization(o.FailureActions, c, $"{nameof(ArazzoWorkflow)} failureActions");
            }
        };
    }

    private static FixedFieldMap<ArazzoComponent> CreateComponentFixedFields()
    {
        return new FixedFieldMap<ArazzoComponent>(ArazzoV1Deserializer.ComponentFixedFields)
        {
            [ArazzoConstants.ArazzoComponentSuccessActions] = static (o, v, c) =>
            {
                ArazzoKeyValidator.ValidateDeserializationKeys(v, c, $"{nameof(ArazzoComponent)}.{nameof(ArazzoComponent.SuccessActions)}");
                o.SuccessActions = v.CreateMap(LoadSuccessActionObject, c);
            },
            [ArazzoConstants.ArazzoComponentFailureActions] = static (o, v, c) =>
            {
                ArazzoKeyValidator.ValidateDeserializationKeys(v, c, $"{nameof(ArazzoComponent)}.{nameof(ArazzoComponent.FailureActions)}");
                o.FailureActions = v.CreateMap(LoadFailureActionObject, c);
            }
        };
    }
}