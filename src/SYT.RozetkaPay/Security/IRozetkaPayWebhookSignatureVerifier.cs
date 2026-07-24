namespace SYT.RozetkaPay.Security;

/// <summary>
/// Verifies that a RozetkaPay callback really came from RozetkaPay, by checking the
/// <c>X-ROZETKAPAY-SIGNATURE</c> header against the raw request body.
/// </summary>
public interface IRozetkaPayWebhookSignatureVerifier
{
    /// <summary>
    /// Verify a callback signature against the exact bytes RozetkaPay sent.
    /// </summary>
    /// <param name="payload">
    /// The raw, unmodified request body. This must be the bytes read straight off the wire: never a
    /// re-serialized version of a parsed object, because whitespace and property order are part of what
    /// was signed.
    /// </param>
    /// <param name="signature">
    /// The value of the <c>X-ROZETKAPAY-SIGNATURE</c> header, or <see langword="null"/> when the header
    /// is absent.
    /// </param>
    /// <returns>
    /// <see langword="true"/> only when the signature matches; <see langword="false"/> for a missing,
    /// malformed, or incorrect signature.
    /// </returns>
    bool Verify(ReadOnlyMemory<byte> payload, string? signature);

    /// <summary>
    /// Verify a callback signature against a body that has already been decoded to a string.
    /// </summary>
    /// <param name="payload">
    /// The raw request body, encoded as UTF-8 for hashing. Pass the body exactly as received; parsing and
    /// re-serializing the JSON changes the bytes and invalidates the signature.
    /// </param>
    /// <param name="signature">
    /// The value of the <c>X-ROZETKAPAY-SIGNATURE</c> header, or <see langword="null"/> when the header
    /// is absent.
    /// </param>
    /// <returns>
    /// <see langword="true"/> only when the signature matches; <see langword="false"/> for a missing,
    /// malformed, or incorrect signature.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    bool Verify(string payload, string? signature);
}
