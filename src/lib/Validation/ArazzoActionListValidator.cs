using BinkyLabs.OpenApi.Arazzo.Reader;

using System.Linq;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoActionListValidator
{
    internal static void ValidateSerialization<T>(IEnumerable<T>? actions, string elementName) where T : IArazzoResultAction
    {
        foreach (var error in Validate(actions, elementName))
        {
            throw new ArazzoSerializationException(error);
        }
    }

    internal static void ValidateDeserialization<T>(IEnumerable<T>? actions, ParsingContext context, string elementName) where T : IArazzoResultAction
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var error in Validate(actions, elementName))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), error));
        }
    }

    private static IEnumerable<string> Validate<T>(IEnumerable<T>? actions, string elementName) where T : IArazzoResultAction
    {
        if (actions is null)
        {
            yield break;
        }

        var actionKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var actionKey in actions.Select(GetActionKey))
        {
            if (string.IsNullOrEmpty(actionKey))
            {
                continue;
            }

            if (!actionKeys.Add(actionKey))
            {
                yield return $"{elementName} contains duplicate action '{actionKey}'.";
            }
        }
    }

    private static string? GetActionKey(IArazzoResultAction action) =>
        action is IArazzoReferenceHolder<BaseArazzoReference> referenceHolder
            ? referenceHolder.Reference.ReferenceV1
            : action.Name;
}