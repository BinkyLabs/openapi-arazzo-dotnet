using BinkyLabs.OpenApi.Arazzo.Validation;
using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a reusable components definition.
/// </summary>
public class ArazzoComponent : IArazzoSerializable, IArazzoExtensible
{
    /// <summary>
    /// Gets or sets the parameters dictionary.
    /// </summary>
    public IDictionary<string, ArazzoParameter>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the success actions dictionary.
    /// </summary>
    public IDictionary<string, ArazzoSuccessAction>? SuccessActions { get; set; }

    /// <summary>
    /// Gets or sets the failure actions dictionary.
    /// </summary>
    public IDictionary<string, ArazzoFailureAction>? FailureActions { get; set; }

    /// <summary>
    /// Gets or sets the inputs dictionary.
    /// </summary>
    public IDictionary<string, IArazzoInput>? Inputs { get; set; }

    /// <summary>
    /// Gets or sets the extensions dictionary.
    /// </summary>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the reusable components as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <summary>
    /// Serializes the reusable components as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1, static (w, obj) => obj.SerializeAsV1_1(w));
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArazzoKeyValidator.ValidateSerializationKeys(Parameters?.Keys, $"{nameof(ArazzoComponent)}.{nameof(Parameters)}");
        ArazzoKeyValidator.ValidateSerializationKeys(SuccessActions?.Keys, $"{nameof(ArazzoComponent)}.{nameof(SuccessActions)}");
        ArazzoKeyValidator.ValidateSerializationKeys(FailureActions?.Keys, $"{nameof(ArazzoComponent)}.{nameof(FailureActions)}");
        ArazzoKeyValidator.ValidateSerializationKeys(Inputs?.Keys, $"{nameof(ArazzoComponent)}.{nameof(Inputs)}");

        writer.WriteStartObject();

        // Write parameters
        writer.WriteOptionalMap(ArazzoConstants.ArazzoComponentParameters, Parameters, callback);

        // Write success actions
        writer.WriteOptionalMap(ArazzoConstants.ArazzoComponentSuccessActions, SuccessActions, callback);

        // Write failure actions
        writer.WriteOptionalMap(ArazzoConstants.ArazzoComponentFailureActions, FailureActions, callback);

        // Write inputs
        writer.WriteOptionalMap(ArazzoConstants.ArazzoComponentInputs, Inputs, callback);

        writer.WriteArazzoExtensions(Extensions, specVersion);
        writer.WriteEndObject();
    }
}