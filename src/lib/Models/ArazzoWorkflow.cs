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
    /// Gets or sets the outputs dictionary.
    /// </summary>
    // TODO: Implement validation during serialization/deserialization that any of the keys 
    // of the Outputs dictionary must match the following regex: ^[a-zA-Z0-9\.\-_]+$
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
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(WorkflowId);

        writer.WriteStartObject();
        writer.WriteProperty(ArazzoConstants.ArazzoWorkflowWorkflowId, WorkflowId);
        writer.WriteProperty(ArazzoConstants.ArazzoWorkflowSummary, Summary);

        // Write inputs
        writer.WriteOptionalObject(ArazzoConstants.ArazzoWorkflowInputs, Inputs, static (w, i) => i.SerializeAsV1(w));

        // Write dependsOn
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowDependsOn, DependsOn, static (w, d) => w.WriteValue(d!));

        // Write steps
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowSteps, Steps, static (w, s) => s.SerializeAsV1(w));

        // Write success actions
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowSuccessActions, SuccessActions, static (w, a) => a.SerializeAsV1(w));

        // Write failure actions
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowFailureActions, FailureActions, static (w, a) => a.SerializeAsV1(w));

        // Write outputs
        writer.WriteOptionalMap(ArazzoConstants.ArazzoWorkflowOutputs, Outputs, static (w, s) => w.WriteValue(s));

        // Write parameters
        writer.WriteOptionalCollection(ArazzoConstants.ArazzoWorkflowParameters, Parameters, static (w, p) => p.SerializeAsV1(w));

        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}