using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a criterion expression type definition.
/// </summary>
public class ArazzoCriterionExpressionType : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the type of the criterion expression (jsonpath or xpath).
    /// </summary>
    public ArazzoCriterionExpressionTypeType? Type { get; set; }

    /// <summary>
    /// Gets or sets the version of the criterion expression.
    /// </summary>
    public ArazzoCriterionExpressionVersion? Version { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the criterion expression type as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <summary>
    /// Serializes the criterion expression type as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1, static (w, obj) => obj.SerializeAsV1_1(w));
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (!Type.HasValue)
        {
            throw new ArgumentNullException(nameof(Type));
        }

        var version = Version;
        if (!version.HasValue)
        {
            if (specVersion is ArazzoSpecVersion.Arazzo1_1)
            {
                version = GetDefaultVersion(Type.Value);
            }
            else
            {
                throw new ArgumentNullException(nameof(Version));
            }
        }

        // Validate that Simple and Regex types are not serialized as they are not supported by the specification
        if (Type.Value == ArazzoCriterionExpressionTypeType.Simple || Type.Value == ArazzoCriterionExpressionTypeType.Regex)
        {
            throw new ArazzoException($"Serializing criterion expression type '{Type.Value.GetDisplayName()}' as an object is NOT supported by the specification.");
        }

        if (Type.Value is ArazzoCriterionExpressionTypeType.JsonPointer)
        {
            ArazzoVersionCompatibility.ThrowIfUnsupportedInV1(specVersion, ArazzoConstants.ArazzoCriterionExpressionTypeType, Type.Value);
        }

        if (version is ArazzoCriterionExpressionVersion.Rfc9535 or ArazzoCriterionExpressionVersion.XPath31 or ArazzoCriterionExpressionVersion.Rfc6901)
        {
            ArazzoVersionCompatibility.ThrowIfUnsupportedInV1(specVersion, ArazzoConstants.ArazzoCriterionExpressionTypeVersion, version.Value);
        }

        writer.WriteStartObject();
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoCriterionExpressionTypeType, Type.Value.GetDisplayName());
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoCriterionExpressionTypeVersion, version.Value.GetDisplayName());
        writer.WriteArazzoExtensions(Extensions, specVersion);
        writer.WriteEndObject();
    }

    internal static ArazzoCriterionExpressionVersion GetDefaultVersion(ArazzoCriterionExpressionTypeType type) =>
        type switch
        {
            ArazzoCriterionExpressionTypeType.JsonPath => ArazzoCriterionExpressionVersion.Rfc9535,
            ArazzoCriterionExpressionTypeType.XPath => ArazzoCriterionExpressionVersion.XPath31,
            ArazzoCriterionExpressionTypeType.JsonPointer => ArazzoCriterionExpressionVersion.Rfc6901,
            _ => throw new ArazzoException($"Expression type '{type.GetDisplayName()}' does not have a default object version.")
        };
}