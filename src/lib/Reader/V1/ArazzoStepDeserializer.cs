namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoStep> StepFixedFields = new()
    {
        { ArazzoConstants.ArazzoStepDescription, (o, v) => o.Description = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepStepId, (o, v) => o.StepId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepOperationId, (o, v) => o.OperationId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepOperationPath, (o, v) => o.OperationPath = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepWorkflowId, (o, v) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepParameters, (o, v) => o.Parameters = v.CreateList<IArazzoParameter>(LoadParameter) },
        { ArazzoConstants.ArazzoStepRequestBody, (o, v) => o.RequestBody = LoadRequestBody(v) },
        { ArazzoConstants.ArazzoStepSuccessCriteria, (o, v) => o.SuccessCriteria = v.CreateList(LoadCriterion) },
        { ArazzoConstants.ArazzoStepOnSuccess, (o, v) => o.OnSuccess = v.CreateList<IArazzoSuccessAction>(LoadSuccessAction) },
        { ArazzoConstants.ArazzoStepOnFailure, (o, v) => o.OnFailure = v.CreateList<IArazzoFailureAction>(LoadFailureAction) },
        { ArazzoConstants.ArazzoStepOutputs, (o, v) => o.Outputs = v.CreateSimpleMap(static n => n.GetScalarValue()) }
    };

    public static readonly PatternFieldMap<ArazzoStep> StepPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k, LoadExtension(k, n)) }
    };

    public static ArazzoStep LoadStep(ParseNode node)
    {
        var mapNode = node.CheckMapNode("Step");
        var step = new ArazzoStep();
        ParseMap(mapNode, step, StepFixedFields, StepPatternFields);

        return step;
    }
}