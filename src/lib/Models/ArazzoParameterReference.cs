using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Parameter reference object.
/// </summary>
public class ArazzoParameterReference : BaseArazzoReferenceHolder<ArazzoParameter, IArazzoParameter, BaseArazzoReference>, IArazzoParameter
{
    private ArazzoParameterValueUnion? _value;

    /// <summary>
    /// Constructor initializing the reference object.
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <param name="hostDocument">The host document.</param>
    /// <param name="externalResource">The external resource.</param>
    public ArazzoParameterReference(string referenceId, ArazzoDocument? hostDocument = null, string? externalResource = null)
        : base(referenceId, hostDocument, ReferenceType.Parameter, externalResource)
    {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="reference">The reference to copy.</param>
    internal ArazzoParameterReference(ArazzoParameterReference reference)
        : base(reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        _value = reference._value?.Clone();
    }

    /// <inheritdoc />
    public string? Name => Target?.Name;

    /// <inheritdoc />
    public ParameterLocation? In => Target?.In;

    /// <summary>
    /// Gets or sets the parameter value override applied to the referenced target.
    /// </summary>
    public ArazzoParameterValueUnion? Value
    {
        get => _value ?? Target?.Value;
        set => _value = value;
    }

    /// <inheritdoc />
    public override IArazzoParameter CopyReferenceAsTargetElementWithOverrides(IArazzoParameter source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (_value is null || source is not ArazzoParameter parameter)
        {
            return source;
        }

        return new ArazzoParameter
        {
            Name = parameter.Name,
            In = parameter.In,
            Value = _value.Clone(),
            Extensions = ArazzoInput.CloneArazzoExtensions(parameter.Extensions)
        };
    }

    /// <inheritdoc />
    public override void SerializeAsV1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_0, static (w, obj) => obj.SerializeAsV1(w));
    }

    /// <inheritdoc />
    public override void SerializeAsV1_1(IOpenApiWriter writer)
    {
        SerializeInternal(writer, ArazzoSpecVersion.Arazzo1_1, static (w, obj) => obj.SerializeAsV1_1(w));
    }

    private void SerializeInternal(IOpenApiWriter writer, ArazzoSpecVersion specVersion, Action<IOpenApiWriter, IArazzoSerializable> callback)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArazzoReusableObjectReferenceValidator.ValidateSerializationReference(Reference.ReferenceV1, ReferenceType.Parameter, nameof(ArazzoParameterReference), specVersion);

        writer.WriteStartObject();
        writer.WriteProperty(ArazzoConstants.ArazzoReusableObjectReference, Reference.ReferenceV1);
        // Unlike inline parameters, reusable value overrides allow literal objects in Arazzo 1.1.
        writer.WriteOptionalObject(ArazzoConstants.ArazzoParameterValue, _value?.AsPayloadValue(), callback);
        writer.WriteEndObject();
    }

    /// <inheritdoc />
    protected override BaseArazzoReference CopyReference(BaseArazzoReference sourceReference)
    {
        return new BaseArazzoReference(sourceReference);
    }
}