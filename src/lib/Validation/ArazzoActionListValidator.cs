using BinkyLabs.OpenApi.Arazzo.Reader;

using System.Linq;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static class ArazzoActionListValidator
{
    internal static void ValidateSerialization<T>(IEnumerable<T>? actions, string elementName) where T : IArazzoResultAction
    {
        var error = Validate(actions, elementName).FirstOrDefault();
        if (error is not null)
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
            return [];
        }

        var actionKeys = new HashSet<string>(StringComparer.Ordinal);
        return actions
                .Select(GetActionKey)
                .Where(key => !string.IsNullOrEmpty(key) && !actionKeys.Contains(key!))
                .Select(x => $"{elementName} contains duplicate action '{x}'.");
    }

    private static string? GetActionKey<T>(T action) where T : IArazzoResultAction =>
        action is IArazzoReferenceHolder<BaseArazzoReference> referenceHolder
            ? referenceHolder.Reference.ReferenceV1
            : action.Name;
}