using System.Linq;
using System.Text.RegularExpressions;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static partial class ArazzoSemanticReferenceValidator
{
    [GeneratedRegex(@"\$sourceDescriptions\.([^.\}\s#]+)\.url", RegexOptions.CultureInvariant)]
    private static partial Regex SourceDescriptionUrlExpressionRegex();

    internal static void ValidateSerialization(ArazzoDocument document)
    {
        foreach (var error in Validate(document))
        {
            throw new ArazzoSerializationException(error);
        }
    }

    internal static void ValidateDeserialization(ArazzoDocument document, ParsingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var error in Validate(document))
        {
            context.Diagnostic.Errors.Add(new OpenApiError(string.Empty, error));
        }
    }

    private static IEnumerable<string> Validate(ArazzoDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.RegisterComponents();

        var workflowIds = new HashSet<string>(
            document.Workflows?
                .Select(static workflow => workflow.WorkflowId)
                .OfType<string>() ?? [],
            StringComparer.Ordinal);
        var sourceDescriptionNames = new HashSet<string>(
            document.SourceDescriptions?
                .Select(static sourceDescription => sourceDescription.Name)
                .OfType<string>() ?? [],
            StringComparer.Ordinal);

        foreach (var workflow in document.Workflows ?? [])
        {
            var stepIds = new HashSet<string>(
                workflow.Steps?
                    .Select(static step => step.StepId)
                    .OfType<string>() ?? [],
                StringComparer.Ordinal);

            foreach (var step in workflow.Steps ?? [])
            {
                foreach (var error in ValidateWorkflowReference(step.WorkflowId, workflowIds, sourceDescriptionNames, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}'"))
                {
                    yield return error;
                }

                foreach (var error in ValidateOperationPathSourceDescriptions(step.OperationPath, sourceDescriptionNames, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}'"))
                {
                    yield return error;
                }

                foreach (var error in ValidateReusableReferences(step.Parameters, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}' parameter", document))
                {
                    yield return error;
                }

                foreach (var error in ValidateActions(step.OnSuccess, workflowIds, sourceDescriptionNames, stepIds, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}' success action", document))
                {
                    yield return error;
                }

                foreach (var error in ValidateActions(step.OnFailure, workflowIds, sourceDescriptionNames, stepIds, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}' failure action", document))
                {
                    yield return error;
                }
            }

            foreach (var error in ValidateReusableReferences(workflow.Parameters, $"Workflow '{workflow.WorkflowId}' parameter", document))
            {
                yield return error;
            }

            foreach (var error in ValidateActions(workflow.SuccessActions, workflowIds, sourceDescriptionNames, stepIds, $"Workflow '{workflow.WorkflowId}' success action", document))
            {
                yield return error;
            }

            foreach (var error in ValidateActions(workflow.FailureActions, workflowIds, sourceDescriptionNames, stepIds, $"Workflow '{workflow.WorkflowId}' failure action", document))
            {
                yield return error;
            }
        }
    }

    private static IEnumerable<string> ValidateActions<T>(
        IEnumerable<T>? actions,
        ISet<string> workflowIds,
        ISet<string> sourceDescriptionNames,
        ISet<string> stepIds,
        string elementName,
        ArazzoDocument document) where T : IArazzoResultAction
    {
        foreach (var error in ValidateReusableReferences(actions, elementName, document))
        {
            yield return error;
        }

        foreach (var action in actions ?? [])
        {
            foreach (var error in ValidateWorkflowReference(action.WorkflowId, workflowIds, sourceDescriptionNames, $"{elementName} '{action.Name}'"))
            {
                yield return error;
            }

            if (!string.IsNullOrEmpty(action.StepId) && !stepIds.Contains(action.StepId))
            {
                yield return $"{elementName} '{action.Name}' references unknown stepId '{action.StepId}'.";
            }
        }
    }

    private static IEnumerable<string> ValidateWorkflowReference(string? workflowId, ISet<string> workflowIds, ISet<string> sourceDescriptionNames, string elementName)
    {
        if (string.IsNullOrEmpty(workflowId))
        {
            yield break;
        }

        if (workflowId.StartsWith("$sourceDescriptions.", StringComparison.Ordinal))
        {
            foreach (var error in ValidateSourceDescriptionExpressions(workflowId, sourceDescriptionNames, elementName))
            {
                yield return error;
            }
            yield break;
        }

        if (workflowId.StartsWith("$", StringComparison.Ordinal))
        {
            yield break;
        }

        if (!workflowIds.Contains(workflowId))
        {
            yield return $"{elementName} references unknown workflowId '{workflowId}'.";
        }
    }

    private static IEnumerable<string> ValidateOperationPathSourceDescriptions(string? operationPath, ISet<string> sourceDescriptionNames, string elementName)
    {
        if (string.IsNullOrEmpty(operationPath))
        {
            yield break;
        }

        foreach (var error in ValidateSourceDescriptionExpressions(operationPath, sourceDescriptionNames, elementName))
        {
            yield return error;
        }
    }

    private static IEnumerable<string> ValidateSourceDescriptionExpressions(string value, ISet<string> sourceDescriptionNames, string elementName)
    {
        foreach (Match match in SourceDescriptionUrlExpressionRegex().Matches(value))
        {
            var sourceDescriptionName = match.Groups[1].Value;
            if (!sourceDescriptionNames.Contains(sourceDescriptionName))
            {
                yield return $"{elementName} references unknown sourceDescription '{sourceDescriptionName}'.";
            }
        }
    }

    private static IEnumerable<string> ValidateReusableReferences<T>(IEnumerable<T>? items, string elementName, ArazzoDocument document)
    {
        foreach (var referenceHolder in (items ?? []).OfType<IArazzoReferenceHolder<BaseArazzoReference>>())
        {
            referenceHolder.Reference.EnsureHostDocumentIsSet(document);
            if (referenceHolder.UnresolvedReference && !DoesComponentReferenceResolve(document, referenceHolder.Reference))
            {
                yield return $"{elementName} reference '{referenceHolder.Reference.ReferenceV1}' does not resolve to a component in the current Arazzo document.";
            }
        }
    }

    private static bool DoesComponentReferenceResolve(ArazzoDocument document, BaseArazzoReference reference)
    {
        var referenceValue = reference.ReferenceV1;
        if (string.IsNullOrEmpty(referenceValue) || !referenceValue.StartsWith("$components.", StringComparison.Ordinal))
        {
            return false;
        }

        var componentPath = referenceValue["$components.".Length..];
        var separatorIndex = componentPath.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex >= componentPath.Length - 1)
        {
            return false;
        }

        var componentMap = componentPath[..separatorIndex];
        var componentKey = componentPath[(separatorIndex + 1)..];

        return componentMap switch
        {
            "parameters" => document.Components?.Parameters?.ContainsKey(componentKey) == true,
            "successActions" => document.Components?.SuccessActions?.ContainsKey(componentKey) == true,
            "failureActions" => document.Components?.FailureActions?.ContainsKey(componentKey) == true,
            _ => false
        };
    }
}