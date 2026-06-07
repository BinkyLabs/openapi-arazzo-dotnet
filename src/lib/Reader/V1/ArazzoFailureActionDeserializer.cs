using System.Text.Json.Nodes;

namespace BinkyLabs.OpenApi.Arazzo.Reader.V1;

internal static partial class ArazzoV1Deserializer
{
    public static readonly FixedFieldMap<ArazzoFailureAction> FailureActionFixedFields = new()
    {
        { ArazzoConstants.ArazzoResultActionName, static (o, v, c) => o.Name = v.GetScalarValue() },
        { ArazzoConstants.ArazzoResultActionType, static (o, v, c) =>
        {
            if (!v.GetScalarValue().TryGetEnumFromDisplayName<ArazzoFailureType>(c, out var type))
            {
                return;
            }
            o.Type = type;
        } },
        { ArazzoConstants.ArazzoResultActionWorkflowId, static (o, v, c) => o.WorkflowId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoResultActionStepId, static (o, v, c) => o.StepId = v.GetScalarValue() },
        { ArazzoConstants.ArazzoFailureActionRetryAfter, static (o, v, c) =>
        {
            if (decimal.TryParse(v.GetScalarValue(), out var retryAfter))
            {
                o.RetryAfter = retryAfter;
            }
        } },
        { ArazzoConstants.ArazzoFailureActionRetryLimit, static (o, v, c) =>
        {
            if (ulong.TryParse(v.GetScalarValue(), out var retryLimit))
            {
                o.RetryLimit = retryLimit;
            }
        } },
        { ArazzoConstants.ArazzoResultActionCriteria, static (o, v, c) => o.Criteria = v.CreateList(LoadCriterion, c) }
    };

    public static readonly PatternFieldMap<ArazzoFailureAction> FailureActionPatternFields = new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };

    public static IArazzoFailureAction LoadFailureAction(JsonNode node, ParsingContext context)
    {
        if (TryGetReferenceObject(node, out _, out var referenceString))
        {
            ThrowIfExternalReferenceNotSupported(referenceString, "Failure action");
            var hostDocument = context.GetFromTempStorage<ArazzoDocument>("CurrentDocument");
            var reference = new ArazzoFailureActionReference(GetReferenceId(referenceString), hostDocument, GetExternalResource(referenceString));
            reference.Reference.EnsureHostDocumentIsSet(hostDocument ?? new ArazzoDocument());
            reference.Reference.SetJsonPointerPath(referenceString, context.GetLocation());
            return reference;
        }

        return LoadFailureActionObject(node, context);
    }

    public static ArazzoFailureAction LoadFailureActionObject(JsonNode node, ParsingContext context)
    {
        var mapNode = node.CheckMapNode("FailureAction", context);
        var failureAction = new ArazzoFailureAction();
        mapNode.ParseMap(failureAction, FailureActionFixedFields, FailureActionPatternFields, context);

        return failureAction;
    }
}