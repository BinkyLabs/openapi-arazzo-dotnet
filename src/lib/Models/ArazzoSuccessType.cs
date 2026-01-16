using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents the type of a success action.
/// </summary>
public enum ArazzoSuccessType
{
    /// <summary>
    /// End success action type.
    /// </summary>
    [Display("end")]
    End,

    /// <summary>
    /// Goto success action type.
    /// </summary>
    [Display("goto")]
    Goto
}