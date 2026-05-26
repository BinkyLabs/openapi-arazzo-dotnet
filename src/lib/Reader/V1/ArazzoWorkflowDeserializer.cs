using System.Text;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoWorkflow> WorkflowFixedFields = new()
    {
        { ArazzoConstants.ArazzoWorkflowWorkflowId, static (o, v, c) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoWorkflowSummary, static (o, v, c) => o.Summary = v.GetScalarValue() },
        { ArazzoConstants.ArazzoWorkflowInputs, static (o, v, c) => o.Inputs = LoadSchema(v, c) },
        { ArazzoConstants.ArazzoWorkflowDependsOn, static (o, v, c) =>
        {
            var list = v.CreateSimpleList(static n => n.GetScalarValue(), c);
            if (list != null && list.Count > 0)
            {
                o.DependsOn = new HashSet<string>(list.Where(static s => s != null).Cast<string>());
            }
        } },
        { ArazzoConstants.ArazzoWorkflowSteps, static (o, v, c) => o.Steps = v.CreateList(LoadStep, c) },
        { ArazzoConstants.ArazzoWorkflowSuccessActions, static (o, v, c) => o.SuccessActions = v.CreateList<IArazzoSuccessAction>(LoadSuccessAction, c) },
        { ArazzoConstants.ArazzoWorkflowFailureActions, static (o, v, c) => o.FailureActions = v.CreateList<IArazzoFailureAction>(LoadFailureAction, c) },
        { ArazzoConstants.ArazzoWorkflowOutputs, static (o, v, c) => o.Outputs = v.CreateSimpleMap(static n => n.GetScalarValue()!, c) },
        { ArazzoConstants.ArazzoWorkflowParameters, static (o, v, c) => o.Parameters = v.CreateMap<IArazzoParameter>(LoadParameter, c) },
    };

    public static readonly PatternFieldMap<ArazzoWorkflow> WorkflowPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static ArazzoWorkflow LoadWorkflow(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("Workflow", context);
        var workflow = new ArazzoWorkflow();

        // TODO: Implement validation during serialization/deserialization that any of the keys 
        // of Outputs and Parameters dictionaries must match the following regex: ^[a-zA-Z0-9\.\-_]+$

        mapNode.ParseMap(workflow, WorkflowFixedFields, WorkflowPatternFields, context);

        return workflow;
    }
}