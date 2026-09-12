using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the action to perform for an AsyncAPI step.
/// </summary>
public enum ArazzoStepAction
{
    /// <summary>
    /// Send a message.
    /// </summary>
    [Display("send")]
    Send,

    /// <summary>
    /// Receive a message.
    /// </summary>
    [Display("receive")]
    Receive
}