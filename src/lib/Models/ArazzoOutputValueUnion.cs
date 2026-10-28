using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>A runtime expression or selector output. Exactly one member must be populated.</summary>
/// <remarks>
/// Inspect the properties; type tests do not invoke implicit conversions.
/// <code>
/// if (output is { Expression: ArazzoRuntimeExpression expression }) { Console.WriteLine(expression); }
/// else if (output is { Selector: ArazzoSelector selector }) { Console.WriteLine(selector.Selector); }
/// </code>
/// </remarks>
public class ArazzoOutputValueUnion : IArazzoSerializable, IOpenApiElement
{
    /// <summary>Gets or sets the runtime expression.</summary>
    public ArazzoRuntimeExpression? Expression { get; set; }
    /// <summary>Gets or sets the selector, available in Arazzo 1.1.</summary>
    public ArazzoSelector? Selector { get; set; }
    /// <summary>Converts a runtime expression to a union.</summary>
    /// <param name="value">The expression.</param>
    public static implicit operator ArazzoOutputValueUnion(ArazzoRuntimeExpression value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Expression = value };
    }
    /// <summary>Converts an expression string to a union.</summary>
    /// <param name="value">The expression string.</param>
    public static implicit operator ArazzoOutputValueUnion(string value) => new ArazzoRuntimeExpression(value);
    /// <summary>Converts a selector to a union.</summary>
    /// <param name="value">The selector.</param>
    public static implicit operator ArazzoOutputValueUnion(ArazzoSelector value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Selector = value };
    }
    /// <summary>Gets the active expression or throws for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoRuntimeExpression(ArazzoOutputValueUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Expression is { } expression && value.Selector is null ? expression : throw new InvalidCastException("The expression is not the only populated member.");
    }
    /// <summary>Gets the active selector or throws for a different or conflicting member.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoSelector(ArazzoOutputValueUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Selector is { } selector && value.Expression is null ? selector : throw new InvalidCastException("The selector is not the only populated member.");
    }
    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer) => AsValue().SerializeAsV1(writer);
    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer) => AsValue().SerializeAsV1_1(writer);
    private ArazzoPayloadReplacementValueUnion AsValue() => new() { Expression = Expression, Selector = Selector };
}