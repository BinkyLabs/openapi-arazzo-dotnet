using BinkyLabs.OpenApi.Arazzo.Validation;
using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a failure action definition.
/// </summary>
public class ArazzoFailureAction : ArazzoResultAction<ArazzoFailureType>, IArazzoFailureAction
{
    private ulong _retryLimit = ArazzoConstants.DefaultFailureActionRetryLimit;

    /// <inheritdoc/>
    public decimal? RetryAfter { get; set; }

    /// <inheritdoc/>
    public ulong RetryLimit
    {
        get => _retryLimit;
        set
        {
            _retryLimit = value;
            HasExplicitRetryLimit = true;
        }
    }

    internal bool HasExplicitRetryLimit { get; private set; }

    /// <summary>
    /// Serializes the failure action as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public override void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <summary>
    /// Serializes the failure action as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public override void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1, static (w, obj) => obj.SerializeAsV1_1(w));
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArazzoFailureActionValidator.ValidateSerialization(this);

        writer.WriteStartObject();

        SerializeCommonPropertiesInternal(writer, specVersion, callback);

        if (RetryAfter.HasValue)
        {
            writer.WriteProperty(ArazzoConstants.ArazzoFailureActionRetryAfter, RetryAfter.Value);
        }
        if (HasExplicitRetryLimit)
        {
            writer.WriteProperty(ArazzoConstants.ArazzoFailureActionRetryLimit, (long)RetryLimit);
        }

        writer.WriteArazzoExtensions(Extensions, specVersion);
        writer.WriteEndObject();
    }
}