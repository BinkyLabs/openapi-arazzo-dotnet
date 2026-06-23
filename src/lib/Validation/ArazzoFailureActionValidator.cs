using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoFailureActionValidator
{
    internal static void ValidateSerialization(ArazzoFailureAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (action.RetryAfter < 0)
        {
            throw new ArazzoSerializationException($"{nameof(ArazzoFailureAction)} '{action.Name}' retryAfter must be a non-negative decimal.");
        }

        if (action.Type != ArazzoFailureType.Retry)
        {
            if (action.RetryAfter.HasValue)
            {
                throw new ArazzoSerializationException($"{nameof(ArazzoFailureAction)} '{action.Name}' retryAfter can only be specified when type is retry.");
            }

            if (action.HasExplicitRetryLimit)
            {
                throw new ArazzoSerializationException($"{nameof(ArazzoFailureAction)} '{action.Name}' retryLimit can only be specified when type is retry.");
            }
        }
    }

    internal static void ValidateDeserialization(ArazzoFailureAction action, ParsingContext context)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(context);

        ArazzoResultActionValidator.ValidateDeserialization(action, context);

        if (action.RetryAfter < 0)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoFailureAction)} '{action.Name}' retryAfter must be a non-negative decimal."));
        }

        if (action.Type != ArazzoFailureType.Retry)
        {
            if (action.RetryAfter.HasValue)
            {
                context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoFailureAction)} '{action.Name}' retryAfter can only be specified when type is retry."));
            }

            if (action.HasExplicitRetryLimit)
            {
                context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoFailureAction)} '{action.Name}' retryLimit can only be specified when type is retry."));
            }
        }
    }
}