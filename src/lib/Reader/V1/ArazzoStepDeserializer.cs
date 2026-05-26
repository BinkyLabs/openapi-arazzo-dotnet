using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoStep> StepFixedFields = new()
    {
        { ArazzoConstants.ArazzoStepDescription, static (o, v, c) => o.Description = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepStepId, static (o, v, c) => o.StepId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepOperationId, static (o, v, c) => o.OperationId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepOperationPath, static (o, v, c) => o.OperationPath = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepWorkflowId, static (o, v, c) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepParameters, static (o, v, c) => o.Parameters = v.CreateList<IArazzoParameter>(LoadParameter, c) },
        { ArazzoConstants.ArazzoStepRequestBody, static (o, v, c) => o.RequestBody = LoadRequestBody(v, c) },
        { ArazzoConstants.ArazzoStepSuccessCriteria, static (o, v, c) => o.SuccessCriteria = v.CreateList(LoadCriterion, c) },
        { ArazzoConstants.ArazzoStepOnSuccess, static (o, v, c) => o.OnSuccess = v.CreateList<IArazzoSuccessAction>(LoadSuccessAction, c) },
        { ArazzoConstants.ArazzoStepOnFailure, static (o, v, c) => o.OnFailure = v.CreateList<IArazzoFailureAction>(LoadFailureAction, c) },
        { ArazzoConstants.ArazzoStepOutputs, static (o, v, c) => o.Outputs = v.CreateSimpleMap(static n => n.GetScalarValue()!, c) }
    };

    public static readonly PatternFieldMap<ArazzoStep> StepPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static ArazzoStep LoadStep(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("Step", context);
        var step = new ArazzoStep();
        mapNode.ParseMap(step, StepFixedFields, StepPatternFields, context);

        return step;
    }
}