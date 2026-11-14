using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a literal JSON value, runtime expression, or selector used in a payload replacement.
/// Exactly one of <see cref="Any"/>, <see cref="Expression"/>, or <see cref="Selector"/> must be set.
/// </summary>
/// <remarks>
/// <para>
/// Use property patterns to test which member is populated. The <c>is</c> operator does not
/// invoke implicit conversions, so test the union's properties rather than the union's type.
/// </para>
/// <code>
/// var replacement = new ArazzoPayloadReplacement
/// {
///     Value = new ArazzoRuntimeExpression("$inputs.id")
/// };
///
/// if (replacement.Value is { Any: JsonNode any })
/// {
///     Console.WriteLine(any.ToJsonString());
/// }
/// else if (replacement.Value is { Expression: ArazzoRuntimeExpression expression })
/// {
///     Console.WriteLine(expression.Expression);
/// }
/// else if (replacement.Value is { Selector: ArazzoSelector selector })
/// {
///     Console.WriteLine(selector.Selector);
/// }
/// </code>
/// </remarks>
public class ArazzoPayloadReplacementValueUnion : IArazzoSerializable, IOpenApiElement
{
    /// <summary>
    /// Gets or sets the literal JSON value. Use <see cref="JsonNullSentinel.JsonNull"/> for an explicit JSON null.
    /// </summary>
    public JsonNode? Any { get; set; }

    /// <summary>
    /// Gets or sets the runtime expression.
    /// </summary>
    public ArazzoRuntimeExpression? Expression { get; set; }

    /// <summary>
    /// Gets or sets the selector.
    /// </summary>
    public ArazzoSelector? Selector { get; set; }

    /// <summary>
    /// Converts a JSON value to a payload replacement value.
    /// </summary>
    /// <param name="value">The JSON value, or null for an explicit JSON null.</param>
    public static implicit operator ArazzoPayloadReplacementValueUnion(JsonNode? value) => new() { Any = value ?? JsonNullSentinel.JsonNull };

    /// <summary>Converts a literal string to a payload replacement value.</summary>
    /// <param name="value">The literal string, or null for an explicit JSON null.</param>
    public static implicit operator ArazzoPayloadReplacementValueUnion(string? value) => (JsonNode?)JsonValue.Create(value);

    /// <summary>
    /// Converts a runtime expression to a payload replacement value.
    /// </summary>
    /// <param name="value">The runtime expression.</param>
    public static implicit operator ArazzoPayloadReplacementValueUnion(ArazzoRuntimeExpression value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Expression = value };
    }

    /// <summary>
    /// Converts a selector to a payload replacement value.
    /// </summary>
    /// <param name="value">The selector.</param>
    public static implicit operator ArazzoPayloadReplacementValueUnion(ArazzoSelector value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Selector = value };
    }

    /// <summary>Gets the active literal, throwing for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator JsonNode(ArazzoPayloadReplacementValueUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Any is { } any && value.Expression is null && value.Selector is null ? any : throw new InvalidCastException("The literal is not the only populated member.");
    }

    /// <summary>Gets the active expression, throwing for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoRuntimeExpression(ArazzoPayloadReplacementValueUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Expression is { } expression && value.Any is null && value.Selector is null ? expression : throw new InvalidCastException("The expression is not the only populated member.");
    }

    /// <summary>Gets the active selector, throwing for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoSelector(ArazzoPayloadReplacementValueUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Selector is { } selector && value.Any is null && value.Expression is null ? selector : throw new InvalidCastException("The selector is not the only populated member.");
    }

    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer) => SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0);

    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer) => SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1);

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if ((Any is null ? 0 : 1) + (Expression is null ? 0 : 1) + (Selector is null ? 0 : 1) != 1)
        {
            throw new ArazzoSerializationException($"{nameof(ArazzoPayloadReplacementValueUnion)} must have exactly one of Any, Expression, or Selector set.");
        }

        if (Selector is not null)
        {
            if (specVersion is ArazzoSpecVersion.Arazzo1_0)
            {
                throw new ArazzoSerializationException("Selector payload replacement values are not supported in Arazzo 1.0.");
            }

            Selector.SerializeAsV1_1(writer);
        }
        else if (Expression is not null)
        {
            if (!ArazzoRuntimeExpressionValidator.IsRuntimeExpression(Expression.Expression, specVersion))
            {
                throw new ArazzoSerializationException($"{nameof(ArazzoPayloadReplacementValueUnion)}.{nameof(Expression)} must be a valid runtime expression for {specVersion}. Invalid value: '{Expression.Expression}'.");
            }

            writer.WriteValue(Expression.Expression);
        }
        else
        {
            writer.WriteAny(Any);
        }
    }
}