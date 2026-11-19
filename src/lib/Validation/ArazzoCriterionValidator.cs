using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoCriterionValidator
{
    internal static void ValidateSerialization(ArazzoCriterion criterion, ArazzoSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(criterion);

        if (string.IsNullOrEmpty(criterion.Condition))
        {
            throw new ArazzoSerializationException($"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Condition)} is required for ArazzoCriterion serialization.");
        }

        if (criterion.Type is not null && criterion.Context is null)
        {
            throw new ArazzoSerializationException($"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Context)} is required when {nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Type)} is specified.");
        }
        if (GetSyntaxError(criterion, specVersion) is { } error)
            throw new ArazzoSerializationException(error);
    }

    internal static void ValidateDeserialization(ArazzoCriterion criterion, ParsingContext context)
    {
        ArgumentNullException.ThrowIfNull(criterion);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(criterion.Condition))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Condition)} is required."));
        }

        if (criterion.Type is not null && criterion.Context is null)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Context)} is required when {nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Type)} is specified."));
        }
        if (GetSyntaxError(criterion, context.Diagnostic.SpecificationVersion) is { } error)
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), error));
    }

    private static string? GetSyntaxError(ArazzoCriterion criterion, ArazzoSpecVersion specVersion) =>
        ArazzoExpressionSyntaxValidator.GetError(criterion.Condition, criterion.Type?.Type, criterion.Type?.ExpressionType,
            $"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Condition)}", specVersion, allowRuntimeExpressions: true);
}