using DotExtensions.Dates;

namespace CliInvoke.Helpers.Processes.Cancellation;

internal static class ForcefulCancellation
{
    /// <param name="process"></param>
    extension(Process process)
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutThreshold"></param>
        /// <param name="cancellationExceptionBehavior"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("windows")]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
        [SupportedOSPlatform("android")]
        internal async Task WaitForExitOrForcefulTimeoutAsync(TimeSpan timeoutThreshold,
            ProcessCancellationExceptionBehavior cancellationExceptionBehavior)
        {
            if (timeoutThreshold < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException();

            DateTime expectedExitTime = DateTime.UtcNow.Add(timeoutThreshold);

            try
            {
                Task waitForExit = process.WaitForExitAsync();

                Task delay = Task.Delay(timeoutThreshold);

                await Task.WhenAny(delay, waitForExit);

                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch (Exception)
            {
                DateTime actualExitTime = DateTime.UtcNow;
                TimeSpan difference = expectedExitTime.Difference(actualExitTime);

                if (cancellationExceptionBehavior
                    == ProcessCancellationExceptionBehavior.SuppressException)
                {
                    return;
                }
                if (cancellationExceptionBehavior
                    == ProcessCancellationExceptionBehavior.AllowExceptionIfUnexpected
                    || cancellationExceptionBehavior
                    == ProcessCancellationExceptionBehavior.AllowException)
                {
                    if (
                        difference > TimeSpan.FromSeconds(10)
                        || cancellationExceptionBehavior
                        == ProcessCancellationExceptionBehavior.AllowException
                    )
                    {
                        throw;
                    }
                }
            }
        }
    }
}