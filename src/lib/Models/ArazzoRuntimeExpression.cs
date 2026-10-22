using BinkyLabs.OpenApi.Arazzo.Validation;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents an immutable Arazzo runtime expression.
/// </summary>
public sealed class ArazzoRuntimeExpression : IEquatable<ArazzoRuntimeExpression>
{
    /// <summary>
    /// The dollar sign prefix for a runtime expression.
    /// </summary>
    public const string Prefix = "$";

    /// <summary>
    /// Gets the expression string.
    /// </summary>
    public string Expression { get; }

    private ArazzoRuntimeExpression(string expression)
    {
        Expression = expression;
    }

    /// <summary>
    /// Builds a runtime expression that is valid in at least one supported Arazzo specification version.
    /// Serialization validates the expression against the output specification version.
    /// </summary>
    /// <param name="expression">The runtime expression string.</param>
    /// <returns>The runtime expression.</returns>
    /// <exception cref="ArgumentNullException">The expression is null.</exception>
    /// <exception cref="ArgumentException">The expression is empty.</exception>
    /// <exception cref="ArazzoException">The expression does not match the Arazzo runtime-expression grammar.</exception>
    public static ArazzoRuntimeExpression Build(string expression)
    {
        ArgumentException.ThrowIfNullOrEmpty(expression);

        if (!ArazzoRuntimeExpressionValidator.IsRuntimeExpression(expression, ArazzoSpecVersion.Arazzo1_0) &&
            !ArazzoRuntimeExpressionValidator.IsRuntimeExpression(expression, ArazzoSpecVersion.Arazzo1_1))
        {
            throw new ArazzoException($"The runtime expression '{expression}' has invalid format.");
        }

        return new(expression);
    }

    /// <inheritdoc/>
    public bool Equals(ArazzoRuntimeExpression? other) =>
        other is not null && StringComparer.Ordinal.Equals(Expression, other.Expression);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ArazzoRuntimeExpression other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Expression);

    /// <inheritdoc/>
    public override string ToString() => Expression;
}