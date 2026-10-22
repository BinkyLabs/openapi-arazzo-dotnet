using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>A literal JSON value, runtime expression, or selector. Exactly one member must be populated.</summary>
/// <remarks>
/// Inspect properties instead of testing the union's type; type tests do not invoke implicit conversions.
/// <code>
/// if (parameter.Value is { Any: JsonNode any }) { Console.WriteLine(any); }
/// else if (parameter.Value is { Expression: ArazzoRuntimeExpression expression }) { Console.WriteLine(expression); }
/// else if (parameter.Value is { Selector: ArazzoSelector selector }) { Console.WriteLine(selector.Selector); }
/// </code>
/// </remarks>
public class ArazzoParameterValueUnion : IArazzoSerializable, IOpenApiElement
{
    /// <summary>Gets or sets the literal value. Use JsonNullSentinel.JsonNull for an explicit null.</summary>
    public JsonNode? Any { get; set; }
    /// <summary>Gets or sets the runtime expression.</summary>
    public ArazzoRuntimeExpression? Expression { get; set; }
    /// <summary>Gets or sets the selector, available in Arazzo 1.1.</summary>
    public ArazzoSelector? Selector { get; set; }
    /// <summary>Converts a JSON value to a union.</summary>
    /// <param name="value">The value.</param>
    public static implicit operator ArazzoParameterValueUnion(JsonNode? value) => new() { Any = value ?? JsonNullSentinel.JsonNull };
    /// <summary>Converts a literal string to a union.</summary>
    /// <param name="value">The literal string.</param>
    public static implicit operator ArazzoParameterValueUnion(string? value) => (JsonNode?)JsonValue.Create(value);
    /// <summary>Converts a runtime expression to a union.</summary>
    /// <param name="value">The expression.</param>
    public static implicit operator ArazzoParameterValueUnion(ArazzoRuntimeExpression value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Expression = value };
    }
    /// <summary>Converts a selector to a union.</summary>
    /// <param name="value">The selector.</param>
    public static implicit operator ArazzoParameterValueUnion(ArazzoSelector value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Selector = value };
    }
    /// <summary>Gets the active literal, throwing for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator JsonNode(ArazzoParameterValueUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Any is { } any && value.Expression is null && value.Selector is null ? any : throw new InvalidCastException("The literal is not the only populated member.");
    }
    /// <summary>Gets the active expression, throwing for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoRuntimeExpression(ArazzoParameterValueUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Expression is { } expression && value.Any is null && value.Selector is null ? expression : throw new InvalidCastException("The expression is not the only populated member.");
    }
    /// <summary>Gets the active selector, throwing for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoSelector(ArazzoParameterValueUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Selector is { } selector && value.Any is null && value.Expression is null ? selector : throw new InvalidCastException("The selector is not the only populated member.");
    }
    private ArazzoPayloadReplacementValueUnion AsPayloadValue() => new() { Any = Any, Expression = Expression, Selector = Selector };
    internal JsonNode? ToJsonNode()
    {
        if (Any is not null && Expression is null && Selector is null) return Any;
        if (Expression is not null && Any is null && Selector is null) return JsonValue.Create(Expression.Expression);
        using var text = new StringWriter();
        AsPayloadValue().SerializeAsV1_1(new OpenApiJsonWriter(text));
        return JsonNode.Parse(text.ToString());
    }
    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer) => AsPayloadValue().SerializeAsV1(writer);
    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (Any is JsonObject)
            throw new ArazzoSerializationException("Arazzo 1.1 parameter objects must use the Selector member; literal objects are not supported by the parameter value schema.");
        AsPayloadValue().SerializeAsV1_1(writer);
    }
}