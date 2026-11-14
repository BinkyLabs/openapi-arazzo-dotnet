using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>A Success Action Object or a reusable success action reference. Exactly one member must be populated.</summary>
/// <remarks>
/// Inspect the properties; type tests do not invoke implicit conversions.
/// <code>
/// if (value is { SuccessAction: ArazzoSuccessAction action }) { Console.WriteLine(action.Name); }
/// else if (value is { Reference: ArazzoSuccessActionReference reference }) { Console.WriteLine(reference.Reference.ReferenceV1); }
/// </code>
/// </remarks>
public class ArazzoSuccessActionUnion : IArazzoSuccessAction
{
    /// <summary>Gets or sets the inline success action.</summary>
    public ArazzoSuccessAction? SuccessAction { get; set; }
    /// <summary>Gets or sets the reusable success action reference.</summary>
    public ArazzoSuccessActionReference? Reference { get; set; }
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
    public ArazzoSuccessType? Type => GetAction().Type;
    /// <summary>Converts an action to a union.</summary>
    /// <param name="value">The action.</param>
    public static implicit operator ArazzoSuccessActionUnion(ArazzoSuccessAction value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { SuccessAction = value };
    }
    /// <summary>Converts a reference to a union.</summary>
    /// <param name="value">The reference.</param>
    public static implicit operator ArazzoSuccessActionUnion(ArazzoSuccessActionReference value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Reference = value };
    }
    /// <summary>Gets the active inline action.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoSuccessAction(ArazzoSuccessActionUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.SuccessAction is { } action && value.Reference is null ? action : throw new InvalidCastException("The action is not the only populated member.");
    }
    /// <summary>Gets the active reference.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoSuccessActionReference(ArazzoSuccessActionUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Reference is { } reference && value.SuccessAction is null ? reference : throw new InvalidCastException("The reference is not the only populated member.");
    }
    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer) => GetAction().SerializeAsV1(writer);
    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer) => GetAction().SerializeAsV1_1(writer);

    internal IArazzoSuccessAction GetAction() => (SuccessAction, Reference) switch
    {
        ({ } action, null) => action,
        (null, { } reference) => reference,
        _ => throw new ArazzoSerializationException("ArazzoSuccessActionUnion must have exactly one of SuccessAction or Reference.")
    };
}