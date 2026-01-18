using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a failure action definition.
/// </summary>
public class ArazzoFailureAction : ArazzoResultAction<ArazzoFailureType>, IArazzoFailureAction
{
    /// <inheritdoc/>
    public decimal? RetryAfter { get; set; }

    /// <inheritdoc/>
    public ulong? RetryLimit { get; set; }

    /// <summary>
    /// Serializes the failure action as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public override void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();

        SerializeCommonPropertiesAsV1(writer);

        if (RetryAfter.HasValue)
        {
            writer.WriteProperty(ArazzoConstants.ArazzoFailureActionRetryAfter, RetryAfter.Value);
        }
        if (RetryLimit.HasValue)
        {
            writer.WriteProperty(ArazzoConstants.ArazzoFailureActionRetryLimit, (long)RetryLimit.Value);
        }

        writer.WriteArazzoExtensions(Extensions, ArazzoSpecVersion.Arazzo1_0);
        writer.WriteEndObject();
    }
}