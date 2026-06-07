using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Failure action reference object.
/// </summary>
public class ArazzoFailureActionReference : BaseArazzoReferenceHolder<ArazzoFailureAction, IArazzoFailureAction, BaseArazzoReference>, IArazzoFailureAction
{
    /// <summary>
    /// Constructor initializing the reference object.
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <param name="hostDocument">The host document.</param>
    /// <param name="externalResource">The external resource.</param>
    public ArazzoFailureActionReference(string referenceId, ArazzoDocument? hostDocument = null, string? externalResource = null)
        : base(referenceId, hostDocument, ReferenceType.FailureAction, externalResource)
    {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="reference">The reference to copy.</param>
    internal ArazzoFailureActionReference(ArazzoFailureActionReference reference)
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
    public ArazzoFailureType? Type => Target?.Type;

    /// <inheritdoc />
    public decimal? RetryAfter => Target?.RetryAfter;

    /// <inheritdoc />
    public ulong? RetryLimit => Target?.RetryLimit;

    /// <inheritdoc />
    public override IArazzoFailureAction CopyReferenceAsTargetElementWithOverrides(IArazzoFailureAction source)
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