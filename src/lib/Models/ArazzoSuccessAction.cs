using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a success action definition.
/// </summary>
public class ArazzoSuccessAction : IArazzoSuccessAction
{
    /// <inheritdoc/>
    public string? Name { get; set; }

    /// <inheritdoc/>
    public ArazzoSuccessType? Type { get; set; }

    /// <inheritdoc/>
    public string? WorkflowId { get; set; }

    /// <inheritdoc/>
    public string? StepId { get; set; }

    /// <inheritdoc/>
    public IList<ArazzoCriterion>? Criteria { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the success action as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(Name);
        if (!Type.HasValue)
        {
            throw new ArgumentNullException(nameof(Type));
        }

        writer.WriteStartObject();
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoSuccessActionName, Name);
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoSuccessActionType, Type.Value.GetDisplayName());
        if (!string.IsNullOrEmpty(WorkflowId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoSuccessActionWorkflowId, WorkflowId);
        }
        if (!string.IsNullOrEmpty(StepId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoSuccessActionStepId, StepId);
        }
        if (Criteria != null && Criteria.Count > 0)
        {
            writer.WritePropertyName(ArazzoConstants.ArazzoSuccessActionCriteria);
            writer.WriteStartArray();
            foreach (var criterion in Criteria)
            {
                criterion.SerializeAsV1(writer);
            }
            writer.WriteEndArray();
        }
        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}