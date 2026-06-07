namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the common properties of result actions.
/// </summary>
public interface IArazzoResultAction : IArazzoSerializable, IArazzoReferenceable
{
    /// <summary>
    /// Gets or sets the action name.
    /// </summary>
    string? Name { get; }

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

/// <summary>
/// Represents a result action with a specific type.
/// </summary>
/// <typeparam name="T">The type of the action, constrained to enums.</typeparam>
public interface IArazzoResultAction<T> : IArazzoResultAction where T : struct, Enum
{
    /// <summary>
    /// Gets or sets the type of the action.
    /// </summary>
    T? Type { get; }
}