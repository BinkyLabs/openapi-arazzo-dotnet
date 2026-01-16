using System;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoSuccessAction> SuccessActionFixedFields = new()
    {
        { ArazzoConstants.ArazzoSuccessActionName, (o, v) => o.Name = v.GetScalarValue() },
        { ArazzoConstants.ArazzoSuccessActionType, (o, v) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoSuccessType>(v.Context, out var type))
            {
                return;
            }
            o.Type = type;
        } },
        { ArazzoConstants.ArazzoSuccessActionWorkflowId, (o, v) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoSuccessActionStepId, (o, v) => o.StepId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoSuccessActionCriteria, (o, v) => o.Criteria = v.CreateList(LoadCriterion) }
    };

    public static readonly PatternFieldMap<ArazzoSuccessAction> SuccessActionPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n) => o.AddExtension(k, LoadExtension(k, n)) }
    };

    public static ArazzoSuccessAction LoadSuccessAction(ParseNode node)
    {
        var mapNode = node.CheckMapNode("SuccessAction");
        var successAction = new ArazzoSuccessAction();
        ParseMap(mapNode, successAction, SuccessActionFixedFields, SuccessActionPatternFields);

        return successAction;
    }
}