using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the common properties and behavior for result actions.
/// </summary>
/// <typeparam name="T">The type of the action, constrained to enums.</typeparam>
public abstract class ArazzoResultAction<T> : IResultAction<T> where T : struct, Enum
{
    /// <inheritdoc/>
    public string? Name { get; set; }

    /// <inheritdoc/>
    public T? Type { get; set; }

    /// <inheritdoc/>
    public string? WorkflowId { get; set; }

    /// <inheritdoc/>
    public string? StepId { get; set; }

    /// <inheritdoc/>
    public IList<ArazzoCriterion>? Criteria { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the common properties of the result action as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    protected void SerializeCommonPropertiesAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(Name);
        if (!Type.HasValue)
        {
            throw new ArgumentNullException(nameof(Type));
        }

        writer.WriteRequiredProperty(ArazzoConstants.ArazzoResultActionName, Name);
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoResultActionType, Type.Value.GetDisplayName());

        if (!string.IsNullOrEmpty(WorkflowId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoResultActionWorkflowId, WorkflowId);
        }

        if (!string.IsNullOrEmpty(StepId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoResultActionStepId, StepId);
        }

        if (Criteria != null && Criteria.Count > 0)
        {
            writer.WritePropertyName(ArazzoConstants.ArazzoResultActionCriteria);
            writer.WriteStartArray();
            foreach (var criterion in Criteria)
            {
                criterion.SerializeAsV1(writer);
            }
            writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Serializes the result action as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public abstract void SerializeAsV1(IOpenApiWriter writer);
}