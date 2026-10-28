using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>A Failure Action Object or a reusable failure action reference. Exactly one member must be populated.</summary>
/// <remarks>
/// Inspect the properties; type tests do not invoke implicit conversions.
/// <code>
/// if (value is { FailureAction: ArazzoFailureAction action }) { Console.WriteLine(action.Name); }
/// else if (value is { Reference: ArazzoFailureActionReference reference }) { Console.WriteLine(reference.Reference.ReferenceV1); }
/// </code>
/// </remarks>
public class ArazzoFailureActionUnion : IArazzoFailureAction
{
    /// <summary>Gets or sets the inline failure action.</summary>
    public ArazzoFailureAction? FailureAction { get; set; }
    /// <summary>Gets or sets the reusable failure action reference.</summary>
    public ArazzoFailureActionReference? Reference { get; set; }
    /// <inheritdoc/>
    public string? Name => GetAction().Name;
    /// <inheritdoc/>
    public string? WorkflowId => GetAction().WorkflowId;
    /// <inheritdoc/>
    public string? StepId => GetAction().StepId;
    /// <inheritdoc/>
    public IList<ArazzoCriterion>? Criteria => GetAction().Criteria;
    /// <inheritdoc/>
    public IList<IArazzoParameter>? Parameters => GetAction().Parameters;
    /// <inheritdoc/>
    public ArazzoFailureType? Type => GetAction().Type;
    /// <inheritdoc/>
    public decimal? RetryAfter => GetAction().RetryAfter;
    /// <inheritdoc/>
    public ulong RetryLimit => GetAction().RetryLimit;
    /// <summary>Converts an action to a union.</summary>
    /// <param name="value">The action.</param>
    public static implicit operator ArazzoFailureActionUnion(ArazzoFailureAction value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { FailureAction = value };
    }
    /// <summary>Converts a reference to a union.</summary>
    /// <param name="value">The reference.</param>
    public static implicit operator ArazzoFailureActionUnion(ArazzoFailureActionReference value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Reference = value };
    }
    /// <summary>Gets the active inline action.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoFailureAction(ArazzoFailureActionUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.FailureAction is { } action && value.Reference is null ? action : throw new InvalidCastException("The action is not the only populated member.");
    }
    /// <summary>Gets the active reference.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoFailureActionReference(ArazzoFailureActionUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Reference is { } reference && value.FailureAction is null ? reference : throw new InvalidCastException("The reference is not the only populated member.");
    }
    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer) => GetAction().SerializeAsV1(writer);
    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer) => GetAction().SerializeAsV1_1(writer);

    internal IArazzoFailureAction GetAction() => (FailureAction, Reference) switch
    {
        ({ } action, null) => action,
        (null, { } reference) => reference,
        _ => throw new ArazzoSerializationException("ArazzoFailureActionUnion must have exactly one of FailureAction or Reference.")
    };
}