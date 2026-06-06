using Microsoft.OpenApi;

namespace BinkyLabs.OpenApi.Arazzo;

/// <summary>
/// Base class for Arazzo reference holders.
/// </summary>
/// <typeparam name="T">The concrete class implementation type for the model.</typeparam>
/// <typeparam name="U">The interface type for the model.</typeparam>
/// <typeparam name="V">The type for the reference holding the additional fields and annotations.</typeparam>
internal abstract class BaseArazzoReferenceHolder<T, U, V> : IArazzoReferenceHolder<T, U, V>
    where T : class, IArazzoReferenceable, U
    where U : IArazzoReferenceable, IArazzoSerializable
    where V : BaseArazzoReference, new()
{
    /// <inheritdoc/>
    public virtual U? Target
    {
        get
        {
            if (Reference.HostDocument is null)
            {
                return default;
            }
            return Reference.HostDocument.ResolveReferenceTo<U>(Reference, this as IArazzoInput);
        }
    }

    /// <inheritdoc/>
    public T? RecursiveTarget => ResolveRecursiveTarget(new HashSet<BaseArazzoReferenceHolder<T, U, V>>());

    private T? ResolveRecursiveTarget(ISet<BaseArazzoReferenceHolder<T, U, V>> visitedReferences)
    {
        if (!visitedReferences.Add(this))
        {
            throw new InvalidOperationException($"Circular reference detected while resolving reference: {Reference.ReferenceV1}");
        }

        return Target switch
        {
            BaseArazzoReferenceHolder<T, U, V> recursiveTarget => recursiveTarget.ResolveRecursiveTarget(visitedReferences),
            T concrete => concrete,
            _ => null
        };
    }

    /// <summary>
    /// Copy the reference as a target element with overrides.
    /// </summary>
    /// <param name="sourceReference">The source reference to copy.</param>
    /// <returns>The copy of the reference.</returns>
    protected abstract V CopyReference(V sourceReference);

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="source">The reference holder to copy.</param>
    protected BaseArazzoReferenceHolder(BaseArazzoReferenceHolder<T, U, V> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Reference = CopyReference(source.Reference);
    }

    /// <summary>
    /// Constructor initializing the reference object.
    /// </summary>
    /// <param name="referenceId">The reference identifier.</param>
    /// <param name="hostDocument">The host Arazzo document.</param>
    /// <param name="referenceType">The reference type.</param>
    /// <param name="externalResource">The external resource when present.</param>
    protected BaseArazzoReferenceHolder(string referenceId, ArazzoDocument? hostDocument, ReferenceType referenceType, string? externalResource)
    {
        ArgumentException.ThrowIfNullOrEmpty(referenceId);

        Reference = new V
        {
            Id = referenceId,
            HostDocument = hostDocument,
            Type = referenceType,
            ExternalResource = externalResource
        };
    }

    /// <inheritdoc/>
    public bool UnresolvedReference => Reference is null || Target is null;

    /// <inheritdoc/>
    public V Reference { get; init; } = new();

    /// <inheritdoc/>
    public abstract U CopyReferenceAsTargetElementWithOverrides(U source);

    /// <inheritdoc/>
    public virtual void SerializeAsV1(IOpenApiWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Reference.SerializeAsV1(writer);
    }
}