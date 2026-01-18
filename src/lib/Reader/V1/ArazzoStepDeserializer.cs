namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoStep> StepFixedFields = new()
    {
        { ArazzoConstants.ArazzoStepDescription, static (o, v) => o.Description = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepStepId, static (o, v) => o.StepId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepOperationId, static (o, v) => o.OperationId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepOperationPath, static (o, v) => o.OperationPath = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepWorkflowId, static (o, v) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoStepParameters, static (o, v) => o.Parameters = v.CreateList<IArazzoParameter>(LoadParameter) },
        { ArazzoConstants.ArazzoStepRequestBody, static (o, v) => o.RequestBody = LoadRequestBody(v) },
        { ArazzoConstants.ArazzoStepSuccessCriteria, static (o, v) => o.SuccessCriteria = v.CreateList(LoadCriterion) },
        { ArazzoConstants.ArazzoStepOnSuccess, static (o, v) => o.OnSuccess = v.CreateList<IArazzoSuccessAction>(LoadSuccessAction) },
        { ArazzoConstants.ArazzoStepOnFailure, static (o, v) => o.OnFailure = v.CreateList<IArazzoFailureAction>(LoadFailureAction) },
        { ArazzoConstants.ArazzoStepOutputs, static (o, v) => o.Outputs = v.CreateSimpleMap(static n => n.GetScalarValue()) }
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