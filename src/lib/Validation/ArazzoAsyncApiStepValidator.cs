using System.Text.RegularExpressions;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static partial class ArazzoAsyncApiStepValidator
{
    [GeneratedRegex(@"^\{\$sourceDescriptions\.([^\.\}\s#]+)\.url\}#/channels/(?:[^~/]|~[01])+$", RegexOptions.CultureInvariant)]
    private static partial Regex ChannelPathRegex();

    internal static string? GetChannelSourceName(string? channelPath)
    {
        if (channelPath is null)
            return null;
        var match = ChannelPathRegex().Match(channelPath);
        return match.Success ? match.Groups[1].Value : null;
    }

    internal static void ValidateSerialization(ArazzoStep step)
    {
        if (Validate(step).FirstOrDefault() is string error)
            throw new ArazzoSerializationException(error);
    }

    internal static void ValidateDeserialization(ArazzoStep step, ParsingContext context)
    {
        foreach (var error in Validate(step))
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), error));
    }

    private static IEnumerable<string> Validate(ArazzoStep step)
    {
        var elementName = $"{nameof(ArazzoStep)} '{step.StepId}'";
        if (step.ChannelPath is not null)
        {
            if (GetChannelSourceName(step.ChannelPath) is null)
                yield return $"{elementName} channelPath '{step.ChannelPath}' must reference a sourceDescription URL runtime expression followed by a JSON Pointer to a channel.";
            if (!step.Action.HasValue)
                yield return $"{elementName} action is REQUIRED for AsyncAPI steps.";
        }

        var nonAsyncTarget = !string.IsNullOrEmpty(step.OperationPath) || !string.IsNullOrEmpty(step.WorkflowId);
        if (step.Action.HasValue && nonAsyncTarget)
            yield return $"{elementName} action can only be specified for AsyncAPI steps.";
        if (step.CorrelationId is not null && (step.Action is not ArazzoStepAction.Receive || nonAsyncTarget))
            yield return $"{elementName} correlationId can only be specified for AsyncAPI receive steps.";
    }
}