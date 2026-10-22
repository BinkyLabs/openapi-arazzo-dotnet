using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a selector language or an Expression Type Object used to resolve a payload replacement target.
/// Exactly one of <see cref="Type"/> or <see cref="ExpressionType"/> must be set.
/// </summary>
public class ArazzoPayloadReplacementTargetSelectorTypeUnion : IArazzoSerializable, IOpenApiElement
{
    /// <summary>
    /// Gets or sets the selector language.
    /// </summary>
    public ArazzoPayloadRequirementTargetSelectorType? Type { get; set; }

    /// <summary>
    /// Gets or sets the Expression Type Object.
    /// </summary>
    public ArazzoCriterionExpressionType? ExpressionType { get; set; }

    /// <summary>
    /// Converts a selector language to a target selector type.
    /// </summary>
    /// <param name="value">The selector language.</param>
    public static implicit operator ArazzoPayloadReplacementTargetSelectorTypeUnion(ArazzoPayloadRequirementTargetSelectorType value) =>
        new() { Type = value };

    /// <summary>
    /// Converts an Expression Type Object to a target selector type.
    /// </summary>
    /// <param name="value">The Expression Type Object.</param>
    public static implicit operator ArazzoPayloadReplacementTargetSelectorTypeUnion(ArazzoCriterionExpressionType value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { ExpressionType = value };
    }

    /// <summary>
    /// Converts a target selector type to its selector language.
    /// </summary>
    /// <param name="value">The target selector type.</param>
    /// <exception cref="ArgumentNullException">The target selector type is null.</exception>
    /// <exception cref="InvalidCastException">The selector language is not the only populated branch.</exception>
    public static implicit operator ArazzoPayloadRequirementTargetSelectorType(ArazzoPayloadReplacementTargetSelectorTypeUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Type is not { } type || value.ExpressionType is not null)
        {
            throw new InvalidCastException($"{nameof(ArazzoPayloadReplacementTargetSelectorTypeUnion)} must have only {nameof(Type)} set to convert to {nameof(ArazzoPayloadRequirementTargetSelectorType)}.");
        }

        return type;
    }

    /// <summary>
    /// Converts a target selector type to its Expression Type Object.
    /// </summary>
    /// <param name="value">The target selector type.</param>
    /// <exception cref="ArgumentNullException">The target selector type is null.</exception>
    /// <exception cref="InvalidCastException">The Expression Type Object is not the only populated branch.</exception>
    public static implicit operator ArazzoCriterionExpressionType(ArazzoPayloadReplacementTargetSelectorTypeUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.ExpressionType is not { } expressionType || value.Type.HasValue)
        {
            throw new InvalidCastException($"{nameof(ArazzoPayloadReplacementTargetSelectorTypeUnion)} must have only {nameof(ExpressionType)} set to convert to {nameof(ArazzoCriterionExpressionType)}.");
        }

        return expressionType;
    }

    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer) =>
        SerializeInternal(writer, static (w, obj) => obj.SerializeAsV1(w));

    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer) =>
        SerializeInternal(writer, static (w, obj) => obj.SerializeAsV1_1(w));

    private void SerializeInternal(IOpenApiWriter writer, Action<IOpenApiWriter, ArazzoCriterionExpressionType> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (Type.HasValue == (ExpressionType is not null))
        {
            throw new ArazzoSerializationException($"{nameof(ArazzoPayloadReplacementTargetSelectorTypeUnion)} must have exactly one of Type or ExpressionType set.");
        }

        if (Type is { } type)
        {
            if (!Enum.IsDefined(type))
            {
                throw new ArazzoSerializationException($"Invalid payload replacement target selector type: '{type}'.");
            }

            writer.WriteValue(type.GetDisplayName());
        }
        else if (ExpressionType is not null)
        {
            callback(writer, ExpressionType);
        }
    }
}