using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BinkyLabs.OpenApi.Arazzo.Validation;

internal static partial class ArazzoStepDependencyValidator
{
    private readonly record struct DependencyNode(string WorkflowId, string? StepId);

    [GeneratedRegex(@"^\$(?:workflows\.(?<workflow>[^.\s]+)\.steps\.(?<step>[^.\s]+)|sourceDescriptions\.(?<source>[^.\s]+)\.(?<workflow>[^.\s]+)\.steps\.(?<step>[^.\s]+))$", RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitDependencyRegex();

    [GeneratedRegex("""(?:"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*')|(?<reference>\$(?:steps\.(?<step>[^.\s{}"'#()[\]=<>!,+*/]+)|workflows\.(?<workflow>[^.\s{}"'#()[\]=<>!,+*/]+)(?:\.steps\.(?<step>[^.\s{}"'#()[\]=<>!,+*/]+))?|sourceDescriptions\.(?<source>[^.\s{}"'#()[\]=<>!,+*/]+)\.(?<workflow>[^.\s{}"'#()[\]=<>!,+*/]+)(?:\.steps\.(?<step>[^.\s{}"'#()[\]=<>!,+*/]+))?)\.outputs(?=[.#\s)}\]=<>!]|$))""", RegexOptions.CultureInvariant)]
    private static partial Regex OutputReferenceRegex();

    internal static IEnumerable<string> Validate(ArazzoDocument document)
    {
        var graph = new Dictionary<DependencyNode, HashSet<DependencyNode>>();
        var steps = new Dictionary<DependencyNode, (ArazzoWorkflow Workflow, ArazzoStep Step, int Index, bool Sequential)>();
        var sources = new Dictionary<string, ArazzoDescriptionType?>(StringComparer.Ordinal);
        foreach (var source in document.SourceDescriptions ?? [])
        {
            if (!string.IsNullOrEmpty(source.Name))
                sources.TryAdd(source.Name, source.Type);
        }

        foreach (var workflow in document.Workflows ?? [])
        {
            if (string.IsNullOrEmpty(workflow.WorkflowId))
                continue;
            var completion = new DependencyNode(workflow.WorkflowId, null);
            if (!graph.TryAdd(completion, []))
                continue;
            var sequential = workflow.Steps?.Any(static step => step.DependsOn is { Count: > 0 }) != true;
            DependencyNode? previous = null;
            for (var index = 0; index < (workflow.Steps?.Count ?? 0); index++)
            {
                var step = workflow.Steps![index];
                if (string.IsNullOrEmpty(step.StepId))
                    continue;
                var node = new DependencyNode(workflow.WorkflowId, step.StepId);
                if (!graph.TryAdd(node, []))
                    continue;
                steps.Add(node, (workflow, step, index, sequential));
                graph[completion].Add(node);
                if (sequential && previous is DependencyNode predecessor)
                    graph[node].Add(predecessor);
                previous = node;
            }
        }

        foreach (var (node, details) in steps)
        {
            var elementName = Describe(node);
            foreach (var dependency in details.Workflow.DependsOn ?? Enumerable.Empty<string>())
            {
                var prerequisite = new DependencyNode(dependency, null);
                if (graph.ContainsKey(prerequisite))
                    graph[node].Add(prerequisite);
            }

            foreach (var dependency in details.Step.DependsOn ?? Enumerable.Empty<string>())
            {
                DependencyNode? target;
                string? error;
                if (dependency.StartsWith("$", StringComparison.Ordinal))
                {
                    var match = ExplicitDependencyRegex().Match(dependency);
                    if (!match.Success)
                    {
                        yield return $"{elementName} dependsOn value '{dependency}' must reference a stepId, '$workflows.<workflowId>.steps.<stepId>', or '$sourceDescriptions.<name>.<workflowId>.steps.<stepId>'.";
                        continue;
                    }
                    (target, error) = ResolveReference(match, node, graph, sources);
                }
                else
                {
                    target = new DependencyNode(node.WorkflowId, dependency);
                    error = graph.ContainsKey(target.Value) ? null : $"references unknown stepId '{dependency}' in workflow '{node.WorkflowId}'.";
                }

                if (error is not null)
                    yield return $"{elementName} dependsOn value '{dependency}' {error}";
                else if (target == node)
                    yield return $"{elementName} cannot depend on itself.";
                else if (target is DependencyNode prerequisite)
                    graph[node].Add(prerequisite);
            }

            foreach (var (expression, afterStep) in GetExpressions(details.Workflow, details.Step))
            {
                foreach (Match match in OutputReferenceRegex().Matches(expression).Where(x => x.Groups["reference"].Success))
                {
                    var (target, error) = ResolveReference(match, node, graph, sources);
                    var reference = match.Groups["reference"].Value;
                    if (error is not null)
                    {
                        yield return $"{elementName} implicit output reference '{reference}' {error}";
                    }
                    else if (target == node)
                    {
                        if (!afterStep)
                            yield return $"{elementName} cannot depend on itself through output reference '{reference}'.";
                    }
                    else if (target is DependencyNode prerequisite)
                    {
                        if (details.Sequential && prerequisite.WorkflowId == node.WorkflowId &&
                            steps.TryGetValue(prerequisite, out var targetDetails) && targetDetails.Index > details.Index)
                        {
                            yield return $"{elementName} has an unsatisfiable forward reference to step '{prerequisite.StepId}' outputs in a sequential workflow without step dependsOn.";
                        }
                        graph[node].Add(prerequisite);
                    }
                }
            }
        }

        if (FindCycle(graph) is string cycle)
            yield return $"Step dependency cycle detected: {cycle}.";
    }

    private static (DependencyNode? Target, string? Error) ResolveReference(
        Match match,
        DependencyNode owner,
        IReadOnlyDictionary<DependencyNode, HashSet<DependencyNode>> graph,
        IReadOnlyDictionary<string, ArazzoDescriptionType?> sources)
    {
        if (match.Groups["source"].Success)
        {
            var sourceName = match.Groups["source"].Value;
            if (!sources.TryGetValue(sourceName, out var type))
                return (null, $"references unknown sourceDescription '{sourceName}'.");
            return type is ArazzoDescriptionType.Arazzo
                ? (null, null)
                : (null, $"references sourceDescription '{sourceName}' whose type must be 'arazzo'.");
        }

        var workflowId = match.Groups["workflow"].Success ? match.Groups["workflow"].Value : owner.WorkflowId;
        if (!graph.ContainsKey(new DependencyNode(workflowId, null)))
            return (null, $"references unknown workflowId '{workflowId}'.");
        var stepId = match.Groups["step"].Success ? match.Groups["step"].Value : null;
        var target = new DependencyNode(workflowId, stepId);
        return graph.ContainsKey(target)
            ? (target, null)
            : (null, $"references unknown stepId '{stepId}' in workflow '{workflowId}'.");
    }

    private static IEnumerable<(string Expression, bool AfterStep)> GetExpressions(ArazzoWorkflow workflow, ArazzoStep step)
    {
        var parameters = (workflow.Parameters ?? []).Concat(step.Parameters ?? [])
            .GroupBy(static parameter => (ArazzoParameterValidator.GetDuplicateDetectionName(parameter.Name ?? string.Empty, parameter.In), parameter.In))
            .Select(static group => group.Last());
        foreach (var parameter in parameters)
        {
            foreach (var expression in GetValueExpressions(parameter.Value?.Any, parameter.Value?.Expression?.Expression, parameter.Value?.Selector))
                yield return (expression, false);
        }

        foreach (var expression in ArazzoRuntimeExpressionValidator.GetRuntimeExpressions(step.CorrelationId))
            yield return (expression, false);
        foreach (var expression in GetValueExpressions(step.RequestBody?.Payload?.Any, step.RequestBody?.Payload?.Expression?.Expression, step.RequestBody?.Payload?.Selector))
            yield return (expression, false);
        foreach (var replacement in step.RequestBody?.Replacements ?? [])
        {
            foreach (var expression in GetValueExpressions(replacement.Value?.Any, replacement.Value?.Expression?.Expression, replacement.Value?.Selector))
                yield return (expression, false);
        }

        foreach (var expression in GetCriterionExpressions(step.SuccessCriteria))
            yield return (expression, true);
        foreach (var output in step.Outputs?.Values ?? [])
        {
            foreach (var expression in GetValueExpressions(null, output.Expression?.Expression, output.Selector))
                yield return (expression, true);
        }

        foreach (var expression in GetActionExpressions(workflow.SuccessActions, step.OnSuccess))
            yield return (expression, true);
        foreach (var expression in GetActionExpressions(workflow.FailureActions, step.OnFailure))
            yield return (expression, true);
    }

    private static IEnumerable<string> GetActionExpressions<T>(IEnumerable<T>? inheritedActions, IEnumerable<T>? actions) where T : IArazzoResultAction
    {
        var effectiveActions = (inheritedActions ?? []).Concat(actions ?? [])
            .GroupBy(ArazzoActionListValidator.GetActionKey)
            .Select(static group => group.Last());
        foreach (var action in effectiveActions)
        {
            foreach (var expression in GetCriterionExpressions(action.Criteria))
                yield return expression;
            foreach (var parameter in action.Parameters ?? [])
            {
                foreach (var expression in GetValueExpressions(parameter.Value?.Any, parameter.Value?.Expression?.Expression, parameter.Value?.Selector))
                    yield return expression;
            }
        }
    }

    private static IEnumerable<string> GetCriterionExpressions(IEnumerable<ArazzoCriterion>? criteria)
    {
        foreach (var criterion in criteria ?? [])
        {
            if (criterion.Context is not null)
                yield return criterion.Context.Expression;
            if (criterion.Condition is not null &&
                (criterion.Type?.Type ?? criterion.Type?.ExpressionType?.Type) is null or ArazzoCriterionExpressionTypeType.Simple)
                yield return criterion.Condition;
        }
    }

    private static IEnumerable<string> GetValueExpressions(JsonNode? literal, string? expression, ArazzoSelector? selector)
    {
        if (expression is not null)
            yield return expression;
        if (selector?.Context is not null)
            yield return selector.Context.Expression;
        if (literal is JsonValue value && value.TryGetValue<string>(out var text))
        {
            foreach (var runtimeExpression in ArazzoRuntimeExpressionValidator.GetRuntimeExpressions(text))
                yield return runtimeExpression;
        }
        else if (literal is JsonArray array)
        {
            foreach (var item in array)
            {
                foreach (var runtimeExpression in GetValueExpressions(item, null, null))
                    yield return runtimeExpression;
            }
        }
        else if (literal is JsonObject jsonObject)
        {
            var values = ArazzoRuntimeExpressionValidator.TryGetSelectorContext(jsonObject, out var selectorContext)
                ? new[] { selectorContext }
                : jsonObject.Select(static property => property.Value);
            foreach (var item in values)
            {
                foreach (var runtimeExpression in GetValueExpressions(item, null, null))
                    yield return runtimeExpression;
            }
        }
    }

    private static string? FindCycle(IReadOnlyDictionary<DependencyNode, HashSet<DependencyNode>> graph)
    {
        var counts = graph.ToDictionary(static item => item.Key, static item => item.Value.Count);
        var dependents = graph.Keys.ToDictionary(static node => node, static _ => new List<DependencyNode>());
        foreach (var (node, prerequisites) in graph)
        {
            foreach (var prerequisite in prerequisites)
                dependents[prerequisite].Add(node);
        }
        var ready = new Queue<DependencyNode>(counts.Where(static item => item.Value == 0).Select(static item => item.Key));
        while (ready.TryDequeue(out var node))
        {
            foreach (var dependent in dependents[node])
            {
                if (--counts[dependent] == 0)
                    ready.Enqueue(dependent);
            }
        }
        var unresolved = counts.Where(static item => item.Value > 0).Select(static item => item.Key).ToArray();
        if (unresolved.Length == 0)
            return null;

        var positions = new Dictionary<DependencyNode, int>();
        var path = new List<DependencyNode>();
        var current = unresolved[0];
        while (positions.TryAdd(current, path.Count))
        {
            path.Add(current);
            current = graph[current].First(prerequisite => counts[prerequisite] > 0);
        }
        return string.Join(" -> ", path.Skip(positions[current]).Append(current).Select(Describe));
    }

    private static string Describe(DependencyNode node) =>
        node.StepId is null ? $"Workflow '{node.WorkflowId}' completion" : $"Workflow '{node.WorkflowId}' step '{node.StepId}'";
}