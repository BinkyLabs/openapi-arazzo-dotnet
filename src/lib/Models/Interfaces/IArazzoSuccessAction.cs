namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a success action definition.
/// </summary>
public interface IArazzoSuccessAction : IArazzoSerializable, IArazzoExtensible, IArazzoReferenceable
{
    /// <summary>
    /// Gets or sets the success action name.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets or sets the type of the success action.
    /// </summary>
    ArazzoSuccessType? Type { get; }

    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    string? WorkflowId { get; }

    /// <summary>
    /// Gets or sets the step identifier.
    /// </summary>
    string? StepId { get; }

    /// <summary>
    /// Gets or sets the criteria list.
    /// </summary>
    IList<ArazzoCriterion>? Criteria { get; }
}