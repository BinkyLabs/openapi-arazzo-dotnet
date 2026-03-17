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
    public IDictionary<string, IOpenApiSchema>? Inputs { get; set; }

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
        ArgumentNullException.ThrowIfNull(writer);

        // TODO: Implement validation during serialization/deserialization that any of the keys 
        // of Parameters, SuccessActions, FailureActions, and Inputs dictionaries must match 
        // the following regex: ^[a-zA-Z0-9\.\-_]+$

        writer.WriteStartObject();

        // Write parameters
        writer.WriteOptionalMap(ArazzoConstants.ArazzoComponentParameters, Parameters, static (w, p) => p.SerializeAsV1(w));

        // Write success actions
        writer.WriteOptionalMap(ArazzoConstants.ArazzoComponentSuccessActions, SuccessActions, static (w, a) => a.SerializeAsV1(w));

        // Write failure actions
        writer.WriteOptionalMap(ArazzoConstants.ArazzoComponentFailureActions, FailureActions, static (w, a) => a.SerializeAsV1(w));

        // Write inputs
        writer.WriteOptionalMap(ArazzoConstants.ArazzoComponentInputs, Inputs, (w, s) => s.SerializeAsV32(w));

        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}