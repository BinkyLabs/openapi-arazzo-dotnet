using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>A selector language or an Expression Type Object. Exactly one member must be populated.</summary>
/// <remarks>
/// Type tests do not invoke conversions; inspect the populated property.
/// <code>
/// if (selector.Type is { Type: ArazzoPayloadRequirementTargetSelectorType type }) { Console.WriteLine(type); }
/// else if (selector.Type is { ExpressionType: ArazzoCriterionExpressionType expressionType }) { Console.WriteLine(expressionType.Version); }
/// </code>
/// </remarks>
public class ArazzoSelectorTypeUnion : IArazzoSerializable, IOpenApiElement
{
    /// <summary>Gets or sets the selector language.</summary>
    public ArazzoPayloadRequirementTargetSelectorType? Type { get; set; }
    /// <summary>Gets or sets the Expression Type Object.</summary>
    public ArazzoCriterionExpressionType? ExpressionType { get; set; }
    /// <summary>Converts a selector language to a union.</summary>
    /// <param name="value">The language.</param>
    public static implicit operator ArazzoSelectorTypeUnion(ArazzoPayloadRequirementTargetSelectorType value) => new() { Type = value };
    /// <summary>Converts an expression type to a union.</summary>
    /// <param name="value">The expression type.</param>
    public static implicit operator ArazzoSelectorTypeUnion(ArazzoCriterionExpressionType value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { ExpressionType = value };
    }
    /// <summary>Gets the active selector language or throws for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoPayloadRequirementTargetSelectorType(ArazzoSelectorTypeUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return (ArazzoPayloadRequirementTargetSelectorType)value.AsTargetType();
    }
    /// <summary>Gets the active expression type or throws for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoCriterionExpressionType(ArazzoSelectorTypeUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return (ArazzoCriterionExpressionType)value.AsTargetType();
    }
    private ArazzoPayloadReplacementTargetSelectorTypeUnion AsTargetType() => new() { Type = Type, ExpressionType = ExpressionType };
    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        throw new ArazzoSerializationException("Selector types are not supported in Arazzo 1.0.");
    }
    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer) => AsTargetType().SerializeAsV1_1(writer);
}