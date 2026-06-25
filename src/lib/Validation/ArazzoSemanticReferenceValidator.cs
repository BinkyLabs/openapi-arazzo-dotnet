using System.Linq;
using System.Text.RegularExpressions;

using BinkyLabs.OpenApi.Arazzo.Reader;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static partial class ArazzoSemanticReferenceValidator
{
    [GeneratedRegex(@"\$sourceDescriptions\.([^\.\}\s#]+)\.url", RegexOptions.CultureInvariant)]
    private static partial Regex SourceDescriptionUrlExpressionRegex();

    [GeneratedRegex(@"^\{\$sourceDescriptions\.([^\.\}\s#]+)\.url\}(#/paths/(?:[^~/]|~[01])+/(?:get|put|post|delete|options|head|patch|trace|query))$", RegexOptions.CultureInvariant)]
    private static partial Regex OperationPathRegex();

    [GeneratedRegex(@"^\$sourceDescriptions\.([^\.\s#]+)\.(.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceDescriptionOperationIdRegex();

    [GeneratedRegex(@"^\$sourceDescriptions\.([^.\s#]+)\.[^.\s#]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceDescriptionWorkflowExpressionRegex();

    internal static void ValidateSerialization(ArazzoDocument document)
    {
        if (Validate(document).FirstOrDefault() is string error)
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

    internal static void ValidateOperationPathSerialization(string? operationPath, string elementName)
    {
        if (GetOperationPathSyntaxError(operationPath, elementName) is string error)
        {
            throw new ArazzoSerializationException(error);
        }
    }

    internal static void ValidateOperationPathDeserialization(string? operationPath, ParsingContext context, string elementName)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (GetOperationPathSyntaxError(operationPath, elementName) is string error)
        {
            context.Diagnostic.Errors.Add(new OpenApiError(context.GetLocation(), error));
        }
    }

    internal static IEnumerable<string> ValidateLoadedOperationReferences(ArazzoDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.RegisterComponents();

        foreach (var workflow in document.Workflows ?? [])
        {
            foreach (var step in workflow.Steps ?? [])
            {
                foreach (var error in ValidateOperationReferenceResolution(step, document, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}'"))
                {
                    yield return error;
                }
            }
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
        var nonArazzoSourceDescriptionCount = document.SourceDescriptions?
            .Count(static sourceDescription => sourceDescription.Type is not ArazzoDescriptionType.Arazzo) ?? 0;

        foreach (var workflow in document.Workflows ?? [])
        {
            foreach (var error in ValidateDependsOnReferences(workflow.DependsOn, workflowIds, sourceDescriptionNames, $"Workflow '{workflow.WorkflowId}'"))
            {
                yield return error;
            }

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

                foreach (var error in ValidateOperationIdSourceDescriptions(step.OperationId, sourceDescriptionNames, nonArazzoSourceDescriptionCount, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}'"))
                {
                    yield return error;
                }

                foreach (var error in ValidateOperationReferenceResolution(step, document, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}'"))
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

    private static IEnumerable<string> ValidateDependsOnReferences(
        IEnumerable<string>? dependsOn,
        ISet<string> workflowIds,
        ISet<string> sourceDescriptionNames,
        string elementName)
    {
        foreach (var dependency in dependsOn ?? [])
        {
            if (string.IsNullOrEmpty(dependency))
            {
                continue;
            }

            if (dependency.StartsWith("$", StringComparison.Ordinal))
            {
                if (!ArazzoRuntimeExpressionValidator.IsRuntimeExpression(dependency))
                {
                    yield return $"{elementName} dependsOn value '{dependency}' must be a valid runtime expression.";
                    continue;
                }

                var match = SourceDescriptionWorkflowExpressionRegex().Match(dependency);
                if (!match.Success)
                {
                    yield return $"{elementName} dependsOn value '{dependency}' must reference an external workflow using '$sourceDescriptions.<name>.<workflowId>'.";
                    continue;
                }

                var sourceDescriptionName = match.Groups[1].Value;
                if (!sourceDescriptionNames.Contains(sourceDescriptionName))
                {
                    yield return $"{elementName} dependsOn value '{dependency}' references unknown sourceDescription '{sourceDescriptionName}'.";
                }

                continue;
            }

            if (!workflowIds.Contains(dependency))
            {
                yield return $"{elementName} dependsOn references unknown workflowId '{dependency}'.";
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
            var match = SourceDescriptionWorkflowExpressionRegex().Match(workflowId);
            if (!match.Success)
            {
                yield return $"{elementName} workflowId value '{workflowId}' must reference an external workflow using '$sourceDescriptions.<name>.<workflowId>'.";
                yield break;
            }

            var sourceDescriptionName = match.Groups[1].Value;
            if (!sourceDescriptionNames.Contains(sourceDescriptionName))
            {
                yield return $"{elementName} workflowId value '{workflowId}' references unknown sourceDescription '{sourceDescriptionName}'.";
            }

            yield break;
        }

        if (workflowId.StartsWith("$", StringComparison.Ordinal))
        {
            yield return $"{elementName} workflowId value '{workflowId}' must reference an external workflow using '$sourceDescriptions.<name>.<workflowId>'.";
            yield break;
        }

        if (!workflowIds.Contains(workflowId))
        {
            yield return $"{elementName} references unknown workflowId '{workflowId}'.";
        }
    }

    private static IEnumerable<string> ValidateOperationPathSourceDescriptions(string? operationPath, ISet<string> sourceDescriptionNames, string elementName)
    {
        return string.IsNullOrEmpty(operationPath)
            ? []
            : ValidateSourceDescriptionExpressions(operationPath, sourceDescriptionNames, elementName);
    }

    private static IEnumerable<string> ValidateOperationIdSourceDescriptions(
        string? operationId,
        ISet<string> sourceDescriptionNames,
        int nonArazzoSourceDescriptionCount,
        string elementName)
    {
        if (string.IsNullOrEmpty(operationId))
        {
            yield break;
        }

        var sourceDescriptionOperationIdMatch = SourceDescriptionOperationIdRegex().Match(operationId);
        if (sourceDescriptionOperationIdMatch.Success)
        {
            if (!ArazzoRuntimeExpressionValidator.IsRuntimeExpression(operationId))
            {
                yield return $"{elementName} operationId '{operationId}' must be a valid runtime expression.";
                yield break;
            }

            var sourceDescriptionName = sourceDescriptionOperationIdMatch.Groups[1].Value;
            if (!sourceDescriptionNames.Contains(sourceDescriptionName))
            {
                yield return $"{elementName} references unknown sourceDescription '{sourceDescriptionName}'.";
            }

            yield break;
        }

        if (nonArazzoSourceDescriptionCount > 1)
        {
            yield return $"{elementName} operationId '{operationId}' is ambiguous because multiple non-arazzo sourceDescriptions are defined; use '$sourceDescriptions.<name>.{operationId}' syntax.";
        }
    }

    private static IEnumerable<string> ValidateSourceDescriptionExpressions(string value, ISet<string> sourceDescriptionNames, string elementName)
    {
        return SourceDescriptionUrlExpressionRegex()
                .Matches(value)
                .Select(static match => match.Groups[1].Value)
                .Where(sourceDescriptionName => !sourceDescriptionNames.Contains(sourceDescriptionName))
                .Select(x => $"{elementName} references unknown sourceDescription '{x}'.");
    }

    private static IEnumerable<string> ValidateOperationReferenceResolution(ArazzoStep step, ArazzoDocument document, string elementName)
    {
        if (!string.IsNullOrEmpty(step.OperationPath))
        {
            foreach (var error in ValidateOperationPathResolution(step.OperationPath, document, elementName))
            {
                yield return error;
            }
        }

        if (!string.IsNullOrEmpty(step.OperationId))
        {
            foreach (var error in ValidateOperationIdResolution(step.OperationId, document, elementName))
            {
                yield return error;
            }
        }
    }

    private static IEnumerable<string> ValidateOperationPathResolution(string operationPath, ArazzoDocument document, string elementName)
    {
        if (GetOperationPathSyntaxError(operationPath, elementName) is string error)
        {
            yield return error;
            yield break;
        }

        var match = OperationPathRegex().Match(operationPath);
        var sourceDescriptionName = match.Groups[1].Value;
        if (document.Workspace?.TryGetSourceDescription(sourceDescriptionName, out var sourceDescription) != true)
        {
            yield break;
        }

        if (!document.Workspace.IsOpenApiDocumentLoaded(sourceDescription.Uri))
        {
            yield break;
        }

        var pointer = match.Groups[2].Value;
        if (!document.Workspace.ContainsOpenApiOperationPointer(sourceDescription.Uri, pointer))
        {
            yield return $"{elementName} operationPath '{operationPath}' does not resolve to an operation in sourceDescription '{sourceDescriptionName}'.";
        }
    }

    private static string? GetOperationPathSyntaxError(string? operationPath, string elementName)
    {
        return string.IsNullOrEmpty(operationPath) || OperationPathRegex().IsMatch(operationPath)
            ? null
            : $"{elementName} operationPath '{operationPath}' must reference a sourceDescription URL runtime expression followed by a JSON Pointer to an operation path.";
    }

    private static IEnumerable<string> ValidateOperationIdResolution(string operationId, ArazzoDocument document, string elementName)
    {
        var sourceDescriptionOperationIdMatch = SourceDescriptionOperationIdRegex().Match(operationId);
        if (sourceDescriptionOperationIdMatch.Success)
        {
            var sourceDescriptionName = sourceDescriptionOperationIdMatch.Groups[1].Value;
            var referencedOperationId = sourceDescriptionOperationIdMatch.Groups[2].Value;
            if (document.SourceDescriptions?.Any(sourceDescription => sourceDescription.Name == sourceDescriptionName) != true ||
                document.Workspace?.TryGetSourceDescription(sourceDescriptionName, out var sourceDescription) != true)
            {
                yield break;
            }

            if (!document.Workspace.IsOpenApiDocumentLoaded(sourceDescription.Uri))
            {
                yield break;
            }

            if (document.Workspace.CountOpenApiOperationId(sourceDescription.Uri, referencedOperationId) == 0)
            {
                yield return $"{elementName} operationId '{operationId}' does not resolve to an operation in sourceDescription '{sourceDescriptionName}'.";
            }

            yield break;
        }

        if ((document.SourceDescriptions?.Count(static sourceDescription => sourceDescription.Type is not ArazzoDescriptionType.Arazzo) ?? 0) > 1)
        {
            yield break;
        }

        if (document.Workspace is null)
        {
            yield break;
        }

        var loadedSourceDescriptions = document.Workspace.GetSourceDescriptions()
            .Where(static sourceDescription => sourceDescription.Type is not ArazzoDescriptionType.Arazzo)
            .Where(sourceDescription => document.Workspace.IsOpenApiDocumentLoaded(sourceDescription.Uri))
            .ToList();

        if (loadedSourceDescriptions.Count == 0)
        {
            yield break;
        }

        var resolvingSourceDescriptionCount = loadedSourceDescriptions
            .Count(sourceDescription => document.Workspace.CountOpenApiOperationId(sourceDescription.Uri, operationId) > 0);

        if (resolvingSourceDescriptionCount == 0)
        {
            yield return $"{elementName} operationId '{operationId}' does not resolve to an operation in any loaded sourceDescription.";
        }
        else if (resolvingSourceDescriptionCount > 1)
        {
            yield return $"{elementName} operationId '{operationId}' is ambiguous across loaded sourceDescriptions; use '$sourceDescriptions.<name>.{operationId}' syntax.";
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