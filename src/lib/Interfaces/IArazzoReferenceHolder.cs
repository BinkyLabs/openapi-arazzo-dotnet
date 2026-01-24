namespace BinkyLabs.OpenApi.Arazzo;
/// <summary>
/// A generic interface for OpenApiReferenceable objects that have a target.
/// </summary>
/// <typeparam name="T">The type of the target being referenced</typeparam>
/// <typeparam name="U">The type of the interface implemented by both the target and the reference type</typeparam>
/// <typeparam name="V">The type for the reference holding the additional fields and annotations</typeparam>
public interface IArazzoReferenceHolder<out T, U, V> : IArazzoReferenceHolder<V> where T : IArazzoReferenceable, U where V : BaseArazzoReference, new()
{
    /// <summary>
    /// Gets the resolved target object.
    /// </summary>
    U? Target { get; }

    /// <summary>
    /// Gets the recursively resolved target object.
    /// </summary>
    T? RecursiveTarget { get; }

    /// <summary>
    /// Copy the reference as a target element with overrides.
    /// </summary>
    U CopyReferenceAsTargetElementWithOverrides(U source);
}
/// <summary>
/// A generic interface for OpenApiReferenceable objects that have a target.
/// </summary>
/// <typeparam name="V">The type for the reference holding the additional fields and annotations</typeparam>
public interface IArazzoReferenceHolder<V> : IArazzoReferenceHolder where V : BaseArazzoReference, new()
{
    /// <summary>
    /// Reference object.
    /// </summary>
    V Reference { get; init; }
}
/// <summary>
/// A generic interface for OpenApiReferenceable objects that have a target.
/// </summary>
public interface IArazzoReferenceHolder : IArazzoSerializable
{
    /// <summary>
    /// Indicates if object is populated with data or is just a reference to the data
    /// </summary>
    bool UnresolvedReference { get; }
}