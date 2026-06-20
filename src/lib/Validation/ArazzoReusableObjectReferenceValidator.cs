using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoReusableObjectReferenceValidator
{
    private static readonly HashSet<ReferenceType> ReusableReferenceTypes =
    [
        ReferenceType.Parameter,
        ReferenceType.SuccessAction,
        ReferenceType.FailureAction
    ];

    internal static bool IsReusableObjectReference(string? reference, ReferenceType? referenceType = null)
    {
        if (!ArazzoRuntimeExpressionValidator.IsRuntimeExpression(reference) ||
            string.IsNullOrEmpty(reference) ||
            !reference.StartsWith("$components.", StringComparison.Ordinal))
        {
            return false;
        }

        return referenceType is null
            ? ReusableReferenceTypes.Any(type => IsReusableObjectReference(reference, type))
            : MatchesReferenceType(reference, referenceType.Value);
    }

    internal static void ValidateSerializationReference(string? reference, ReferenceType? referenceType, string elementName)
    {
        if (!IsReusableObjectReference(reference, referenceType))
        {
            throw new ArazzoSerializationException(GetErrorMessage(reference, referenceType, elementName));
        }
    }

    internal static void ValidateDeserializationReference(string? reference, ParsingContext context, ReferenceType? referenceType, string elementName)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!IsReusableObjectReference(reference, referenceType))
        {
            context.Diagnostic.Errors.Add(new OpenApiError($"{context.GetLocation()}/{ArazzoConstants.ArazzoReusableObjectReference}", GetErrorMessage(reference, referenceType, elementName)));
        }
    }

    private static bool MatchesReferenceType(string reference, ReferenceType referenceType)
    {
        if (!ReusableReferenceTypes.Contains(referenceType))
        {
            return false;
        }

        var prefix = $"$components.{referenceType.GetDisplayName()}.";
        return reference.StartsWith(prefix, StringComparison.Ordinal) && reference.Length > prefix.Length;
    }

    private static string GetErrorMessage(string? reference, ReferenceType? referenceType, string elementName)
    {
        var expectedReference = referenceType is null
            ? "$components.parameters.<name>, $components.successActions.<name>, or $components.failureActions.<name>"
            : $"$components.{referenceType.Value.GetDisplayName()}.<name>";

        return $"{elementName} reference must be a valid runtime expression that targets {expectedReference} in the current Arazzo document. Invalid reference: '{reference}'.";
    }
}