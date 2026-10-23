using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// A literal JSON payload, runtime expression, or selector. Exactly one member must be populated.
/// </summary>
/// <remarks>
/// Inspect properties instead of testing the union's type; type tests do not invoke implicit conversions.
/// <code>
/// if (body.Payload is { Any: JsonNode any }) { Console.WriteLine(any); }
/// else if (body.Payload is { Expression: ArazzoRuntimeExpression expression }) { Console.WriteLine(expression); }
/// else if (body.Payload is { Selector: ArazzoSelector selector }) { Console.WriteLine(selector.Selector); }
/// </code>
/// Structured payloads containing embedded expressions or selectors are represented by <see cref="Any"/>.
/// </remarks>
public class ArazzoRequestBodyPayloadUnion : IArazzoSerializable, IOpenApiElement
{
    /// <summary>Gets or sets the literal payload. Use JsonNullSentinel.JsonNull for an explicit null.</summary>
    public JsonNode? Any { get; set; }

    /// <summary>Gets or sets the runtime expression.</summary>
    public ArazzoRuntimeExpression? Expression { get; set; }

    /// <summary>Gets or sets the selector, available in Arazzo 1.1.</summary>
    public ArazzoSelector? Selector { get; set; }

    /// <summary>Converts a JSON value to a union.</summary>
    /// <param name="value">The payload, or null for an explicit JSON null.</param>
    public static implicit operator ArazzoRequestBodyPayloadUnion(JsonNode? value) => new() { Any = value ?? JsonNullSentinel.JsonNull };

    /// <summary>Converts a literal string to a union.</summary>
    /// <param name="value">The literal string, or null for an explicit JSON null.</param>
    public static implicit operator ArazzoRequestBodyPayloadUnion(string? value) => (JsonNode?)JsonValue.Create(value);

    /// <summary>Converts a runtime expression to a union.</summary>
    /// <param name="value">The expression.</param>
    public static implicit operator ArazzoRequestBodyPayloadUnion(ArazzoRuntimeExpression value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Expression = value };
    }

    /// <summary>Converts a selector to a union.</summary>
    /// <param name="value">The selector.</param>
    public static implicit operator ArazzoRequestBodyPayloadUnion(ArazzoSelector value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Selector = value };
    }

    /// <summary>Gets the active literal, throwing for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator JsonNode(ArazzoRequestBodyPayloadUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Any is { } any && value.Expression is null && value.Selector is null ? any : throw new InvalidCastException("The literal is not the only populated member.");
    }

    /// <summary>Gets the active expression, throwing for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoRuntimeExpression(ArazzoRequestBodyPayloadUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Expression is { } expression && value.Any is null && value.Selector is null ? expression : throw new InvalidCastException("The expression is not the only populated member.");
    }

    /// <summary>Gets the active selector, throwing for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoSelector(ArazzoRequestBodyPayloadUnion value)
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
        ArazzoRuntimeExpressionValidator.ValidateSerializationExpressionStrings(Any, $"{nameof(ArazzoRequestBody)}.{nameof(ArazzoRequestBody.Payload)}", specVersion);
        if (Selector is not null && specVersion is ArazzoSpecVersion.Arazzo1_0)
        {
            throw new ArazzoSerializationException("Selector request body payloads are not supported in Arazzo 1.0.");
        }

        var value = new ArazzoPayloadReplacementValueUnion { Any = Any, Expression = Expression, Selector = Selector };
        if (specVersion is ArazzoSpecVersion.Arazzo1_0)
        {
            value.SerializeAsV1(writer);
        }
        else
        {
            value.SerializeAsV1_1(writer);
        }
    }
}