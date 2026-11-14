using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>A Parameter Object or a reusable parameter reference. Exactly one member must be populated.</summary>
/// <remarks>
/// Inspect the properties; type tests do not invoke implicit conversions.
/// <code>
/// if (value is { Parameter: ArazzoParameter parameter }) { Console.WriteLine(parameter.Name); }
/// else if (value is { Reference: ArazzoParameterReference reference }) { Console.WriteLine(reference.Reference.ReferenceV1); }
/// </code>
/// </remarks>
public class ArazzoParameterUnion : IArazzoParameter
{
    /// <summary>Gets or sets the inline parameter.</summary>
    public ArazzoParameter? Parameter { get; set; }
    /// <summary>Gets or sets the reusable parameter reference.</summary>
    public ArazzoParameterReference? Reference { get; set; }
    /// <inheritdoc/>
    public string? Name => GetParameter().Name;
    /// <inheritdoc/>
    public ParameterLocation? In => GetParameter().In;
    /// <inheritdoc/>
    public ArazzoParameterValueUnion? Value => GetParameter().Value;
    /// <summary>Converts a parameter to a union.</summary>
    /// <param name="value">The parameter.</param>
    public static implicit operator ArazzoParameterUnion(ArazzoParameter value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Parameter = value };
    }
    /// <summary>Converts a reference to a union.</summary>
    /// <param name="value">The reference.</param>
    public static implicit operator ArazzoParameterUnion(ArazzoParameterReference value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new() { Reference = value };
    }
    /// <summary>Gets the active inline parameter.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoParameter(ArazzoParameterUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Parameter is { } parameter && value.Reference is null ? parameter : throw new InvalidCastException("The parameter is not the only populated member.");
    }
    /// <summary>Gets the active reference.</summary>
    /// <param name="value">The union.</param>
    public static implicit operator ArazzoParameterReference(ArazzoParameterUnion value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Reference is { } reference && value.Parameter is null ? reference : throw new InvalidCastException("The reference is not the only populated member.");
    }
    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer) => GetParameter().SerializeAsV1(writer);
    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer) => GetParameter().SerializeAsV1_1(writer);

    internal IArazzoParameter GetParameter() => (Parameter, Reference) switch
    {
        ({ } parameter, null) => parameter,
        (null, { } reference) => reference,
        _ => throw new ArazzoSerializationException("ArazzoParameterUnion must have exactly one of Parameter or Reference.")
    };
}