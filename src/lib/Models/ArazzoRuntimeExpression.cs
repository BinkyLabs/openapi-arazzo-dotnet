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

    /// <summary>
    /// Initializes a runtime expression that is valid in at least one supported Arazzo specification version.
    /// Serialization validates the expression against the output specification version.
    /// </summary>
    /// <param name="expression">The runtime expression string.</param>
    /// <exception cref="ArgumentNullException">The expression is null.</exception>
    /// <exception cref="ArgumentException">The expression is empty.</exception>
    /// <exception cref="ArazzoException">The expression does not match the Arazzo runtime-expression grammar.</exception>
    public ArazzoRuntimeExpression(string expression)
    {
        ArgumentException.ThrowIfNullOrEmpty(expression);

        if (!ArazzoRuntimeExpressionValidator.IsRuntimeExpression(expression, ArazzoSpecVersion.Arazzo1_0) &&
            !ArazzoRuntimeExpressionValidator.IsRuntimeExpression(expression, ArazzoSpecVersion.Arazzo1_1))
        {
            throw new ArazzoException($"The runtime expression '{expression}' has invalid format.");
        }

        Expression = expression;
    }

    /// <inheritdoc/>
    public bool Equals(ArazzoRuntimeExpression? other) =>
        other is not null && Equals(other.Expression);

    /// <summary>
    /// Determines whether the specified expression string is equal to the current runtime expression.
    /// </summary>
    /// <param name="expression">The expression string to compare with the current runtime expression.</param>
    /// <returns><c>true</c> if the specified expression string is equal to the current runtime expression; otherwise, <c>false</c>.</returns>
    public bool Equals(string? expression) =>
        expression is not null && StringComparer.Ordinal.Equals(Expression, expression);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ArazzoRuntimeExpression other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Expression);

    /// <inheritdoc/>
    public override string ToString() => Expression;
}