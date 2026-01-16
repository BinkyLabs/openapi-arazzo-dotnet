namespace BinkyLabs.OpenApi.Arazzo;
/// <summary>
/// Contains constant values used across Arazzo models and readers.
/// </summary>
public static class ArazzoConstants
{
    /// <summary>
    /// The prefix for extension field names.
    /// </summary>
    public const string ExtensionFieldNamePrefix = "x-";

    /// <summary>
    /// The "info" field name in the Arazzo document.
    /// </summary>
    public const string ArazzoDocumentInfo = "info";
    /// <summary>
    /// The "arazzo" field name in the Arazzo document.
    /// </summary>
    public const string ArazzoDocumentArazzo = "arazzo";

    // ArazzoInfo
    /// <summary>
    /// The "title" field name in the Arazzo Info object.
    /// </summary>
    public const string ArazzoInfoTitle = "title";
    /// <summary>
    /// The "version" field name in the Arazzo Info object.
    /// </summary>
    public const string ArazzoInfoVersion = "version";

    // ArazzoParameter
    /// <summary>
    /// The "name" field name in the Arazzo Parameter object.
    /// </summary>
    public const string ArazzoParameterName = "name";
    /// <summary>
    /// The "in" field name in the Arazzo Parameter object.
    /// </summary>
    public const string ArazzoParameterIn = "in";
    /// <summary>
    /// The "value" field name in the Arazzo Parameter object.
    /// </summary>
    public const string ArazzoParameterValue = "value";

    // ArazzoSourceDescription
    /// <summary>
    /// The "name" field name in the Arazzo Source Description object.
    /// </summary>
    public const string ArazzoSourceDescriptionName = "name";
    /// <summary>
    /// The "url" field name in the Arazzo Source Description object.
    /// </summary>
    public const string ArazzoSourceDescriptionUrl = "url";
    /// <summary>
    /// The "type" field name in the Arazzo Source Description object.
    /// </summary>
    public const string ArazzoSourceDescriptionType = "type";

    // ArazzoPayloadReplacement
    /// <summary>
    /// The "target" field name in the Arazzo Payload Replacement object.
    /// </summary>
    public const string ArazzoPayloadReplacementTarget = "target";
    /// <summary>
    /// The "value" field name in the Arazzo Payload Replacement object.
    /// </summary>
    public const string ArazzoPayloadReplacementValue = "value";

    // ArazzoRequestBody
    /// <summary>
    /// The "contentType" field name in the Arazzo Request Body object.
    /// </summary>
    public const string ArazzoRequestBodyContentType = "contentType";
    /// <summary>
    /// The "payload" field name in the Arazzo Request Body object.
    /// </summary>
    public const string ArazzoRequestBodyPayload = "payload";
    /// <summary>
    /// The "replacements" field name in the Arazzo Request Body object.
    /// </summary>
    public const string ArazzoRequestBodyReplacements = "replacements";

    // ArazzoCriterion
    /// <summary>
    /// The "context" field name in the Arazzo Criterion object.
    /// </summary>
    public const string ArazzoCriterionContext = "context";
    /// <summary>
    /// The "type" field name in the Arazzo Criterion object.
    /// </summary>
    public const string ArazzoCriterionType = "type";
    /// <summary>
    /// The "condition" field name in the Arazzo Criterion object.
    /// </summary>
    public const string ArazzoCriterionCondition = "condition";

    // ArazzoCriterionExpressionType
    /// <summary>
    /// The "type" field name in the Arazzo Criterion Expression Type object.
    /// </summary>
    public const string ArazzoCriterionExpressionTypeType = "type";
    /// <summary>
    /// The "version" field name in the Arazzo Criterion Expression Type object.
    /// </summary>
    public const string ArazzoCriterionExpressionTypeVersion = "version";

    // ArazzoSuccessAction
    /// <summary>
    /// The "name" field name in the Arazzo Success Action object.
    /// </summary>
    public const string ArazzoSuccessActionName = "name";
    /// <summary>
    /// The "type" field name in the Arazzo Success Action object.
    /// </summary>
    public const string ArazzoSuccessActionType = "type";
    /// <summary>
    /// The "workflowId" field name in the Arazzo Success Action object.
    /// </summary>
    public const string ArazzoSuccessActionWorkflowId = "workflowId";
    /// <summary>
    /// The "stepId" field name in the Arazzo Success Action object.
    /// </summary>
    public const string ArazzoSuccessActionStepId = "stepId";
    /// <summary>
    /// The "criteria" field name in the Arazzo Success Action object.
    /// </summary>
    public const string ArazzoSuccessActionCriteria = "criteria";
}