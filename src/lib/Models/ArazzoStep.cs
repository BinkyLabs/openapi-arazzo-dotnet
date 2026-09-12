using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;
using BinkyLabs.OpenApi.Arazzo.Writers;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Represents a step definition in an Arazzo workflow.
/// </summary>
public class ArazzoStep : IArazzoExtensible, IArazzoSerializable
{
    /// <summary>
    /// Builds a JSON Pointer operation key from an OpenAPI path item and HTTP method.
    /// </summary>
    /// <param name="pathItem">The OpenAPI path item, for example <c>/pets/{petId}</c>.</param>
    /// <param name="httpMethod">The HTTP method, for example <c>GET</c> or <c>POST</c>.</param>
    /// <returns>The normalized operation pointer key.</returns>
    public static string BuildOperationPointer(string pathItem, string httpMethod)
    {
        ArgumentException.ThrowIfNullOrEmpty(pathItem);
        ArgumentException.ThrowIfNullOrEmpty(httpMethod);

        return $"#/paths/{EscapePointerSegment(pathItem)}/{httpMethod.ToLowerInvariant()}";
    }

    /// <summary>
    /// Gets or sets the description of the step.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the step identifier.
    /// </summary>
    public string? StepId { get; set; }

    /// <summary>
    /// Gets or sets the operation identifier.
    /// </summary>
    public string? OperationId { get; set; }

    /// <summary>
    /// Gets or sets the operation path.
    /// </summary>
    public string? OperationPath { get; set; }

    /// <summary>
    /// Gets or sets the AsyncAPI channel path.
    /// </summary>
    public string? ChannelPath { get; set; }

    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public string? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the AsyncAPI step action.
    /// </summary>
    public ArazzoStepAction? Action { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier for AsyncAPI receive steps.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the timeout in milliseconds.
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the step identifiers or runtime references that this step depends on.
    /// </summary>
    public ISet<string>? DependsOn { get; set; }

    /// <summary>
    /// Gets or sets the list of parameters.
    /// </summary>
    public List<IArazzoParameter>? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the request body.
    /// </summary>
    public ArazzoRequestBody? RequestBody { get; set; }

    /// <summary>
    /// Gets or sets the success criteria.
    /// </summary>
    public List<ArazzoCriterion>? SuccessCriteria { get; set; }

    /// <summary>
    /// Gets or sets the success actions.
    /// </summary>
    public List<IArazzoSuccessAction>? OnSuccess { get; set; }

    /// <summary>
    /// Gets or sets the failure actions.
    /// </summary>
    public List<IArazzoFailureAction>? OnFailure { get; set; }

    /// <summary>
    /// Gets or sets output values as runtime expressions or selector objects.
    /// </summary>
    public IDictionary<string, JsonNode>? OutputValues { get; set; }

    /// <summary>
    /// Gets or sets the output expressions.
    /// Values must be valid runtime expressions as defined by
    /// <see href="https://spec.openapis.org/arazzo/v1.0.1.html#runtime-expressions">the Arazzo specification</see>.
    /// </summary>
    public IDictionary<string, string>? Outputs { get; set; }

    /// <inheritdoc/>
    public IDictionary<string, IArazzoExtension>? Extensions { get; set; }

    /// <summary>
    /// Serializes the step as an OpenAPI Arazzo v1.0.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <summary>
    /// Serializes the step as an OpenAPI Arazzo v1.1.0 JSON object.
    /// </summary>
    /// <param name="writer">The OpenAPI writer to use for serialization.</param>
    public void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1, static (w, obj) => obj.SerializeAsV1_1(w));
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);

        ArgumentException.ThrowIfNullOrEmpty(StepId);
        ValidateOperationReferenceFields(specVersion);
        ArazzoSemanticReferenceValidator.ValidateOperationPathSerialization(OperationPath, $"{nameof(ArazzoStep)} '{StepId}'");
        ValidateRequestBodyApplicability();
        ValidateParameters();
        ValidateActions();
        ArazzoKeyValidator.ValidateSerializationKeys(Outputs?.Keys, $"{nameof(ArazzoStep)}.{nameof(Outputs)}");
        ArazzoRuntimeExpressionValidator.ValidateSerializationExpressions(Outputs, $"{nameof(ArazzoStep)}.{nameof(Outputs)}", specVersion);

        writer.WriteStartObject();

        if (!string.IsNullOrEmpty(Description))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoStepDescription, Description);
        }

        writer.WriteProperty(ArazzoConstants.ArazzoStepStepId, StepId);

        if (!string.IsNullOrEmpty(OperationId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoStepOperationId, OperationId);
        }

        if (!string.IsNullOrEmpty(OperationPath))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoStepOperationPath, OperationPath);
        }

        writer.WriteProperty(GetVersionedFieldName(specVersion, ArazzoConstants.ArazzoStepChannelPath), ChannelPath);

        if (!string.IsNullOrEmpty(WorkflowId))
        {
            writer.WriteProperty(ArazzoConstants.ArazzoStepWorkflowId, WorkflowId);
        }

        if (Action.HasValue)
        {
            writer.WriteProperty(GetVersionedFieldName(specVersion, ArazzoConstants.ArazzoStepAction), Action.Value.GetDisplayName());
        }

        writer.WriteProperty(GetVersionedFieldName(specVersion, ArazzoConstants.ArazzoStepCorrelationId), CorrelationId);

        if (Timeout.HasValue)
        {
            writer.WriteProperty(GetVersionedFieldName(specVersion, ArazzoConstants.ArazzoStepTimeout), Timeout.Value);
        }

        writer.WriteOptionalCollection(GetVersionedFieldName(specVersion, ArazzoConstants.ArazzoStepDependsOn), DependsOn, static (w, d) => w.WriteValue(d!));

        writer.WriteOptionalCollection(ArazzoConstants.ArazzoStepParameters, Parameters, (w, p) =>
        {
            if (p is not null)
            {
                callback(w, p);
            }
        });

        writer.WriteOptionalObject(
            ArazzoConstants.ArazzoStepRequestBody,
            RequestBody,
            callback);

        writer.WriteOptionalCollection(ArazzoConstants.ArazzoStepSuccessCriteria, SuccessCriteria, (w, c) =>
        {
            if (c is not null)
            {
                callback(w, c);
            }
        });

        writer.WriteOptionalCollection(ArazzoConstants.ArazzoStepOnSuccess, OnSuccess, (w, a) =>
        {
            if (a is not null)
            {
                callback(w, a);
            }
        });

        writer.WriteOptionalCollection(ArazzoConstants.ArazzoStepOnFailure, OnFailure, (w, a) =>
        {
            if (a is not null)
            {
                callback(w, a);
            }
        });

        if (OutputValues is not null)
        {
            writer.WriteOptionalMap(
                ArazzoConstants.ArazzoStepOutputs,
                OutputValues,
                static (w, v) => w.WriteAny(v));
        }
        else
        {
            writer.WriteOptionalMap(
                ArazzoConstants.ArazzoStepOutputs,
                Outputs,
                (w, v) => w.WriteValue(v));
        }

        writer.WriteArazzoExtensions(Extensions, specVersion);
        writer.WriteEndObject();
    }

    private void ValidateOperationReferenceFields(ArazzoSpecVersion specVersion)
    {
        var operationReferenceCount = CountTargetFields();

        if (operationReferenceCount > 1)
        {
            throw new ArazzoSerializationException(GetMultipleTargetFieldsError($"{nameof(ArazzoStep)} '{StepId}'", specVersion));
        }

        if (operationReferenceCount == 0)
        {
            throw new ArazzoSerializationException(GetMissingTargetFieldError($"{nameof(ArazzoStep)} '{StepId}'", specVersion));
        }
    }

    private void ValidateRequestBodyApplicability()
    {
        if (RequestBody is not null && !CanHaveRequestBody())
        {
            throw new ArazzoSerializationException($"{nameof(ArazzoStep)} '{StepId}' requestBody can only be specified when the step targets operationId or operationPath.");
        }
    }

    private void ValidateParameters()
    {
        ArazzoParameterValidator.ValidateSerializationParameters(
            Parameters,
            $"{nameof(ArazzoStep)} '{StepId}'",
            IsOperationTargeted(),
            "when the step targets an operation");
    }

    private bool IsOperationTargeted() =>
        !string.IsNullOrEmpty(OperationId) || !string.IsNullOrEmpty(OperationPath) || !string.IsNullOrEmpty(ChannelPath);

    internal int CountTargetFields()
    {
        var targetCount = 0;
        if (!string.IsNullOrEmpty(OperationId))
        {
            targetCount++;
        }

        if (!string.IsNullOrEmpty(OperationPath))
        {
            targetCount++;
        }

        if (!string.IsNullOrEmpty(ChannelPath))
        {
            targetCount++;
        }

        if (!string.IsNullOrEmpty(WorkflowId))
        {
            targetCount++;
        }

        return targetCount;
    }

    internal bool CanHaveRequestBody() =>
        IsOperationTargeted();

    internal static string GetMultipleTargetFieldsError(string elementName, ArazzoSpecVersion specVersion) =>
        specVersion is ArazzoSpecVersion.Arazzo1_0
            ? $"{elementName} can define only one of operationId, operationPath, channelPath, or workflowId. For Arazzo 1.0 compatibility, {elementName} can define only one of operationId, operationPath, or workflowId; channelPath uses x-channelPath."
            : $"{elementName} can define only one of {GetTargetFieldsDescription(specVersion)}.";

    internal static string GetMissingTargetFieldError(string elementName, ArazzoSpecVersion specVersion) =>
        specVersion is ArazzoSpecVersion.Arazzo1_0
            ? $"{elementName} must define exactly one of operationId, operationPath, channelPath, or workflowId. For Arazzo 1.0 compatibility, {elementName} must define exactly one of operationId, operationPath, or workflowId; channelPath uses x-channelPath."
            : $"{elementName} must define exactly one of {GetTargetFieldsDescription(specVersion)}.";

    private static string GetTargetFieldsDescription(ArazzoSpecVersion specVersion) =>
        specVersion is ArazzoSpecVersion.Arazzo1_0
            ? "operationId, operationPath, or workflowId"
            : "operationId, operationPath, channelPath, or workflowId";

    private void ValidateActions()
    {
        ArazzoActionListValidator.ValidateSerialization(OnSuccess, $"{nameof(ArazzoStep)} '{StepId}' onSuccess");
        ArazzoActionListValidator.ValidateSerialization(OnFailure, $"{nameof(ArazzoStep)} '{StepId}' onFailure");
    }

    private static string EscapePointerSegment(string segment)
    {
        return segment.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
    }

    private static string GetVersionedFieldName(ArazzoSpecVersion specVersion, string fieldName) =>
        specVersion is ArazzoSpecVersion.Arazzo1_0 ? ArazzoConstants.GetArazzo1_0ExtensionName(fieldName) : fieldName;
}