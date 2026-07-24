using System.Security.Cryptography;
using System.Text;

namespace SYT.RozetkaPay.Security;

/// <summary>
/// Verifies RozetkaPay callback signatures using the algorithm published at
/// <see href="https://docs.rozetkapay.com/guides/callbacks/">docs.rozetkapay.com/guides/callbacks</see>:
/// <c>base64url_encode(sha1(password + base64url_encode(json_body) + password))</c>.
/// </summary>
/// <remarks>
/// <para>
/// The instance is immutable and thread-safe: it holds only the merchant password and creates its hash
/// primitives per call, so a single instance can serve every incoming callback.
/// </para>
/// <para>
/// SHA-1 is dictated by the RozetkaPay protocol. It is used here purely to reproduce the signature the
/// provider sends, and must not be swapped for SHA-256 or an HMAC construction: doing so would reject
/// every genuine callback. The comparison itself is constant-time and the password never leaves this
/// object, so the weakness of SHA-1 as a general-purpose hash is not amplified by this implementation.
/// </para>
/// </remarks>
public sealed class RozetkaPayWebhookSignatureVerifier : IRozetkaPayWebhookSignatureVerifier
{
    /// <summary>
    /// Name of the header RozetkaPay puts the callback signature in.
    /// </summary>
    public const string SignatureHeaderName = "X-ROZETKAPAY-SIGNATURE";

    /// <summary>Length of a SHA-1 digest in bytes.</summary>
    private const int Sha1DigestLength = 20;

    private readonly string _password;

    /// <summary>
    /// Create a verifier for the merchant password used for the payment operation. RozetkaPay signs each
    /// callback with the same password the payment was created with.
    /// </summary>
    /// <param name="password">The merchant API password. Used exactly as supplied and never trimmed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="password"/> is empty or contains only whitespace. This is a configuration error and
    /// surfaces immediately rather than turning every callback into a failed verification.
    /// </exception>
    public RozetkaPayWebhookSignatureVerifier(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (string.IsNullOrWhiteSpace(password))
        {
            // The message deliberately describes the problem without echoing the supplied value.
            throw new ArgumentException(
                "The RozetkaPay merchant password must not be empty or whitespace.",
                nameof(password));
        }

        // Stored verbatim: surrounding whitespace may legitimately be part of the secret.
        _password = password;
    }

    /// <inheritdoc />
    public bool Verify(ReadOnlyMemory<byte> payload, string? signature)
    {
        // A missing or malformed header is ordinary untrusted input, not an exceptional condition, so it
        // fails closed instead of throwing.
        if (signature is null)
        {
            return false;
        }

        Span<byte> supplied = stackalloc byte[Sha1DigestLength];
        if (!TryDecodeCanonicalBase64Url(signature, supplied, out int suppliedLength) ||
            suppliedLength != Sha1DigestLength)
        {
            return false;
        }

        Span<byte> expected = stackalloc byte[Sha1DigestLength];
        try
        {
            ComputeExpectedDigest(payload.Span, expected);
            return CryptographicOperations.FixedTimeEquals(expected, supplied);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(expected);
        }
    }

    /// <inheritdoc />
    public bool Verify(string payload, string? signature)
    {
        ArgumentNullException.ThrowIfNull(payload);

        // UTF-8 without a BOM, matching the encoding RozetkaPay signs the body with.
        return Verify(Encoding.UTF8.GetBytes(payload), signature);
    }

    /// <summary>
    /// Compute <c>sha1(password + base64url(payload) + password)</c> over the raw callback bytes.
    /// </summary>
    private void ComputeExpectedDigest(ReadOnlySpan<byte> payload, Span<byte> destination)
    {
        // Standard Base64 translated to the URL-safe alphabet with the '=' padding kept. The padding is
        // part of what RozetkaPay hashes, so Base64Url.EncodeToString (which omits it) cannot be used.
        string inner = Convert.ToBase64String(payload)
            .Replace('+', '-')
            .Replace('/', '_');

        // The inner encoding is Base64, hence pure ASCII.
        byte[] innerBytes = Encoding.ASCII.GetBytes(inner);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(_password);

        try
        {
            // Hashed incrementally so the concatenation - which contains the secret twice - is never
            // materialised as a single buffer. No delimiter, newline, or normalisation is inserted.
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
            hash.AppendData(passwordBytes);
            hash.AppendData(innerBytes);
            hash.AppendData(passwordBytes);
            hash.GetHashAndReset(destination);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    /// <summary>
    /// Decode a base64url string, accepting only the single canonical encoding of its bytes.
    /// </summary>
    /// <remarks>
    /// Deliberately stricter than <see cref="Convert.FromBase64String(string)"/>, which silently skips
    /// whitespace and ignores the unused trailing bits of the final character. Both quirks let an attacker
    /// mint several distinct header values that decode to a genuine digest, so each is rejected here:
    /// only the exact string RozetkaPay would send is accepted.
    /// </remarks>
    /// <param name="value">The candidate signature.</param>
    /// <param name="destination">Buffer receiving the decoded bytes.</param>
    /// <param name="bytesWritten">Number of bytes decoded, or zero when decoding failed.</param>
    /// <returns><see langword="true"/> when <paramref name="value"/> is canonical base64url.</returns>
    private static bool TryDecodeCanonicalBase64Url(
        string value,
        Span<byte> destination,
        out int bytesWritten)
    {
        bytesWritten = 0;

        // Canonical Base64 always arrives in whole four-character quanta.
        if (value.Length == 0 || value.Length % 4 != 0)
        {
            return false;
        }

        int padding = 0;
        if (value[^1] == '=')
        {
            padding = value[^2] == '=' ? 2 : 1;
        }

        int dataLength = value.Length - padding;
        if (dataLength == 0)
        {
            return false;
        }

        int decodedLength = (value.Length / 4 * 3) - padding;
        if (decodedLength > destination.Length)
        {
            return false;
        }

        int accumulator = 0;
        int accumulatedBits = 0;
        int written = 0;

        for (int index = 0; index < dataLength; index++)
        {
            // Rejects '+', '/', '=' in a non-padding position, whitespace, and anything else outside the
            // URL-safe alphabet.
            int sextet = DecodeBase64UrlCharacter(value[index]);
            if (sextet < 0)
            {
                return false;
            }

            accumulator = (accumulator << 6) | sextet;
            accumulatedBits += 6;

            if (accumulatedBits >= 8)
            {
                accumulatedBits -= 8;
                destination[written++] = (byte)(accumulator >> accumulatedBits);
                accumulator &= (1 << accumulatedBits) - 1;
            }
        }

        // Leftover bits of the final character must be zero; otherwise several encodings would map to the
        // same bytes and the signature would be malleable.
        if (accumulator != 0 || written != decodedLength)
        {
            return false;
        }

        bytesWritten = written;
        return true;
    }

    /// <summary>
    /// Map one base64url character to its 6-bit value, or <c>-1</c> when it is not in the alphabet.
    /// </summary>
    private static int DecodeBase64UrlCharacter(char character)
    {
        return character switch
        {
            >= 'A' and <= 'Z' => character - 'A',
            >= 'a' and <= 'z' => character - 'a' + 26,
            >= '0' and <= '9' => character - '0' + 52,
            '-' => 62,
            '_' => 63,
            _ => -1
        };
    }
}
