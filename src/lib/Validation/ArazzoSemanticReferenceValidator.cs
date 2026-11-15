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

    [GeneratedRegex(@"^\$sourceDescriptions\.([^.\s#]+)\.([^.\s#]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex SourceDescriptionWorkflowExpressionRegex();

    internal static void ValidateSerialization(ArazzoDocument document, ArazzoSpecVersion specVersion)
    {
        if (Validate(document, specVersion).FirstOrDefault() is string error)
        {
            throw new ArazzoSerializationException(error);
        }
    }

    internal static void ValidateDeserialization(ArazzoDocument document, ParsingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var error in Validate(document, context.Diagnostic.SpecificationVersion))
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

    internal static IEnumerable<string> ValidateLoadedReferences(ArazzoDocument document)
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

        foreach (var error in ValidateExternalWorkflowReferences(document))
        {
            yield return error;
        }
    }

    private static IEnumerable<string> Validate(ArazzoDocument document, ArazzoSpecVersion specVersion)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.RegisterComponents();

        var workflowIds = new HashSet<string>(
            document.Workflows?
                .Select(static workflow => workflow.WorkflowId)
                .OfType<string>() ?? [],
            StringComparer.Ordinal);
        var sourceDescriptionTypes = new Dictionary<string, ArazzoDescriptionType?>(StringComparer.Ordinal);
        foreach (var sourceDescription in document.SourceDescriptions ?? [])
        {
            if (!string.IsNullOrEmpty(sourceDescription.Name))
            {
                sourceDescriptionTypes.TryAdd(sourceDescription.Name, sourceDescription.Type);
            }
        }

        var sourceDescriptionNames = sourceDescriptionTypes.Keys.ToHashSet(StringComparer.Ordinal);
        var nonArazzoSourceDescriptionNames = sourceDescriptionTypes
            .Where(static sourceDescription => sourceDescription.Value is not ArazzoDescriptionType.Arazzo)
            .Select(static sourceDescription => sourceDescription.Key)
            .ToHashSet(StringComparer.Ordinal);
        var nonArazzoSourceDescriptionCount = document.SourceDescriptions?
            .Count(static sourceDescription => sourceDescription.Type is not ArazzoDescriptionType.Arazzo) ?? 0;

        foreach (var workflow in document.Workflows ?? [])
        {
            foreach (var error in ValidateDependsOnReferences(workflow.DependsOn, workflowIds, sourceDescriptionNames, sourceDescriptionTypes, $"Workflow '{workflow.WorkflowId}'", specVersion))
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
                foreach (var error in ValidateWorkflowReference(step.WorkflowId, workflowIds, sourceDescriptionNames, sourceDescriptionTypes, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}'"))
                {
                    yield return error;
                }

                foreach (var error in ValidateOperationPathSourceDescriptions(step.OperationPath, sourceDescriptionNames, nonArazzoSourceDescriptionNames, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}'"))
                {
                    yield return error;
                }

                foreach (var error in ValidateOperationIdSourceDescriptions(step.OperationId, sourceDescriptionNames, nonArazzoSourceDescriptionNames, nonArazzoSourceDescriptionCount, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}'", specVersion))
                {
                    yield return error;
                }

                foreach (var error in ValidateAsyncApiSourceConstraints(step, sourceDescriptionTypes, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}'"))
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

                foreach (var error in ValidateActions(step.OnSuccess, workflowIds, sourceDescriptionNames, sourceDescriptionTypes, stepIds, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}' success action", document))
                {
                    yield return error;
                }

                foreach (var error in ValidateActions(step.OnFailure, workflowIds, sourceDescriptionNames, sourceDescriptionTypes, stepIds, $"Workflow '{workflow.WorkflowId}' step '{step.StepId}' failure action", document))
                {
                    yield return error;
                }
            }

            foreach (var error in ValidateReusableReferences(workflow.Parameters, $"Workflow '{workflow.WorkflowId}' parameter", document))
            {
                yield return error;
            }

            foreach (var error in ValidateActions(workflow.SuccessActions, workflowIds, sourceDescriptionNames, sourceDescriptionTypes, stepIds, $"Workflow '{workflow.WorkflowId}' success action", document))
            {
                yield return error;
            }

            foreach (var error in ValidateActions(workflow.FailureActions, workflowIds, sourceDescriptionNames, sourceDescriptionTypes, stepIds, $"Workflow '{workflow.WorkflowId}' failure action", document))
            {
                yield return error;
            }
        }

        foreach (var error in ArazzoStepDependencyValidator.Validate(document))
        {
            yield return error;
        }

        foreach (var error in ValidateExternalWorkflowReferences(document))
        {
            yield return error;
        }
    }

    private static IEnumerable<string> ValidateExternalWorkflowReferences(ArazzoDocument document)
    {
        foreach (var workflow in document.Workflows ?? [])
        {
            var elementName = $"Workflow '{workflow.WorkflowId}'";
            // Direct iteration avoids captured delegates and LINQ iterators on the parsing hot path.
            foreach (var dependency in workflow.DependsOn ?? Enumerable.Empty<string>())
            {
                if (GetExternalWorkflowResolutionError(dependency, document, $"{elementName} dependsOn") is { } error)
                {
                    yield return error;
                }
            }
            foreach (var error in ValidateExternalActionWorkflowReferences(workflow.SuccessActions, document, $"{elementName} success action"))
            {
                yield return error;
            }
            foreach (var error in ValidateExternalActionWorkflowReferences(workflow.FailureActions, document, $"{elementName} failure action"))
            {
                yield return error;
            }
            foreach (var step in workflow.Steps ?? [])
            {
                var stepName = $"{elementName} step '{step.StepId}'";
                if (GetExternalWorkflowResolutionError(step.WorkflowId, document, stepName) is { } error)
                {
                    yield return error;
                }
                foreach (var actionError in ValidateExternalActionWorkflowReferences(step.OnSuccess, document, $"{stepName} success action"))
                {
                    yield return actionError;
                }
                foreach (var actionError in ValidateExternalActionWorkflowReferences(step.OnFailure, document, $"{stepName} failure action"))
                {
                    yield return actionError;
                }
            }
        }
    }

    private static IEnumerable<string> ValidateExternalActionWorkflowReferences(
        IEnumerable<IArazzoResultAction>? actions, ArazzoDocument document, string elementName)
    {
        // Most action collections are empty; avoid allocating a capturing LINQ pipeline.
        foreach (var action in actions ?? [])
        {
            if (GetExternalWorkflowResolutionError(action.WorkflowId, document, $"{elementName} '{action.Name}'") is { } error)
            {
                yield return error;
            }
        }
    }

    private static string? GetExternalWorkflowResolutionError(string? reference, ArazzoDocument document, string elementName)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }
        var match = SourceDescriptionWorkflowExpressionRegex().Match(reference);
        if (!match.Success ||
            document.Workspace?.TryGetSourceDescription(document, match.Groups[1].Value, out var source) != true ||
            source.Type is not ArazzoDescriptionType.Arazzo ||
            !document.Workspace.TryGetArazzoDocument(source.Uri, out var targetDocument))
        {
            return null;
        }
        var workflowId = match.Groups[2].Value;
        return targetDocument?.Workflows?.Any(workflow => workflow.WorkflowId == workflowId) == true
            ? null
            : $"{elementName} workflow reference '{reference}' does not resolve to workflowId '{workflowId}' in sourceDescription '{match.Groups[1].Value}'.";
    }

    private static IEnumerable<string> ValidateDependsOnReferences(
        IEnumerable<string>? dependsOn,
        ISet<string> workflowIds,
        ISet<string> sourceDescriptionNames,
        IReadOnlyDictionary<string, ArazzoDescriptionType?> sourceDescriptionTypes,
        string elementName,
        ArazzoSpecVersion specVersion)
    {
        foreach (var dependency in dependsOn ?? [])
        {
            if (string.IsNullOrEmpty(dependency))
            {
                continue;
            }

            if (dependency.StartsWith("$", StringComparison.Ordinal))
            {
                if (!ArazzoRuntimeExpressionValidator.IsRuntimeExpression(dependency, specVersion))
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
                else if (sourceDescriptionTypes[sourceDescriptionName] is not ArazzoDescriptionType.Arazzo)
                {
                    yield return $"{elementName} dependsOn value '{dependency}' references sourceDescription '{sourceDescriptionName}' whose type must be 'arazzo'.";
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
        IReadOnlyDictionary<string, ArazzoDescriptionType?> sourceDescriptionTypes,
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
            foreach (var error in ValidateReusableReferences(action.Parameters, $"{elementName} '{action.Name}' parameter", document))
            {
                yield return error;
            }

            if (action is IArazzoReferenceHolder<BaseArazzoReference> ||
                action.Parameters?.Any(static parameter => parameter is ArazzoParameterReference) == true)
            {
                foreach (var error in ArazzoParameterValidator.ValidateActionParameters(action.Parameters, $"{elementName} '{action.Name}'"))
                {
                    yield return error;
                }
            }

            foreach (var error in ValidateWorkflowReference(action.WorkflowId, workflowIds, sourceDescriptionNames, sourceDescriptionTypes, $"{elementName} '{action.Name}'"))
            {
                yield return error;
            }

            if (!string.IsNullOrEmpty(action.StepId) && !stepIds.Contains(action.StepId))
            {
                yield return $"{elementName} '{action.Name}' references unknown stepId '{action.StepId}'.";
            }
        }
    }

    private static IEnumerable<string> ValidateWorkflowReference(
        string? workflowId,
        ISet<string> workflowIds,
        ISet<string> sourceDescriptionNames,
        IReadOnlyDictionary<string, ArazzoDescriptionType?> sourceDescriptionTypes,
        string elementName)
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
            else if (sourceDescriptionTypes[sourceDescriptionName] is not ArazzoDescriptionType.Arazzo)
            {
                yield return $"{elementName} workflowId value '{workflowId}' references sourceDescription '{sourceDescriptionName}' whose type must be 'arazzo'.";
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

    private static IEnumerable<string> ValidateOperationPathSourceDescriptions(
        string? operationPath,
        ISet<string> sourceDescriptionNames,
        ISet<string> nonArazzoSourceDescriptionNames,
        string elementName)
    {
        return string.IsNullOrEmpty(operationPath)
            ? []
            : ValidateSourceDescriptionExpressions(operationPath, sourceDescriptionNames, nonArazzoSourceDescriptionNames, elementName, "operationPath");
    }

    private static IEnumerable<string> ValidateOperationIdSourceDescriptions(
        string? operationId,
        ISet<string> sourceDescriptionNames,
        ISet<string> nonArazzoSourceDescriptionNames,
        int nonArazzoSourceDescriptionCount,
        string elementName,
        ArazzoSpecVersion specVersion)
    {
        if (string.IsNullOrEmpty(operationId))
        {
            yield break;
        }

        var sourceDescriptionOperationIdMatch = SourceDescriptionOperationIdRegex().Match(operationId);
        if (sourceDescriptionOperationIdMatch.Success)
        {
            if (!ArazzoRuntimeExpressionValidator.IsRuntimeExpression(operationId, specVersion))
            {
                yield return $"{elementName} operationId '{operationId}' must be a valid runtime expression.";
                yield break;
            }

            var sourceDescriptionName = sourceDescriptionOperationIdMatch.Groups[1].Value;
            if (!sourceDescriptionNames.Contains(sourceDescriptionName))
            {
                yield return $"{elementName} references unknown sourceDescription '{sourceDescriptionName}'.";
            }
            else if (!nonArazzoSourceDescriptionNames.Contains(sourceDescriptionName))
            {
                yield return $"{elementName} operationId '{operationId}' references sourceDescription '{sourceDescriptionName}' with type 'arazzo'; operationId must target an OpenAPI sourceDescription.";
            }

            yield break;
        }

        if (nonArazzoSourceDescriptionCount > 1)
        {
            yield return $"{elementName} operationId '{operationId}' is ambiguous because multiple non-arazzo sourceDescriptions are defined; use '$sourceDescriptions.<name>.{operationId}' syntax.";
        }
    }

    private static IEnumerable<string> ValidateSourceDescriptionExpressions(
        string value,
        ISet<string> sourceDescriptionNames,
        ISet<string> nonArazzoSourceDescriptionNames,
        string elementName,
        string fieldName)
    {
        foreach (var sourceDescriptionName in SourceDescriptionUrlExpressionRegex()
            .Matches(value)
            .Select(static match => match.Groups[1].Value))
        {
            if (!sourceDescriptionNames.Contains(sourceDescriptionName))
            {
                yield return $"{elementName} references unknown sourceDescription '{sourceDescriptionName}'.";
            }
            else if (!nonArazzoSourceDescriptionNames.Contains(sourceDescriptionName))
            {
                yield return $"{elementName} {fieldName} '{value}' references sourceDescription '{sourceDescriptionName}' with type 'arazzo'; {fieldName} must target an OpenAPI sourceDescription.";
            }
        }
    }

    private static IEnumerable<string> ValidateAsyncApiSourceConstraints(
        ArazzoStep step,
        IReadOnlyDictionary<string, ArazzoDescriptionType?> sourceDescriptionTypes,
        string elementName)
    {
        if (ArazzoAsyncApiStepValidator.GetChannelSourceName(step.ChannelPath) is string channelSourceName)
        {
            if (!sourceDescriptionTypes.TryGetValue(channelSourceName, out var channelSourceType))
                yield return $"{elementName} references unknown sourceDescription '{channelSourceName}'.";
            else if (channelSourceType is not ArazzoDescriptionType.AsyncAPI)
                yield return $"{elementName} channelPath must target an AsyncAPI sourceDescription; '{channelSourceName}' is not an AsyncAPI sourceDescription.";
        }

        if (string.IsNullOrEmpty(step.OperationId))
            yield break;

        ArazzoDescriptionType? sourceType;
        var match = SourceDescriptionOperationIdRegex().Match(step.OperationId);
        if (match.Success)
        {
            if (!sourceDescriptionTypes.TryGetValue(match.Groups[1].Value, out sourceType))
                yield break;
        }
        else
        {
            var candidates = sourceDescriptionTypes.Values.Where(static type => type is not ArazzoDescriptionType.Arazzo).Take(2).ToArray();
            if (candidates.Length != 1)
                yield break;
            sourceType = candidates[0];
        }

        if (sourceType is ArazzoDescriptionType.AsyncAPI)
        {
            if (!step.Action.HasValue)
                yield return $"{elementName} action is REQUIRED for AsyncAPI steps.";
        }
        else
        {
            if (step.Action.HasValue)
                yield return $"{elementName} action can only be specified for AsyncAPI steps.";
            if (step.CorrelationId is not null && step.Action is ArazzoStepAction.Receive)
                yield return $"{elementName} correlationId can only be specified for AsyncAPI receive steps.";
        }
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
        foreach (var error in ValidateQueryStringContent(step, document, elementName))
            yield return error;
    }

    private static IEnumerable<string> ValidateQueryStringContent(ArazzoStep step, ArazzoDocument document, string elementName)
    {
        if (document.Workspace is null)
            yield break;
        IReadOnlyList<IOpenApiParameter>? targetParameters = null;
        var path = step.OperationPath is null ? null : OperationPathRegex().Match(step.OperationPath);
        var id = step.OperationId is null ? null : SourceDescriptionOperationIdRegex().Match(step.OperationId);
        if (path?.Success == true || id?.Success == true)
        {
            var match = path?.Success == true ? path : id;
            if (match is not null && document.Workspace.TryGetSourceDescription(document, match.Groups[1].Value, out var source))
                targetParameters = document.Workspace.GetOpenApiOperationParameters(source.Uri, match.Groups[2].Value, path?.Success == true);
        }
        else if (step.OperationId is not null)
        {
            var sources = document.Workspace.GetSourceDescriptions(document)
                .Where(static source => source.Type is not ArazzoDescriptionType.Arazzo and not ArazzoDescriptionType.AsyncAPI).Take(2).ToArray();
            if (sources.Length == 1)
                targetParameters = document.Workspace.GetOpenApiOperationParameters(sources[0].Uri, step.OperationId, false);
        }
        if (targetParameters is null)
            yield break;
        var workflow = document.Workflows?.FirstOrDefault(workflow => workflow.Steps?.Contains(step) == true);
        var allParameters = (workflow?.Parameters ?? []).Concat(step.Parameters ?? []).ToArray();
        foreach (var reference in allParameters.OfType<IArazzoReferenceHolder<BaseArazzoReference>>())
            reference.Reference.EnsureHostDocumentIsSet(document);
        var parameters = allParameters.GroupBy(static parameter => (parameter.Name, parameter.In)).Select(static group => group.Last());
        foreach (var error in ArazzoParameterValidator.ValidateQueryStringContent(parameters, targetParameters, elementName))
            yield return error;
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
        if (document.Workspace?.TryGetSourceDescription(document, sourceDescriptionName, out var sourceDescription) != true)
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
                document.Workspace?.TryGetSourceDescription(document, sourceDescriptionName, out var sourceDescription) != true)
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

        var loadedSourceDescriptions = document.Workspace.GetSourceDescriptions(document)
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
        foreach (var referenceHolder in (items ?? [])
            .OfType<IArazzoReferenceHolder<BaseArazzoReference>>())
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