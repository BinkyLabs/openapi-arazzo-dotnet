namespace BinkyLabs.OpenApi.Arazzo;

internal static class ArazzoUnionHelper
{
    internal static object? Unwrap(object? value) => value switch
    {
        ArazzoParameterUnion parameter => parameter.GetParameter(),
        ArazzoSuccessActionUnion action => action.GetAction(),
        ArazzoFailureActionUnion action => action.GetAction(),
        _ => value
    };
}