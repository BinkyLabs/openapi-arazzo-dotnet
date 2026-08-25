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
        SerializeInternal(writer);
    }

    /// <summary>
    /// Serializes the failure action as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public override void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer);
    }

    private void SerializeInternal(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArazzoFailureActionValidator.ValidateSerialization(this);

        writer.WriteStartObject();

        SerializeCommonPropertiesAsV1(writer);

        if (RetryAfter.HasValue)
        {
            writer.WriteProperty(ArazzoConstants.ArazzoFailureActionRetryAfter, RetryAfter.Value);
        }
        if (HasExplicitRetryLimit)
        {
            writer.WriteProperty(ArazzoConstants.ArazzoFailureActionRetryLimit, (long)RetryLimit);
        }

        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}