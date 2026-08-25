using System.Globalization;
using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

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
            var value = v.GetScalarValue();
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var retryAfter))
            {
                o.RetryAfter = retryAfter;
            }
            else
            {
                c.Diagnostic.Errors.Add(new OpenApiError($"{c.GetLocation()}/{ArazzoConstants.ArazzoFailureActionRetryAfter}", $"{nameof(ArazzoFailureAction)} retryAfter must be a non-negative decimal. Invalid value: '{value}'."));
            }
        } },
        { ArazzoConstants.ArazzoFailureActionRetryLimit, static (o, v, c) =>
        {
            var value = v.GetScalarValue();
            if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var retryLimit))
            {
                o.RetryLimit = retryLimit;
            }
            else
            {
                c.Diagnostic.Errors.Add(new OpenApiError($"{c.GetLocation()}/{ArazzoConstants.ArazzoFailureActionRetryLimit}", $"{nameof(ArazzoFailureAction)} retryLimit must be a non-negative integer. Invalid value: '{value}'."));
            }
        } },
        { ArazzoConstants.ArazzoResultActionCriteria, static (o, v, c) => o.Criteria = v.CreateList(LoadCriterion, c) },
        { "x-parameters", static (o, v, c) => o.Parameters = v.CreateList<IArazzoParameter>(LoadParameter, c) }
    };

    public static PatternFieldMap<ArazzoFailureAction> GetFailureActionPatternFields() =>
    new()
    {
        { s => s.StartsWith(ArazzoConstants.ExtensionFieldNamePrefix, StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.AddExtension(k, LoadExtension(k, n, c)) }
    };
    public static readonly PatternFieldMap<ArazzoFailureAction> FailureActionPatternFields = GetFailureActionPatternFields();

    public static IArazzoFailureAction LoadFailureAction(JsonNode node, ParsingContext context)
    {
        if (TryGetReferenceObject(node, out _, out var referenceString))
        {
            return CreateLocalReusableReference(
                referenceString,
                context,
                ReferenceType.FailureAction,
                "Failure action",
                static (referenceId, hostDocument) => new ArazzoFailureActionReference(referenceId, hostDocument));
        }

        return LoadFailureActionObject(node, context);
    }

    public static ArazzoFailureAction LoadFailureActionObject(JsonNode node, ParsingContext context)
    {
        return LoadFailureActionObjectInternal(node, context, FailureActionFixedFields, FailureActionPatternFields);
    }

    public static ArazzoFailureAction LoadFailureActionObjectInternal(JsonNode node, ParsingContext context, FixedFieldMap<ArazzoFailureAction> failureActionFixedFields, PatternFieldMap<ArazzoFailureAction> failureActionPatternFields)
    {
        var mapNode = node.CheckMapNode("FailureAction", context);
        var failureAction = new ArazzoFailureAction();
        mapNode.ParseMap(failureAction, failureActionFixedFields, failureActionPatternFields, context);
        ArazzoFailureActionValidator.ValidateDeserialization(failureAction, context);

        return failureAction;
    }
}