using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a criterion object used in Arazzo workflows.
/// </summary>
public class ArazzoCriterion : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the context for the criterion.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Gets or sets the criterion expression type.
    /// Can be serialized as a string (for Simple or Regex types) or as an object (for JsonPath or XPath types).
    /// </summary>
    public ArazzoCriterionExpressionType? Type { get; set; }

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
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(Condition);

        writer.WriteStartObject();

        // Write context if provided
        if (!string.IsNullOrEmpty(Context))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoCriterionContext, Context);
        }

        // Handle type field - can be string or object depending on the type's sub-type
        if (Type != null)
        {
            if (Type.Type == ArazzoCriterionExpressionTypeType.Simple || Type.Type == ArazzoCriterionExpressionTypeType.Regex)
            {
                // For Simple and Regex types, serialize as a string
                if (Type.Version.HasValue)
                {
                    throw new ArazzoException($"Criterion expression type of '{Type.Type?.GetDisplayName()}' cannot have a version property.");
                }

                writer.WriteProperty(ArazzoConstants.ArazzoCriterionType, Type.Type.Value.GetDisplayName());
            }
            else
            {
                // For other types (JsonPath, XPath), serialize as an object
                writer.WriteOptionalObject(ArazzoConstants.ArazzoCriterionType, Type, static (w, o) => o.SerializeAsV1(w));
            }
        }

        writer.WriteProperty(ArazzoConstants.ArazzoCriterionCondition, Condition);
        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}
