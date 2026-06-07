namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a failure action definition.
/// </summary>
public interface IArazzoFailureAction : IArazzoResultAction<ArazzoFailureType>
{
    /// <summary>
    /// Gets or sets the retry after time in seconds.
    /// </summary>
    decimal? RetryAfter { get; }

    /// <summary>
    /// Gets or sets the retry limit.
    /// </summary>
    ulong? RetryLimit { get; }
}