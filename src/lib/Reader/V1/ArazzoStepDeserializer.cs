using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoStep> StepFixedFields = new()
    {
        { ArazzoConstants.ArazzoStepDescription, static (o, v, c) => o.Description = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepStepId, static (o, v, c) => o.StepId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepOperationId, static (o, v, c) => o.OperationId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepOperationPath, static (o, v, c) => o.OperationPath = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepWorkflowId, static (o, v, c) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepParameters, static (o, v, c) => o.Parameters = v.CreateList<IArazzoParameter>(LoadParameter, c) },
        { ArazzoConstants.ArazzoStepRequestBody, static (o, v, c) => o.RequestBody = LoadRequestBody(v, c) },
        { ArazzoConstants.ArazzoStepSuccessCriteria, static (o, v, c) => o.SuccessCriteria = v.CreateList(LoadCriterion, c) },
        { ArazzoConstants.ArazzoStepOnSuccess, static (o, v, c) =>
        {
            o.OnSuccess = v.CreateList<IArazzoSuccessAction>(LoadSuccessAction, c);
            ArazzoActionListValidator.ValidateDeserialization(o.OnSuccess, c, $"{nameof(ArazzoStep)} onSuccess");
        } },
        { ArazzoConstants.ArazzoStepOnFailure, static (o, v, c) =>
        {
            o.OnFailure = v.CreateList<IArazzoFailureAction>(LoadFailureAction, c);
            ArazzoActionListValidator.ValidateDeserialization(o.OnFailure, c, $"{nameof(ArazzoStep)} onFailure");
        } },
        { ArazzoConstants.ArazzoStepOutputs, static (o, v, c) =>
        {
            ArazzoKeyValidator.ValidateDeserializationKeys(v, c, $"{nameof(ArazzoStep)}.{nameof(ArazzoStep.Outputs)}");
            var outputs = v.CreateSimpleMap(static n => n.GetScalarValue(), c)
                .Where(static x => x.Value is not null)
                .ToDictionary(static x => x.Key, static x => x.Value!);
            ArazzoRuntimeExpressionValidator.ValidateDeserializationExpressions(outputs, c, $"{nameof(ArazzoStep)}.{nameof(ArazzoStep.Outputs)}");
            o.Outputs = outputs;
        } }
    };

    public static readonly PatternFieldMap<ArazzoStep> StepPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static ArazzoStep LoadStep(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("Step", context);
        var step = new ArazzoStep();
        mapNode.ParseMap(step, StepFixedFields, StepPatternFields, context);
        ValidateStepRequiredFields(step, context);
        ValidateStepTargetFields(step, context);
        ArazzoSemanticReferenceValidator.ValidateOperationPathDeserialization(step.OperationPath, context, $"{nameof(ArazzoStep)} '{step.StepId}'");

        return step;
    }

    private static void ValidateStepRequiredFields(ArazzoStep step, ParsingContext context)
    {
        if (string.IsNullOrEmpty(step.StepId))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoStep)}.{nameof(ArazzoStep.StepId)} is a REQUIRED field."));
        }
    }

    private static void ValidateStepTargetFields(ArazzoStep step, ParsingContext context)
    {
        var referenceCount = step.CountTargetFields();
        if (referenceCount > 1)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoStep)} '{step.StepId}' can define only one of operationId, operationPath, or workflowId."));
        }

        if (referenceCount == 0)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoStep)} '{step.StepId}' must define exactly one of operationId, operationPath, or workflowId."));
        }

        if (step.RequestBody is not null && !step.CanHaveRequestBody())
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoStep)} '{step.StepId}' requestBody can only be specified when the step targets operationId or operationPath."));
        }
    }
}