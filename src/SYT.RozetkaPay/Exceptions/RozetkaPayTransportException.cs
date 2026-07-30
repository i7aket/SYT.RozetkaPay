namespace SYT.RozetkaPay.Exceptions;

/// <summary>
/// A request failed before the provider's answer was read, so the outcome is unknown.
/// </summary>
/// <remarks>
/// <para>
/// The one failure that matters most and the one the SDK used to report worst. A timeout escaped as a
/// bare <see cref="TaskCanceledException"/>, outside this hierarchy entirely: a caller writing the
/// documented <c>catch (RozetkaPayException)</c> did not catch the single case that can leave money in
/// an unknown state, and it left their request handler as an unhandled 500 while a payment may exist.
/// </para>
/// <para>
/// Every other exception here answers "what did the provider say". This one says the provider may not
/// have been asked, or may have answered into a socket nobody was listening on. That is a different
/// question and it needs a different type.
/// </para>
/// <para>
/// A caller's own cancellation is <strong>not</strong> reported through this type. When the caller
/// cancels, their <see cref="OperationCanceledException"/> reaches them carrying their own token, which
/// is how they tell "I stopped this" from "this did not finish".
/// </para>
/// </remarks>
public class RozetkaPayTransportException : RozetkaPayException
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">What happened.</param>
    /// <param name="isTimeout">Whether the SDK's own timeout tripped.</param>
    /// <param name="attemptsDispatched">How many times the operation was handed to the transport.</param>
    /// <param name="innerException">The transport failure underneath.</param>
    public RozetkaPayTransportException(
        string message,
        bool isTimeout,
        int attemptsDispatched,
        Exception? innerException = null)
        : base(message, innerException!)
    {
        IsTimeout = isTimeout;
        AttemptsDispatched = attemptsDispatched;
    }

    /// <summary>
    /// Whether the SDK's configured timeout tripped, as opposed to the transport failing outright.
    /// </summary>
    public bool IsTimeout { get; }

    /// <summary>
    /// How many times this operation was handed to the transport before giving up.
    /// </summary>
    /// <remarks>
    /// More than one means the retry policy repeated it. For an idempotent read that is unremarkable;
    /// for a payment creation it is the number the caller needs in their log, because the provider's
    /// at-most-one-success guarantee is what stands between that number and a double charge.
    /// </remarks>
    public int AttemptsDispatched { get; }

    /// <summary>
    /// Whether the request may have reached the provider. Always <c>true</c>, deliberately.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The SDK cannot prove a request did not arrive. A timeout means it was sent and no answer came
    /// back; a socket failure may have happened before or after the bytes left. Reporting anything
    /// other than "assume it may have" would invite a caller to skip reconciliation on the strength of
    /// a guess.
    /// </para>
    /// <para>
    /// The property exists rather than being left implicit because the safe reading is not obvious, and
    /// a caller who has to infer it will infer the convenient one.
    /// </para>
    /// </remarks>
    public bool MayHaveReachedProvider => true;
}
