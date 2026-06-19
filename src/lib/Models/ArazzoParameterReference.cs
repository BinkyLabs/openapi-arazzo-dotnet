using System.Text.Json.Nodes;

using BinkyLabs.OpenApi.Arazzo.Validation;

using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Parameter reference object.
/// </summary>
public class ArazzoParameterReference : BaseArazzoReferenceHolder<ArazzoParameter, IArazzoParameter, BaseArazzoReference>, IArazzoParameter
{
    private JsonNode? _value;

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
        _value = ArazzoInput.CloneNode(reference._value);
    }

    /// <inheritdoc />
    public string? Name => Target?.Name;

    /// <inheritdoc />
    public ParameterLocation? In => Target?.In;

    /// <summary>
    /// Gets or sets the parameter value override applied to the referenced target.
    /// </summary>
    public JsonNode? Value
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
            Value = ArazzoInput.CloneNode(_value),
            Extensions = ArazzoInput.CloneArazzoExtensions(parameter.Extensions)
        };
    }

    /// <inheritdoc />
    public override void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArazzoReusableObjectReferenceValidator.ValidateSerializationReference(Reference.ReferenceV1, ReferenceType.Parameter, nameof(ArazzoParameterReference));

        writer.WriteStartObject();
        writer.WriteProperty(ArazzoConstants.ArazzoReusableObjectReference, Reference.ReferenceV1);
        writer.WriteOptionalObject(ArazzoConstants.ArazzoParameterValue, _value, static (w, value) => w.WriteAny(value));
        writer.WriteEndObject();
    }

    /// <inheritdoc />
    protected override BaseArazzoReference CopyReference(BaseArazzoReference sourceReference)
    {
        return new BaseArazzoReference(sourceReference);
    }
}