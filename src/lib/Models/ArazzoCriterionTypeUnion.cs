using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// A criterion type name or an Expression Type Object. Exactly one member must be populated.
/// </summary>
/// <remarks>
/// Test the populated property, since type tests do not invoke implicit conversions.
/// <code>
/// if (criterion.Type is { Type: ArazzoCriterionExpressionTypeType type }) { Console.WriteLine(type); }
/// else if (criterion.Type is { ExpressionType: ArazzoCriterionExpressionType expressionType }) { Console.WriteLine(expressionType.Version); }
/// </code>
/// </remarks>
public class ArazzoCriterionTypeUnion : IArazzoSerializable, IOpenApiElement
{
    /// <summary>Gets or sets the type name.</summary>
    public ArazzoCriterionExpressionTypeType? Type { get; set; }
    /// <summary>Gets or sets the Expression Type Object.</summary>
    public ArazzoCriterionExpressionType? ExpressionType { get; set; }
    /// <summary>Converts a type name to a union.</summary>
    /// <param name="value">The type name.</param>
    public static implicit operator ArazzoCriterionTypeUnion(ArazzoCriterionExpressionTypeType value) => new() { Type = value };
    /// <summary>Converts an Expression Type Object to a union.</summary>
    /// <param name="value">The expression type.</param>
    public static implicit operator ArazzoCriterionTypeUnion(ArazzoCriterionExpressionType value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { ExpressionType = value };
    }
    /// <summary>Gets the active type name, throwing if another or multiple members are populated.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoCriterionExpressionTypeType(ArazzoCriterionTypeUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Type is { } type && value.ExpressionType is null ? type : throw new InvalidCastException("The criterion type name is not the only populated member.");
    }
    /// <summary>Gets the active Expression Type Object, throwing if another or multiple members are populated.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoCriterionExpressionType(ArazzoCriterionTypeUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ExpressionType is { } type && !value.Type.HasValue ? type : throw new InvalidCastException("The Expression Type Object is not the only populated member.");
    }
    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer) => SerializeInternal(writer, static (w, o) => o.SerializeAsV1(w));
    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer) => SerializeInternal(writer, static (w, o) => o.SerializeAsV1_1(w));
    private void SerializeInternal(IOpenApiWriter writer, Action<IOpenApiWriter, ArazzoCriterionExpressionType> serialize)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (Type.HasValue == (ExpressionType is not null))
            throw new ArazzoSerializationException("ArazzoCriterionTypeUnion must have exactly one of Type or ExpressionType set.");
        if (Type is { } type)
        {
            if (type is not (ArazzoCriterionExpressionTypeType.Simple or ArazzoCriterionExpressionTypeType.Regex or ArazzoCriterionExpressionTypeType.JsonPath or ArazzoCriterionExpressionTypeType.XPath))
                throw new ArazzoSerializationException($"Unsupported criterion type name '{type}'.");
            writer.WriteValue(type.GetDisplayName());
        }
        else if (ExpressionType is { } expressionType)
            serialize(writer, expressionType);
    }
}