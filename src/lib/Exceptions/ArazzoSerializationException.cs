// Licensed under the MIT license.

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Defines an exception indicating Arazzo serialization encountered an issue.
/// </summary>
[Serializable]
public class ArazzoSerializationException : ArazzoException
{
    /// <summary>
    /// Initializes the <see cref="ArazzoSerializationException"/> class.
    /// </summary>
    public ArazzoSerializationException() { }

    /// <summary>
    /// Initializes the <see cref="ArazzoSerializationException"/> class with a custom message.
    /// </summary>
    /// <param name="message">Plain text error message for this exception.</param>
    public ArazzoSerializationException(string message) : base(message) { }

    /// <summary>
    /// Initializes the <see cref="ArazzoSerializationException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="message">Plain text error message for this exception.</param>
    /// <param name="innerException">The inner exception.</param>
    public ArazzoSerializationException(string message, Exception innerException) : base(message, innerException) { }
}
