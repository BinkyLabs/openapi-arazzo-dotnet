using BinkyLabs.OpenApi.Arazzo.Validation;
using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a criterion object used in Arazzo workflows.
/// </summary>
public class ArazzoCriterion : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the runtime expression that provides the criterion context.
    /// </summary>
    public ArazzoRuntimeExpression? Context { get; set; }

    /// <summary>
    /// Gets or sets the criterion expression type.
    /// Can be a type name or an Expression Type Object.
    /// </summary>
    public ArazzoCriterionTypeUnion? Type { get; set; }

    /// <summary>
    /// Gets or sets the condition expression.
    /// </summary>
    public string? Condition { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the criterion as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <summary>
    /// Serializes the criterion as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1, static (w, obj) => obj.SerializeAsV1_1(w));
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArazzoCriterionValidator.ValidateSerialization(this);
        ArazzoRuntimeExpressionValidator.ValidateSerializationExpression(Context?.Expression, $"{nameof(ArazzoCriterion)}.{nameof(Context)}", specVersion);

        writer.WriteStartObject();

        // Write context if provided
        if (Context is not null)
        {
            writer.WriteProperty(ArazzoConstants.ArazzoCriterionContext, Context.Expression);
        }

        writer.WriteOptionalObject(ArazzoConstants.ArazzoCriterionType, Type, callback);

        writer.WriteProperty(ArazzoConstants.ArazzoCriterionCondition, Condition);
        writer.WriteArazzoExtensions(Extensions, specVersion);
        writer.WriteEndObject();
    }
}