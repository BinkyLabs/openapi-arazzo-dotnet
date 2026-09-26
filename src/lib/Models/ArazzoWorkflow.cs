using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;
using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a workflow definition in an Arazzo document.
/// </summary>
public class ArazzoWorkflow : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the summary of the workflow.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the description of the workflow.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the inputs schema.
    /// </summary>
    public IArazzoInput? Inputs { get; set; }

    /// <summary>
    /// Gets or sets the set of workflow identifiers that this workflow depends on.
    /// </summary>
    public ISet<string>? DependsOn { get; set; }

    /// <summary>
    /// Gets or sets the list of steps in the workflow.
    /// </summary>
    public IList<ArazzoStep>? Steps { get; set; }

    /// <summary>
    /// Gets or sets the list of success actions.
    /// </summary>
    public IList<IArazzoSuccessAction>? SuccessActions { get; set; }

    /// <summary>
    /// Gets or sets the list of failure actions.
    /// </summary>
    public IList<IArazzoFailureAction>? FailureActions { get; set; }

    /// <summary>
    /// Gets or sets output values as runtime expressions or selector objects.
    /// </summary>
    public IDictionary<string, JsonNode>? OutputValues { get; set; }

    /// <summary>
    /// Gets or sets the outputs dictionary.
    /// </summary>
    public IDictionary<string, string>? Outputs { get; set; }

    /// <summary>
    /// Gets or sets the list of workflow parameters.
    /// </summary>
    public IList<IArazzoParameter>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the extensions dictionary.
    /// </summary>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the workflow as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <summary>
    /// Serializes the workflow as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1, static (w, obj) => obj.SerializeAsV1_1(w));
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(WorkflowId);
        if (Steps is not { Count: > 0 })
        {
            throw new ArazzoSerializationException("Steps is required and must contain at least one element for ArazzoWorkflow serialization.");
        }
        ValidateUniqueStepIds();
        ValidateWorkflowParameters();
        ValidateActions();
        ArazzoRuntimeExpressionValidator.ValidateSerializationExpressions(Outputs, $"{nameof(ArazzoWorkflow)}.{nameof(Outputs)}", specVersion);

        writer.WriteStartObject();
        writer.WriteProperty(ArazzoConstants.ArazzoWorkflowWorkflowId, WorkflowId);
        writer.WriteProperty(ArazzoConstants.ArazzoWorkflowSummary, Summary);
        writer.WriteProperty(ArazzoConstants.ArazzoWorkflowDescription, Description);

        // Write inputs
        writer.WriteOptionalObject(ArazzoConstants.ArazzoWorkflowInputs, Inputs, callback);

        // Write dependsOn
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowDependsOn, DependsOn, static (w, d) => w.WriteValue(d!));

        // Write steps
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowSteps, Steps, callback);

        // Write success actions
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowSuccessActions, SuccessActions, callback);

        // Write failure actions
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowFailureActions, FailureActions, callback);

        ArazzoKeyValidator.ValidateSerializationKeys((OutputValues?.Keys ?? Outputs?.Keys), $"{nameof(ArazzoWorkflow)}.{nameof(Outputs)}");

        // Write outputs
        if (OutputValues is not null)
        {
            writer.WriteOptionalMap(ArazzoConstants.ArazzoWorkflowOutputs, OutputValues, static (w, s) => w.WriteAny(s));
        }
        else
        {
            writer.WriteOptionalMap(ArazzoConstants.ArazzoWorkflowOutputs, Outputs, static (w, s) => w.WriteValue(s));
        }

        // Write parameters
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowParameters, Parameters, callback);

        writer.WriteArazzoExtensions(Extensions, specVersion);
        writer.WriteEndObject();
    }

    private void ValidateUniqueStepIds()
    {
        var stepIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in Steps ?? [])
        {
            if (!string.IsNullOrEmpty(step.StepId) && !stepIds.Add(step.StepId))
            {
                throw new ArazzoSerializationException($"Workflow '{WorkflowId}' contains duplicate stepId '{step.StepId}'.");
            }
        }
    }

    private void ValidateWorkflowParameters()
    {
        ArazzoParameterValidator.ValidateSerializationParameters(
            Parameters,
            $"{nameof(ArazzoWorkflow)} '{WorkflowId}'",
            Steps?.Any(static step => !string.IsNullOrEmpty(step.OperationId) || !string.IsNullOrEmpty(step.OperationPath)) == true,
            "when applied to an operation step");
    }

    private void ValidateActions()
    {
        ArazzoActionListValidator.ValidateSerialization(SuccessActions, $"{nameof(ArazzoWorkflow)} '{WorkflowId}' successActions");
        ArazzoActionListValidator.ValidateSerialization(FailureActions, $"{nameof(ArazzoWorkflow)} '{WorkflowId}' failureActions");
    }
}