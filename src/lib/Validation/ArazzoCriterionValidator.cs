using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoCriterionValidator
{
    internal static void ValidateSerialization(ArazzoCriterion criterion)
    {
        ArgumentNullException.ThrowIfNull(criterion);

        if (string.IsNullOrEmpty(criterion.Condition))
        {
            throw new ArazzoSerializationException($"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Condition)} is required for ArazzoCriterion serialization.");
        }

        if (criterion.Type is not null && string.IsNullOrEmpty(criterion.Context))
        {
            throw new ArazzoSerializationException($"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Context)} is required when {nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Type)} is specified.");
        }
    }

    internal static void ValidateDeserialization(ArazzoCriterion criterion, ParsingContext context)
    {
        ArgumentNullException.ThrowIfNull(criterion);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(criterion.Condition))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Condition)} is required."));
        }

        if (criterion.Type is not null && string.IsNullOrEmpty(criterion.Context))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Context)} is required when {nameof(ArazzoCriterion)}.{nameof(ArazzoCriterion.Type)} is specified."));
        }
    }
}