using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoResultActionValidator
{
    internal static void ValidateSerialization<T>(ArazzoResultAction<T> action) where T : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(action);

        if (Validate(action).FirstOrDefault() is string error)
        {
            throw new ArazzoSerializationException(error);
        }
    }

    internal static void ValidateDeserialization<T>(ArazzoResultAction<T> action, ParsingContext context) where T : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(context);

        AddRequiredFieldErrorIfMissing(action.Name, action.GetType().Name, nameof(IArazzoResultAction.Name), context);
        AddRequiredFieldErrorIfMissing(action.Type, action.GetType().Name, nameof(IArazzoResultAction<ArazzoSuccessType>.Type), context);

        foreach (var error in Validate(action))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), error));
        }
    }

    private static void AddRequiredFieldErrorIfMissing(object? value, string elementName, string fieldName, ParsingContext context)
    {
        if (value is null || value is string stringValue && string.IsNullOrEmpty(stringValue))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), $"{elementName}.{fieldName} is a REQUIRED field."));
        }
    }

    private static IEnumerable<string> Validate<T>(ArazzoResultAction<T> action) where T : struct, Enum
    {
        var workflowIdSpecified = !string.IsNullOrEmpty(action.WorkflowId);
        var stepIdSpecified = !string.IsNullOrEmpty(action.StepId);
        if (workflowIdSpecified && stepIdSpecified)
        {
            yield return $"{action.GetType().Name} '{action.Name}' can define only one of workflowId or stepId.";
            yield break;
        }

        if (!action.Type.HasValue)
        {
            yield break;
        }

        var targetCount = (workflowIdSpecified ? 1 : 0) + (stepIdSpecified ? 1 : 0);
        var actionType = action.Type.Value.GetDisplayName();
        if (string.Equals(actionType, "end", StringComparison.Ordinal) && targetCount > 0)
        {
            yield return $"{action.GetType().Name} '{action.Name}' type=end must not define workflowId or stepId.";
        }
        else if (string.Equals(actionType, "goto", StringComparison.Ordinal) && targetCount == 0)
        {
            yield return $"{action.GetType().Name} '{action.Name}' type=goto must define exactly one of workflowId or stepId.";
        }
    }
}