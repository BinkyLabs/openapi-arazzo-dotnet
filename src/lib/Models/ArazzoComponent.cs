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
    public IDictionary<string, OpenApiSchema>? Inputs { get; set; }

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
        if (Parameters != null && Parameters.Count > 0)
        {
            writer.WritePropertyName(ArazzoConstants.ArazzoComponentParameters);
            writer.WriteStartObject();
            foreach (var parameter in Parameters)
            {
                writer.WritePropertyName(parameter.Key);
                parameter.Value.SerializeAsV1(writer);
            }
            writer.WriteEndObject();
        }
        
        // Write success actions
        if (SuccessActions != null && SuccessActions.Count > 0)
        {
            writer.WritePropertyName(ArazzoConstants.ArazzoComponentSuccessActions);
            writer.WriteStartObject();
            foreach (var action in SuccessActions)
            {
                writer.WritePropertyName(action.Key);
                action.Value.SerializeAsV1(writer);
            }
            writer.WriteEndObject();
        }
        
        // Write failure actions
        if (FailureActions != null && FailureActions.Count > 0)
        {
            writer.WritePropertyName(ArazzoConstants.ArazzoComponentFailureActions);
            writer.WriteStartObject();
            foreach (var action in FailureActions)
            {
                writer.WritePropertyName(action.Key);
                action.Value.SerializeAsV1(writer);
            }
            writer.WriteEndObject();
        }
        
        // Write inputs
        if (Inputs != null && Inputs.Count > 0)
        {
            writer.WriteOptionalMap(ArazzoConstants.ArazzoComponentInputs, Inputs, (w, s) => s.SerializeAsV32(w));
        }
        
        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}