using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a step definition in an Arazzo workflow.
/// </summary>
public class ArazzoStep : IArazzoExtensible, IArazzoSerializable
{
    /// <summary>
    /// Gets or sets the description of the step.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the step identifier.
    /// </summary>
    public string? StepId { get; set; }

    /// <summary>
    /// Gets or sets the operation identifier.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Gets or sets the operation path.
    /// </summary>
    public string? OperationPath { get; set; }

    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the list of parameters.
    /// </summary>
    public List<IArazzoParameter>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the request body.
    /// </summary>
    public ArazzoRequestBody? RequestBody { get; set; }

    /// <summary>
    /// Gets or sets the success criteria.
    /// </summary>
    public List<ArazzoCriterion>? SuccessCriteria { get; set; }

    /// <summary>
    /// Gets or sets the success actions.
    /// </summary>
    public List<IArazzoSuccessAction>? OnSuccess { get; set; }

    /// <summary>
    /// Gets or sets the failure actions.
    /// </summary>
    public List<IArazzoFailureAction>? OnFailure { get; set; }

    /// <summary>
    /// Gets or sets the output expressions.
    /// </summary>
    // TODO: Implement ABNF parsing of the output expressions
    public IDictionary<string, string>? Outputs { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the step as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(StepId);

        writer.WriteStartObject();

        if (!string.IsNullOrEmpty(Description))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoStepDescription, Description);
        }

        writer.WriteProperty(ArazzoConstants.ArazzoStepStepId, StepId);

        if (!string.IsNullOrEmpty(OperationId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoStepOperationId, OperationId);
        }

        if (!string.IsNullOrEmpty(OperationPath))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoStepOperationPath, OperationPath);
        }

        if (!string.IsNullOrEmpty(WorkflowId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoStepWorkflowId, WorkflowId);
        }

        if (Parameters is not null && Parameters.Count > 0)
        {
            writer.WritePropertyName(ArazzoConstants.ArazzoStepParameters);
            writer.WriteStartArray();
            foreach (var parameter in Parameters)
            {
                parameter?.SerializeAsV1(writer);
            }
            writer.WriteEndArray();
        }

        writer.WriteOptionalObject(
            ArazzoConstants.ArazzoStepRequestBody,
            RequestBody,
            (w, rb) => rb.SerializeAsV1(w));

        if (SuccessCriteria is not null && SuccessCriteria.Count > 0)
        {
            writer.WritePropertyName(ArazzoConstants.ArazzoStepSuccessCriteria);
            writer.WriteStartArray();
            foreach (var criterion in SuccessCriteria)
            {
                criterion?.SerializeAsV1(writer);
            }
            writer.WriteEndArray();
        }

        if (OnSuccess is not null && OnSuccess.Count > 0)
        {
            writer.WritePropertyName(ArazzoConstants.ArazzoStepOnSuccess);
            writer.WriteStartArray();
            foreach (var action in OnSuccess)
            {
                action?.SerializeAsV1(writer);
            }
            writer.WriteEndArray();
        }

        if (OnFailure is not null && OnFailure.Count > 0)
        {
            writer.WritePropertyName(ArazzoConstants.ArazzoStepOnFailure);
            writer.WriteStartArray();
            foreach (var action in OnFailure)
            {
                action?.SerializeAsV1(writer);
            }
            writer.WriteEndArray();
        }

        writer.WriteOptionalMap(
            ArazzoConstants.ArazzoStepOutputs,
            Outputs,
            (w, v) => w.WriteValue(v));

        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}