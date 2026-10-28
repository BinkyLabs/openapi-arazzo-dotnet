using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static ArazzoSuccessActionUnion LoadSuccessActionUnion(JsonNode node, ParsingContext context)
    {
        if (node is JsonObject map && map.ContainsKey(ArazzoConstants.ArazzoReusableObjectReference) &&
            (map.ContainsKey(ArazzoConstants.ArazzoResultActionName) || map.ContainsKey(ArazzoConstants.ArazzoResultActionType)))
            throw new ArazzoReaderException("A success action must be either a Success Action Object or a Reusable Object, not both.", context);
        var action = context.Diagnostic.SpecificationVersion is ArazzoSpecVersion.Arazzo1_1
            ? V1_1.ArazzoV1_1Deserializer.LoadSuccessAction(node, context)
            : LoadSuccessAction(node, context);
        return action switch
        {
            ArazzoSuccessAction inline => inline,
            ArazzoSuccessActionReference reference => reference,
            _ => throw new ArazzoReaderException("Unsupported success action union member.", context)
        };
    }

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
        { ArazzoConstants.ArazzoResultActionCriteria, static (o, v, c) => o.Criteria = v.CreateList(LoadCriterion, c) },
        { ArazzoConstants.GetArazzo1_0ExtensionName(ArazzoConstants.ArazzoResultActionParameters), static (o, v, c) => o.Parameters = v.CreateList<IArazzoParameter>(LoadParameter, c) }
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