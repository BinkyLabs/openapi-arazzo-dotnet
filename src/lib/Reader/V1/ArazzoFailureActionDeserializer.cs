namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoFailureAction> FailureActionFixedFields = new()
    {
        { ArazzoConstants.ArazzoResultActionName, (o, v) => o.Name = v.GetScalarValue() },
        { ArazzoConstants.ArazzoResultActionType, (o, v) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoFailureType>(v.Context, out var type))
            {
                return;
            }
            o.Type = type;
        } },
        { ArazzoConstants.ArazzoResultActionWorkflowId, (o, v) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoResultActionStepId, (o, v) => o.StepId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoFailureActionRetryAfter, (o, v) =>
        {
            if (decimal.TryParse(v.GetScalarValue(), out var retryAfter))
            {
                o.RetryAfter = retryAfter;
            }
        } },
        { ArazzoConstants.ArazzoFailureActionRetryLimit, (o, v) =>
        {
            if (ulong.TryParse(v.GetScalarValue(), out var retryLimit))
            {
                o.RetryLimit = retryLimit;
            }
        } },
        { ArazzoConstants.ArazzoResultActionCriteria, (o, v) => o.Criteria = v.CreateList(LoadCriterion) }
    };

    public static readonly PatternFieldMap<ArazzoFailureAction> FailureActionPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k, LoadExtension(k, n)) }
    };

    public static ArazzoFailureAction LoadFailureAction(ParseNode node)
    {
        var mapNode = node.CheckMapNode("FailureAction");
        var failureAction = new ArazzoFailureAction();
        ParseMap(mapNode, failureAction, FailureActionFixedFields, FailureActionPatternFields);

        return failureAction;
    }
}