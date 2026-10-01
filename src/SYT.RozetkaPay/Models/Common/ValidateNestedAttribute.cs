namespace SYT.RozetkaPay.Models.Common;

/// <summary>
/// Marks a request property whose object — or each element of whose collection — is validated together with
/// the request that carries it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="System.ComponentModel.DataAnnotations.Validator"/> is not recursive: the annotations on a nested
/// type are never read unless something walks into it. The SDK walks only through properties carrying this
/// marker, deliberately not into every nested object, because the annotations on most nested types have
/// never been enforced — validating them wholesale would start rejecting requests earlier versions sent (for
/// example the empty <c>payment_method</c> a hosted batch carries).
/// </para>
/// <para>
/// <see cref="OwnAnnotations"/> <see langword="false"/> makes a property a pass-through: the walk descends into
/// the object only to follow <em>its</em> marked properties, and leaves the object's own annotations
/// unenforced, exactly as before. That is how <c>orders[].order_recipient</c> is validated without starting
/// to enforce <c>BatchOrder.api_key</c> and <c>description</c>, which 7.0.0 never checked.
/// </para>
/// <para>
/// A type whose own annotations the walk enforces is held to its schema's <c>required</c> list and length
/// limits by <c>NestedRequestValidationTests</c>, the same rule the top-level bodies follow.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
internal sealed class ValidateNestedAttribute : Attribute
{
    /// <summary>
    /// Whether the nested object's own annotations are enforced (<see langword="true"/>, the default) or the
    /// object is only walked through to reach its marked properties (<see langword="false"/>).
    /// </summary>
    public bool OwnAnnotations { get; init; } = true;
}
