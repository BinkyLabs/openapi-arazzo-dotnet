using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the common properties and behavior for result actions.
/// </summary>
/// <typeparam name="T">The type of the action, constrained to enums.</typeparam>
public abstract class ArazzoResultAction<T> : IArazzoResultAction<T>, IArazzoExtensible where T : struct, Enum
{
    /// <inheritdoc/>
    public string? Name { get; set; }

    /// <inheritdoc/>
    public T? Type { get; set; }

    /// <inheritdoc/>
    public string? WorkflowId { get; set; }

    /// <inheritdoc/>
    public string? StepId { get; set; }

    /// <inheritdoc/>
    public IList<ArazzoCriterion>? Criteria { get; set; }

    /// <inheritdoc/>
    public IList<IArazzoParameter>? Parameters { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the common properties of the result action as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    protected void SerializeCommonPropertiesAsV1(IOpenApiWriter writer)
    {
        SerializeCommonPropertiesInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <summary>
    /// Serializes the common properties of the result action using the specified Arazzo version.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    /// <param name="specVersion">The Arazzo specification version to use for serialization.</param>
    /// <param name="callback">The callback to use for serializing nested Arazzo serializable objects.</param>
    private protected void SerializeCommonPropertiesInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(callback);

        ArgumentException.ThrowIfNullOrEmpty(Name);
        if (!Type.HasValue)
        {
            throw new ArgumentNullException(nameof(Type));
        }
        ArazzoResultActionValidator.ValidateSerialization(this);

        writer.WriteRequiredProperty(ArazzoConstants.ArazzoResultActionName, Name);
        writer.WriteRequiredProperty(ArazzoConstants.ArazzoResultActionType, Type.Value.GetDisplayName());

        if (!string.IsNullOrEmpty(WorkflowId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoResultActionWorkflowId, WorkflowId);
        }

        if (!string.IsNullOrEmpty(StepId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoResultActionStepId, StepId);
        }

        writer.WriteOptionalCollection(ArazzoConstants.ArazzoResultActionCriteria, Criteria, callback);

        writer.WriteOptionalCollection(GetVersionedFieldName(specVersion, ArazzoConstants.ArazzoResultActionParameters), Parameters, (w, p) =>
        {
            if (p is not null)
            {
                callback(w, p);
            }
        });
    }

    /// <summary>
    /// Serializes the result action as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public abstract void SerializeAsV1(IOpenApiWriter writer);

    /// <summary>
    /// Serializes the result action as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public abstract void SerializeAsV1_1(IOpenApiWriter writer);

    private static string GetVersionedFieldName(ArazzoSpecVersion specVersion, string fieldName) =>
        specVersion is ArazzoSpecVersion.Arazzo1_0 ? ArazzoConstants.GetArazzo1_0ExtensionName(fieldName) : fieldName;
}