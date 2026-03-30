namespace CliInvoke.Helpers;

internal enum CancellationReason
{
    Timeout,
    RequestedCancellation,
    NotKnown
}