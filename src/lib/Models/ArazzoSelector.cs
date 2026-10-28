using BinkyLabs.OpenApi.Arazzo.Validation;
using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a selector used to extract data from structured values.
/// </summary>
public class ArazzoSelector : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the runtime expression that provides the selector context.
    /// </summary>
    public ArazzoRuntimeExpression? Context { get; set; }

    /// <summary>
    /// Gets or sets the selector expression.
    /// </summary>
    public string? Selector { get; set; }

    /// <summary>
    /// Gets or sets the selector expression type as a string or Expression Type Object.
    /// </summary>
    public ArazzoSelectorTypeUnion? Type { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <inheritdoc/>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0);
    }

    /// <inheritdoc/>
    public void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1);
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (specVersion is ArazzoSpecVersion.Arazzo1_0)
            throw new ArazzoSerializationException("Selector objects are not supported in Arazzo 1.0.");
        ArgumentNullException.ThrowIfNull(Context);
        ArgumentException.ThrowIfNullOrEmpty(Selector);
        ArgumentNullException.ThrowIfNull(Type);
        ArazzoRuntimeExpressionValidator.ValidateSerializationExpression(Context.Expression, $"{nameof(ArazzoSelector)}.{nameof(Context)}", specVersion);

        writer.WriteStartObject();
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoSelectorContext, Context.Expression);
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoSelectorSelector, Selector);
        writer.WriteOptionalObject(ArazzoConstants.ArazzoSelectorType, Type, static (w, v) => v.SerializeAsV1_1(w));
        writer.WriteArazzoExtensions(Extensions, specVersion);
        writer.WriteEndObject();
    }
}