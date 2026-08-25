using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoSuccessAction> SuccessActionFixedFields = new()
    {
        { ArazzoConstants.ArazzoResultActionName, static (o, v, c) => o.Name = v.GetScalarValue() },
        { ArazzoConstants.ArazzoResultActionType, static (o, v, c) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoSuccessType>(c, out var type))
            {
                return;
            }
            o.Type = type;
        } },
        { ArazzoConstants.ArazzoResultActionWorkflowId, static (o, v, c) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoResultActionStepId, static (o, v, c) => o.StepId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoResultActionCriteria, static (o, v, c) => o.Criteria = v.CreateList(LoadCriterion, c) }
    };

    public static PatternFieldMap<ArazzoSuccessAction> GetSuccessActionPatternFields() =>
    new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };
    public static readonly PatternFieldMap<ArazzoSuccessAction> SuccessActionPatternFields = GetSuccessActionPatternFields();

    public static IArazzoSuccessAction LoadSuccessAction(JsonNode node, ParsingContext context)
    {
        if (TryGetReferenceObject(node, out _, out var referenceString))
        {
            return CreateLocalReusableReference(
                referenceString,
                context,
                ReferenceType.SuccessAction,
                "Success action",
                static (referenceId, hostDocument) => new ArazzoSuccessActionReference(referenceId, hostDocument));
        }

        return LoadSuccessActionObject(node, context);
    }

    public static ArazzoSuccessAction LoadSuccessActionObject(JsonNode node, ParsingContext context)
    {
        return LoadSuccessActionObjectInternal(node, context, SuccessActionFixedFields, SuccessActionPatternFields);
    }

    public static ArazzoSuccessAction LoadSuccessActionObjectInternal(JsonNode node, ParsingContext context, FixedFieldMap<ArazzoSuccessAction> successActionFixedFields, PatternFieldMap<ArazzoSuccessAction> successActionPatternFields)
    {
        var mapNode = node.CheckMapNode("SuccessAction", context);
        var successAction = new ArazzoSuccessAction();
        mapNode.ParseMap(successAction, successActionFixedFields, successActionPatternFields, context);
        ArazzoResultActionValidator.ValidateDeserialization(successAction, context);

        return successAction;
    }
}