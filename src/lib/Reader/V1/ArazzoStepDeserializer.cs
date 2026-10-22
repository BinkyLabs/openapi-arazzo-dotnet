using System.Globalization;
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
        { ArazzoConstants.GetArazzo1_0ExtensionName(ArazzoConstants.ArazzoStepChannelPath), static (o, v, c) => o.ChannelPath = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepWorkflowId, static (o, v, c) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.GetArazzo1_0ExtensionName(ArazzoConstants.ArazzoStepAction), static (o, v, c) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoStepAction>(c, out var action))
            {
                return;
            }
            o.Action = action;
        } },
        { ArazzoConstants.GetArazzo1_0ExtensionName(ArazzoConstants.ArazzoStepCorrelationId), static (o, v, c) => o.CorrelationId = v.GetScalarValue() },
        { ArazzoConstants.GetArazzo1_0ExtensionName(ArazzoConstants.ArazzoStepTimeout), static (o, v, c) =>
        {
            var value = v.GetScalarValue();
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout))
            {
                o.Timeout = timeout;
            }
            else
            {
                c.Diagnostic.Errors.Add(new OpenApiError($"{c.GetLocation()}/{ArazzoConstants.GetArazzo1_0ExtensionName(ArazzoConstants.ArazzoStepTimeout)}", $"{nameof(ArazzoStep)} timeout must be an integer. Invalid value: '{value}'."));
            }
        } },
        { ArazzoConstants.GetArazzo1_0ExtensionName(ArazzoConstants.ArazzoStepDependsOn), static (o, v, c) => o.DependsOn = v.CreateSimpleList(static n => n.GetScalarValue()!, c).ToHashSet(StringComparer.Ordinal) },
        { ArazzoConstants.ArazzoStepParameters, static (o, v, c) => o.Parameters = v.CreateUnionList(LoadParameterUnion, c) },
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
            o.Outputs = LoadOutputMap(v, c);
        } }
    };

    public static PatternFieldMap<ArazzoStep> GetStepPatternFields() =>
    new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };
    public static readonly PatternFieldMap<ArazzoStep> StepPatternFields = GetStepPatternFields();

    public static ArazzoStep LoadStep(JsonNode node, ParsingContext context)
    {
        return LoadStepInternal(node, context, StepFixedFields, StepPatternFields);
    }

    public static ArazzoStep LoadStepInternal(JsonNode node, ParsingContext context, FixedFieldMap<ArazzoStep> stepFixedFields, PatternFieldMap<ArazzoStep> stepPatternFields)
    {
        var mapNode = node.CheckMapNode("Step", context);
        var step = new ArazzoStep();
        mapNode.ParseMap(step, stepFixedFields, stepPatternFields, context);
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
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), ArazzoStep.GetMultipleTargetFieldsError($"{nameof(ArazzoStep)} '{step.StepId}'", context.Diagnostic.SpecificationVersion)));
        }

        if (referenceCount == 0)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), ArazzoStep.GetMissingTargetFieldError($"{nameof(ArazzoStep)} '{step.StepId}'", context.Diagnostic.SpecificationVersion)));
        }

        if (step.RequestBody is not null && !step.CanHaveRequestBody())
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoStep)} '{step.StepId}' requestBody can only be specified when the step targets operationId or operationPath."));
        }
    }

}