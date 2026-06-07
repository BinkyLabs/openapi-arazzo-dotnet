using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Success action reference object.
/// </summary>
public class ArazzoSuccessActionReference : BaseArazzoReferenceHolder<ArazzoSuccessAction, IArazzoSuccessAction, BaseArazzoReference>, IArazzoSuccessAction
{
    /// <summary>
    /// Constructor initializing the reference object.
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <param name="hostDocument">The host document.</param>
    /// <param name="externalResource">The external resource.</param>
    public ArazzoSuccessActionReference(string referenceId, ArazzoDocument? hostDocument = null, string? externalResource = null)
        : base(referenceId, hostDocument, ReferenceType.SuccessAction, externalResource)
    {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="reference">The reference to copy.</param>
    internal ArazzoSuccessActionReference(ArazzoSuccessActionReference reference)
        : base(reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
    }

    /// <inheritdoc />
    public string? Name => Target?.Name;

    /// <inheritdoc />
    public string? WorkflowId => Target?.WorkflowId;

    /// <inheritdoc />
    public string? StepId => Target?.StepId;

    /// <inheritdoc />
    public IList<ArazzoCriterion>? Criteria => Target?.Criteria;

    /// <inheritdoc />
    public ArazzoSuccessType? Type => Target?.Type;

    /// <inheritdoc />
    public override IArazzoSuccessAction CopyReferenceAsTargetElementWithOverrides(IArazzoSuccessAction source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source;
    }

    /// <inheritdoc />
    protected override BaseArazzoReference CopyReference(BaseArazzoReference sourceReference)
    {
        return new BaseArazzoReference(sourceReference);
    }
}