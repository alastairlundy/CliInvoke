/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using CliInvoke.Processes.Internal.Cancellation;
using CliInvoke.Processes.Internal.ControlAdapters;

namespace CliInvoke.Processes.Internal;

internal class ProcessWrapper : Process
{
    /// <summary>
    /// Computes how long, in seconds, the graceful cancellation path should wait after the
    /// interrupt signal has been sent before giving up and falling back to
    /// (optionally) a forceful exit.
    /// </summary>
    /// <param name="timeoutSeconds">The user-supplied timeout threshold, in whole seconds.</param>
    /// <returns>
    /// <c>min(10 + floor(timeoutSeconds * 0.05), 20)</c>, i.e. a fixed 10s base plus 5% of the
    /// requested timeout (rounded down to an integer), capped at 20s.
    /// </returns>
    internal static int CalculatePostInterruptGracePeriodSeconds(int timeoutSeconds)
    {
        int waitSeconds = 10 + (int)Math.Floor(timeoutSeconds * 0.05);
        return Math.Min(waitSeconds, 20);
    }

    /// <summary>
    /// Computes the total maximum time, in seconds, that a graceful cancellation may take.
    /// This includes the initial timeout before the interrupt is sent, plus the grace period
    /// after the interrupt.
    /// </summary>
    /// <param name="timeoutSeconds">The user-supplied timeout threshold, in whole seconds.</param>
    /// <returns>
    /// <c>timeoutSeconds + CalculatePostInterruptGracePeriodSeconds(timeoutSeconds)</c>.
    /// </returns>
    internal static int CalculateGracefulTimeoutWaitSeconds(int timeoutSeconds)
    {
        return timeoutSeconds + CalculatePostInterruptGracePeriodSeconds(timeoutSeconds);
    }

    // Synchronisation primitive to prevent simultaneous cancellation attempts
    private readonly SemaphoreSlim _cancellationSemaphore = new(1, 1);

    // Guards ExitTime: written once by the Exited event callback (OnExited) or, when that
    // callback has not run yet, by the wait paths' fallback (EnsureExitTime). Single-assignment
    // under this lock keeps readers from ever observing default(DateTime) or a torn value.
    private readonly object _exitTimeSync = new();

    // Resolved cancellation reason, persisted across the wait so Cancelled can be computed afterward.
    private CancellationReason _cancellationReason = CancellationReason.NotKnown;

#if NET11_0
    // True while a process created suspended via ProcessStartInfo.StartSuspended
    // (net11.0 leg, Windows/macOS only — set in Start()) is still awaiting its initial
    // release. This is a one-shot: ResumeProcess consumes it when it delivers the
    // creation-suspended release through SafeProcessHandle.Resume(); every resume after
    // that takes the per-OS control-adapter route, which resumes all threads and keeps
    // the public Suspend/Resume pair symmetric. Also tells OnStarted that no post-start
    // suspend step is needed.
    private bool _awaitingInitialResume;
#endif

    internal ProcessWrapper(ProcessConfiguration configuration,
        FileInfo resolvedFilePath)
    {
        ProcessControlAdapter = ProcessControlAdapterFactory.Create();
        ResourcePolicy = configuration.ResourcePolicy;
        ProcessControlAdapter.ApplyConfiguration(this, configuration);
        StartInfo.FileName = resolvedFilePath.FullName;

        // Validated here rather than in ApplyConfiguration: the resolved absolute path can be
        // substantially longer than the unresolved TargetFilePath (PATH lookup / recursion),
        // so measuring before this overwrite would under-count the real command line.
        if (OperatingSystem.IsWindows())
            BaseProcessControlAdapter.ValidateCommandLineLength(StartInfo);

        ProcessName = StartInfo.FileName;
        EnableRaisingEvents = true;
        Exited += OnExited;
        Started += OnStarted;

        HasStarted = false;
    }
    
    internal BaseProcessControlAdapter ProcessControlAdapter { get; }

    internal ProcessResourcePolicy ResourcePolicy { get; }

    internal bool HasStarted { get; private set; }

    internal new DateTime StartTime { get; private set; }

    internal new DateTime ExitTime { get; private set; }

    internal new int Id { get; private set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    internal new string ProcessName { get; private set; }

    /// <summary>
    ///     A value indicating whether the library terminated the process via its cancellation
    ///     machinery (timeout or requested cancellation) rather than the process exiting on its own.
    /// </summary>
    internal bool Canceled =>
        _cancellationReason is CancellationReason.Timeout or CancellationReason.RequestedCancellation;

    // The child's stdin writer, cached during Start() for the same reason as the
    // StandardOutput/StandardError readers below: property access throws post-exit on
    // .NET 10 for fast-exiting processes. Closed by PipeStandardInputAsync to deliver
    // EOF to stdin-reading children.
    private StreamWriter? _cachedStdInWriter;

    /// <summary>
    ///     The POSIX signal that terminated the process (Unix only), obtained via the control adapter.
    /// </summary>
    /// <remarks>
    ///     On the net11.0 leg this reads the runtime's exit-status introspection
    ///     (<c>TryWaitForExitStatus</c>) instead of the adapter's 128+n exit-code heuristic.
    ///     The runtime casts the raw positive native signal number (<c>WTERMSIG</c>) into
    ///     <see cref="PosixSignal"/>, so before any check, positive raw values are converted
    ///     through the adapter's platform-specific signal-number mapping; the SIGKILL→null
    ///     and unknown-signal→null contracts are preserved so both legs report identical
    ///     <see cref="ProcessResult"/> values.
    /// </remarks>
    // On net11.0 the base Process type gains a Signal(PosixSignal) method; this
    // terminating-signal property intentionally keeps its name, so the hide
    // warning is suppressed (the net10.0 base has no Signal member at all).
#pragma warning disable CS0108
    internal PosixSignal? Signal
#pragma warning restore CS0108
    {
        get
        {
#if NET11_0
            // TimeSpan.Zero makes this a non-blocking status check. Not-yet-exited (or
            // un-started) falls through to the adapter path below, which reads ExitCode
            // and throws InvalidOperationException exactly like the net10.0 leg.
            bool hasStatus;
#pragma warning disable CA1416 // TryWaitForExitStatus is marked ios/tvos-unsupported; this internal property is only reached from ios/tvos-annotated wait paths.
            hasStatus = TryWaitForExitStatus(TimeSpan.Zero, out ProcessExitStatus? exitStatus);
#pragma warning restore CA1416
            if (hasStatus && exitStatus is not null)
            {
                PosixSignal? signal = exitStatus.Signal;

                // The runtime casts the raw positive WTERMSIG into PosixSignal, but BCL members
                // are negative sentinels (SIGTERM = -4), so a raw positive value can never equal
                // a member. Map it through the adapter (Linux vs BSD numbering) BEFORE the
                // SIGKILL/undefined checks below; negative values pass through untouched.
                if (signal is not null && (int)signal.Value > 0)
                {
                    signal = ProcessControlAdapter.MapNativeSignalNumber((int)signal.Value);
                }

                // Parity with the net10.0 heuristic: SIGKILL and undefined values both map to
                // null (see the property remarks).
#pragma warning disable CA1416 // PosixSignal members are unix-flavoured; Signal is always null on Windows so this comparison never fires there.
                if (signal is PosixSignal.SIGKILL)
                    return null;
#pragma warning restore CA1416

                if (signal is not null && !Enum.IsDefined(signal.Value))
                    return null;

                return signal;
            }

            return ProcessControlAdapter.GetTerminatingSignal(ExitCode);
#else
            return ProcessControlAdapter.GetTerminatingSignal(ExitCode);
#endif
        }
    }

    
    private void OnStarted(object? sender, EventArgs e)
    {
        // ReSharper disable once InvertIf
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
            || OperatingSystem.IsFreeBSD())
        {
            // Fast-exiting processes (e.g. `which`, `echo`) may have already exited
            // between base.Start() returning and this handler running. Skip the
            // suspend/resume cycle in that case to avoid races on process handles.
            if (HasExited) return;

#if NET11_0
            // On the net11.0 leg, Windows/macOS processes are created suspended via
            // ProcessStartInfo.StartSuspended (see Start()), so they cannot have run yet
            // and no suspend step is needed here — the process is simply resumed below
            // after the resource policy has been applied. Linux/FreeBSD (StartSuspended
            // is unsupported there) and shell-executed starts (StartSuspended is
            // incompatible with UseShellExecute) fall through to the legacy
            // suspend → apply → resume cycle.
            if (!_awaitingInitialResume)
#endif
            {
                try
                {
                    SuspendProcess();
                }
                catch (InvalidOperationException)
                {
                    // Process exited before we could suspend it.
                    return;
                }
                catch (Win32Exception exception) when (IsErrnoNoSuchProcess(exception))
                {
                    // Adapter suspend signals a just-exited child; errno ESRCH means it is
                    // gone. Same race as the exiting-before-suspend case above.
                    return;
                }
            }

            try
            {
#pragma warning disable CA1416
                ProcessControlAdapter.SetResourcePolicy(this, ResourcePolicy);
#pragma warning restore CA1416
            }
            catch (InvalidOperationException)
            {
                // Process exited before we could set the resource policy.
            }
            catch (Win32Exception exception) when (IsErrnoNoSuchProcess(exception))
            {
                // PriorityClass and working-set setters translate libc ESRCH into
                // Win32Exception ("No such process") when the fast-exiting child is
                // already reaped (observed on macOS arm64 runners). Unrelated access
                // failures keep surfacing.
            }
            finally
            {
                // Always resume the process — even if SetResourcePolicy or a raced
                // child exit throws — so it cannot stay suspended.
                try
                {
                    ResumeProcess();
                }
                catch (InvalidOperationException)
                {
                    // The process may have already exited during SetResourcePolicy.
                    // Swallow the exception — the process is gone, nothing to resume.
                }
                catch (Win32Exception exception) when (IsErrnoNoSuchProcess(exception))
                {
                    // Same as above: the child exited mid-sequence.
                }
#if NET11_0
                catch (Win32Exception)
                {
                    // SafeProcessHandle.Resume() (net11.0, Windows/macOS) reports OS
                    // failures as Win32Exception. Only the exit race is benign: when
                    // the child is already gone there is nothing to resume. A live
                    // child whose resume failed would stay creation-suspended forever,
                    // so terminate it and surface the failure to the caller instead.
                    if (!HasExited)
                    {
                        try
                        {
                            ForcefulExit();
                        }
                        catch (Exception)
                        {
                            // Best-effort kill; swallow any exception to avoid masking
                            // the resume failure rethrown below.
                        }

                        throw;
                    }
                }
#endif
            }
        }
    }

    /// <summary>
    ///     Matches Linux, macOS, and FreeBSD <c>Win32Exception</c> translations of
    ///     errno <c>ESRCH</c> (3), which mean the child process is gone. Windows maps
    ///     its own error numbers into the same exception type (3 is
    ///     <c>ERROR_PATH_NOT_FOUND</c>), so Windows never matches here.
    /// </summary>
    private static bool IsErrnoNoSuchProcess(Win32Exception exception) =>
        !OperatingSystem.IsWindows()
        && exception.NativeErrorCode == 3;

    private void OnExited(object? sender, EventArgs e)
    {
        // base.ExitTime is local time; store UTC to match StartTime (DateTime.UtcNow).
        DateTime exitTime = base.ExitTime.ToUniversalTime();

        lock (_exitTimeSync)
        {
            // Single assignment: when a wait path already published the fallback value
            // first, keep it rather than racing a second write.
            if (ExitTime == default)
                ExitTime = exitTime;
        }
    }

    /// <summary>
    ///     Guarantees <see cref="ExitTime"/> holds a real timestamp once the process is known
    ///     to have exited. The Exited event callback that normally assigns it can run after
    ///     <see cref="Process.HasExited"/> flips true, so wait paths call this before returning;
    ///     otherwise readers (e.g. the <c>ProcessResult</c> built by ExternalProcess) race the
    ///     callback and can observe <c>default</c>, producing a bogus negative RuntimeDuration.
    ///     No-ops while the process is still running, so it is safe to call from clean-up paths.
    /// </summary>
    private void EnsureExitTime()
    {
        if (ExitTime != default)
            return;

        bool exited;
        try
        {
            exited = HasExited;
        }
        catch (ObjectDisposedException)
        {
            // Wait paths treat a disposed process as exited.
            exited = true;
        }
        catch (InvalidOperationException)
        {
            // No process associated; nothing to publish yet.
            exited = false;
        }

        if (!exited)
            return;

        lock (_exitTimeSync)
        {
            if (ExitTime != default)
                return;

            try
            {
                ExitTime = base.ExitTime.ToUniversalTime();
            }
            catch (ObjectDisposedException)
            {
                // Process handle already released (disposed externally); the wait paths treat
                // that as exited, so the best available timestamp is now.
                ExitTime = DateTime.UtcNow;
            }
            catch (InvalidOperationException)
            {
                ExitTime = DateTime.UtcNow;
            }
        }
    }

    internal event EventHandler Started;

    /// <summary>
    ///     Maps a <see cref="Win32Exception"/> thrown during process start to the
    ///     natural .NET exception type for the given <see cref="Win32Exception.NativeErrorCode"/>.
    /// </summary>
    /// <remarks>
    ///     Known codes: 2/3 → <see cref="FileNotFoundException"/>,
    ///     5 → <see cref="UnauthorizedAccessException"/>,
    ///     193 → <see cref="BadImageFormatException"/>.
    ///     All other codes rethrow the original <see cref="Win32Exception"/>, preserving its
    ///     <see cref="Win32Exception.NativeErrorCode"/> so callers can catch it specifically.
    /// </remarks>
    internal static Exception MapWin32ExceptionToStartFailureException(
        Win32Exception exception,
        string targetFilePath)
    {
        return exception.NativeErrorCode switch
        {
            // ERROR_FILE_NOT_FOUND (2) or ERROR_PATH_NOT_FOUND (3)
            2 or 3 =>
                new FileNotFoundException(
                    $"The file '{targetFilePath}' was not found.",
                    targetFilePath,
                    exception),

            // ERROR_ACCESS_DENIED (5)
            5 =>
                new UnauthorizedAccessException(
                    $"The current user does not have permission to execute the file '{targetFilePath}'.",
                    exception),

            // ERROR_BAD_EXE_FORMAT (193)
            193 =>
                new BadImageFormatException(
                    $"The file '{targetFilePath}' is not a valid executable image.",
                    exception),

            // Unknown codes — rethrow the original so callers can catch Win32Exception
            // and inspect NativeErrorCode rather than being forced into catch (Exception).
            _ => exception,
        };
    }

    public new bool Start()
    {
#if NET11_0
        // net11.0 leg, Windows/macOS ONLY: StartSuspended is annotated for those two
        // platforms and the runtime throws PlatformNotSupportedException elsewhere —
        // Linux/FreeBSD have no create-suspended OS primitive — so the property is never
        // even assigned on those platforms. Shell-executed starts are excluded because
        // StartSuspended cannot be combined with UseShellExecute (the runtime throws);
        // those keep the legacy suspend → apply → resume cycle in OnStarted, as do
        // Linux/FreeBSD on every start.
        if ((OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            && !StartInfo.UseShellExecute)
        {
            StartInfo.StartSuspended = true;
            _awaitingInitialResume = true;
        }
#endif
        try
        {
            HasStarted = base.Start();
        }
        catch(Win32Exception exception)
        {
            HasStarted = false;

            throw MapWin32ExceptionToStartFailureException(exception, StartInfo.FileName);
        }

        if (!HasStarted)
        {
            throw new InvalidOperationException($"Process with Target File Name of '{StartInfo.FileName}' could not be started.");
        }

        // Cache StandardOutput/StandardError StreamReaders and the child's stdin writer
        // while the process is still guaranteed alive. These properties internally call
        // EnsureState, which in .NET 10 throws InvalidOperationException("process has
        // exited") on fast-exiting processes (e.g. `which dotnet`) that exit before the
        // next line of code runs. Touching them here means downstream code that reads
        // from the cached readers — or closes the cached stdin writer — works even if
        // the process has already exited.
        try
        {
            _ = StandardOutput;
            _ = StandardError;
            _cachedStdInWriter = StandardInput;
        }
        catch (InvalidOperationException)
        {
            // Process exited before we could cache the stream readers.
            // Downstream code must tolerate an exited process (see HasExited guards).
        }

        // Capture Id (safe after Start) and ProcessName (throws if exited).
        // base.Id does not throw on exit; base.ProcessName does.
        Id = base.Id;

        // Fast-exiting processes (e.g. `which dotnet`) can exit between the
        // HasExited check and the base.ProcessName call, so guard with a
        // try/catch as well. Fall back to StartInfo.FileName in that case.
        try
        {
            ProcessName = base.ProcessName;
        }
        catch (InvalidOperationException)
        {
            ProcessName = StartInfo.FileName;
        }

        StartTime = DateTime.UtcNow;
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        Started?.Invoke(this, EventArgs.Empty);

        return HasStarted;
    }

    /// <summary>
    /// Suspends the current process. Routes to the platform-specific implementation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an attempt is made to suspend a process that has already exited.
    /// </exception>
    /// <remarks>
    /// This method uses platform-specific mechanisms to suspend a process and is supported
    /// on Windows, macOS, Linux, and FreeBSD. It is not supported on iOS, tvOS, or browser platforms.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    internal void SuspendProcess()
    {
        if (HasExited)
            throw new InvalidOperationException(Resources.Exceptions_Process_Suspension_CannotSuspendExited);

        ProcessControlAdapter.SuspendProcess(this);
    }

    /// <summary>
    /// Resumes the execution of the current process. Routes to the platform-specific implementation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an attempt is made to resume a process that has already exited.
    /// </exception>
    /// <remarks>
    /// This method uses platform-specific mechanisms to resume a suspended process
    /// and is supported on Windows, macOS, Linux, and FreeBSD. It is not supported on iOS, tvOS, or browser platforms.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    internal void ResumeProcess()
    {
        if (HasExited)
            throw new InvalidOperationException(Resources.Exceptions_Process_CannotResumeExited);

#if NET11_0
        // One-shot: the creation-suspended release resumes via SafeProcessHandle
        // (ResumeThread on Windows, SIGCONT on macOS); every later resume takes the
        // adapter route so all threads resume. The flag stays set if the resume
        // throws, so the next call retries. Shell-exec starts and Linux/FreeBSD
        // never set it and keep the adapter route throughout.
        if (_awaitingInitialResume && (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()))
        {
            SafeHandle.Resume();
            _awaitingInitialResume = false;
            return;
        }
#endif

        ProcessControlAdapter.ResumeProcess(this);
    }

    #region Piping Standard Inputs and Outputs
    /// <summary>
    ///     Asynchronously pipes the standard input from a source stream to a specified process,
    ///     then closes the child's stdin write end so the child receives an end-of-file signal
    ///     on every exit path of the piping (successful copy, cancellation, or source-stream error).
    /// </summary>
    /// <remarks>
    ///     The close targets only the child's pipe write end; the caller-owned source stream is
    ///     never closed or disposed by this method. All exceptions propagate to the caller.
    /// </remarks>
    /// <param name="source">The stream from which to read the standard input data.</param>
    /// <param name="cancellationToken">A cancellation token that aborts the copy.</param>
    /// <returns>A task that represents the asynchronous piping operation.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    internal async Task PipeStandardInputAsync(Stream source,
        CancellationToken cancellationToken)
    {
        if (StartInfo.RedirectStandardInput)
        {
            try
            {
                await source.CopyToAsync(StandardInput.BaseStream, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // In the finally so EOF is delivered on every exit path; see remarks above.
                _cachedStdInWriter?.Close();
            }
        }
    }
    
    /// <summary>
    ///     Asynchronously retrieves the standard output stream from a specified process.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation, containing the standard output stream.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    internal async Task<Stream> PipeStandardOutputAsync(CancellationToken cancellationToken)
    {
        Stream destination = new MemoryStream();
        bool completed = false;

        try
        {
            if (StartInfo.RedirectStandardOutput)
                if (StandardOutput != StreamReader.Null)
                    await StandardOutput.BaseStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

            destination.Position = 0;
            completed = true;
            return destination;
        }
        finally
        {
            // On the fault path only: on success the caller owns the returned stream.
            if (!completed)
                await destination.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Asynchronously retrieves the standard error stream from a specified process.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation, containing the standard error stream.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    internal async Task<Stream> PipeStandardErrorAsync(CancellationToken cancellationToken)
    {
        Stream destination = new MemoryStream();
        bool completed = false;

        try
        {
            if (StartInfo.RedirectStandardError)
                if (StandardError != StreamReader.Null)
                    await StandardError.BaseStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

            destination.Position = 0;
            completed = true;
            return destination;
        }
        finally
        {
            // On the fault path only: on success the caller owns the returned stream.
            if (!completed)
                await destination.DisposeAsync().ConfigureAwait(false);
        }
    }
    #endregion

    #region Buffered truncation-aware capture

    /// <summary>
    ///     Asynchronously reads both redirected streams into strings, truncating each at its optional
    ///     per-stream byte cap (discarding the remainder beyond the limit).
    /// </summary>
    /// <remarks>
    ///     This is a distinct overload of the base buffered-capture method on <see cref="Process"/>;
    ///     the inherited method is NOT overridden. The cap parameter accepts these values:
    ///     <list type="bullet">
    ///         <item><c>null</c>: no cap is applied; the stream is read in full.</item>
    ///         <item>Negative value: no cap is applied; equivalent to <c>null</c>.</item>
    ///         <item><c>0</c>: a valid zero-byte cap producing empty text with the truncated flag set.</item>
    ///         <item>Positive value: the stream is read up to that many bytes, then truncated.</item>
    ///     </list>
    /// </remarks>
    /// <param name="cancellationToken">A cancellation token for the read operations.</param>
    /// <param name="maxStandardOutputBytes">Optional byte cap for standard output; value spellings are described above.</param>
    /// <param name="maxStandardErrorBytes">Optional byte cap for standard error; same semantics as <paramref name="maxStandardOutputBytes"/>.</param>
    /// <returns>
    ///     A tuple containing the captured standard output, standard error, and a flag indicating
    ///     whether either stream was truncated.
    /// </returns>
    internal async Task<(string StandardOutput, string StandardError, bool WasTruncated)> ReadAllTextAsync(
        CancellationToken cancellationToken,
        long? maxStandardOutputBytes = null,
        long? maxStandardErrorBytes = null)
    {
        Task<(string Text, bool Truncated)> standardOutputTask =
            ReadStreamCappedAsync(StandardOutput, StartInfo.RedirectStandardOutput, maxStandardOutputBytes, cancellationToken);
        Task<(string Text, bool Truncated)> standardErrorTask =
            ReadStreamCappedAsync(StandardError, StartInfo.RedirectStandardError, maxStandardErrorBytes, cancellationToken);

        await Task.WhenAll(standardOutputTask, standardErrorTask).ConfigureAwait(false);

        return (standardOutputTask.Result.Text, standardErrorTask.Result.Text,
            standardOutputTask.Result.Truncated || standardErrorTask.Result.Truncated);
    }

    /// <summary>
    ///     Reads a redirected stream into a string, copying at most <paramref name="maxBytes"/> bytes and
    ///     discarding any remainder (lossy truncation).
    /// </summary>
    /// <remarks>
    ///     <paramref name="maxBytes"/> accepts the same value spellings as
    ///     <c>ReadAllTextAsync</c> above. Multibyte sequences that straddle the cap boundary
    ///     are decoded incrementally so that split trailing bytes are held back and dropped
    ///     cleanly (no U+FFFD replacement characters).
    /// </remarks>
    private static async Task<(string Text, bool Truncated)> ReadStreamCappedAsync(
        StreamReader reader,
        bool redirected,
        long? maxBytes,
        CancellationToken cancellationToken)
    {
        if (!redirected || reader == StreamReader.Null)
            return (string.Empty, false);

        Encoding encoding = reader.CurrentEncoding;
        Stream stream = reader.BaseStream;

        // null or negative = no cap
        if (maxBytes is null or < 0)
        {
            using MemoryStream memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            return (encoding.GetString(memoryStream.ToArray()), false);
        }

        // 0 = valid zero-byte cap: empty text, truncated flag set
        if (maxBytes == 0)
        {
            // Drain any existing data so the pipe is fully consumed.
            byte[] drainBuffer = new byte[8192];
            while (await stream.ReadAsync(drainBuffer, cancellationToken).ConfigureAwait(false) > 0)
            {
                // Remainder is discarded.
            }
            return (string.Empty, true);
        }

        byte[] buffer = new byte[8192];
        long totalBytes = 0;
        bool truncated = false;

        using MemoryStream outputStream = new MemoryStream();

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            long room = maxBytes.Value - totalBytes;

            if (room <= 0)
            {
                truncated = true;
                break;
            }

            int bytesToCopy = (int)Math.Min(bytesRead, room);
            outputStream.Write(buffer, 0, bytesToCopy);
            totalBytes += bytesToCopy;

            if (bytesToCopy < bytesRead)
            {
                truncated = true;
                break;
            }
        }

        // Drain any remainder so the pipe is fully consumed even when truncated.
        while (truncated && (_ = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            // Remainder is discarded.
        }

        // Use incremental decoding so split trailing multibyte sequences are
        // held back and dropped cleanly instead of producing U+FFFD.
        Decoder decoder = encoding.GetDecoder();
        byte[] outputBytes = outputStream.ToArray();
        int charCount = decoder.GetCharCount(outputBytes, 0, outputBytes.Length);
        char[] chars = new char[charCount];
        decoder.GetChars(outputBytes, 0, outputBytes.Length, chars, 0);

        return (new string(chars), truncated);
    }

    #endregion

    internal void ForcefulExit()
    {
        if (HasExited)
            return;

        try
        {
            Kill(true);
        }
        catch (InvalidOperationException)
        {
            // Process exited between the HasExited check and Kill(true); nothing to do.
        }
        catch (Win32Exception)
        {
            // Process handle may be invalid or access denied; nothing more we can do.
        }
    }
    
    #region Cancellation Methods
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    internal async Task WaitForExitOrTimeoutAsync(
        ProcessExitConfiguration processExitConfiguration,
        CancellationToken cancellationToken = default)
    {
        // Disabled policy or a non-positive threshold: no timeout to enforce.
        if (!processExitConfiguration.TimeoutPolicy.Enabled
            || processExitConfiguration.TimeoutPolicy.TimeoutThreshold <= TimeSpan.Zero)
        {
            await WaitForExitOrCancellationAsync(processExitConfiguration,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        switch (processExitConfiguration.TimeoutPolicy.TimeoutExitBehaviour)
        {
            case ProcessExitBehaviour.WaitForExit:
            {
                await WaitForExitOrCancellationAsync(processExitConfiguration,
                    cancellationToken).ConfigureAwait(false);
                return;
            }
            case ProcessExitBehaviour.GracefulExit:
            default:
            {
                await WaitForExitOrGracefulTimeoutAsync(processExitConfiguration,
                    cancellationToken).ConfigureAwait(false);
                return;
            }
            case ProcessExitBehaviour.ForcefulExit:
            {
                await WaitForExitOrForcefulTimeoutAsync(processExitConfiguration,
                    cancellationToken).ConfigureAwait(false);
                return;
            }
        }
    }

    private async Task WaitForExitOrCancellationAsync(
        ProcessExitConfiguration processExitConfiguration,
        CancellationToken cancellationToken = default)
    {
        await WaitForExitCoreAsync(processExitConfiguration, cancellationToken, isGraceful: false).ConfigureAwait(false);
    }
    
    /// <summary>
    ///     Asynchronously waits for the process to exit or for the exit configuration's timeout policy
    ///     threshold to be exceeded, whichever is sooner.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the timeout threshold is less than 0.</exception>
    /// <exception cref="NotSupportedException">Thrown if run on a remote computer or device.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    internal async Task WaitForExitOrGracefulTimeoutAsync(
        ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken,
        bool fallbackToForceful = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(
            exitConfiguration.TimeoutPolicy.TimeoutThreshold, TimeSpan.Zero);

        // Disabled policy: no threshold to interrupt at, so wait for exit or cancellation.
        if (!exitConfiguration.TimeoutPolicy.Enabled)
        {
            await WaitForExitOrCancellationAsync(exitConfiguration, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await WaitForExitCoreAsync(exitConfiguration, cancellationToken, isGraceful: true, fallbackToForceful).ConfigureAwait(false);
    }

    /// <summary>
    ///     Waits for the process to exit without blocking on stream EOF.
    ///     <para>
    ///     The base-class <see cref="Process.WaitForExitAsync(CancellationToken)"/> always calls
    ///     <c>WaitUntilOutputEOF</c> after exit detection, which blocks indefinitely when
    ///     redirected stdout/stderr pipes are held open by grandchild or unrelated processes
    ///     (see dotnet/runtime#51277). This method avoids that by polling <see cref="Process.HasExited"/>
    ///     via <see cref="Task.Delay(int, CancellationToken)"/>, which never blocks on stream EOF.
    ///     </para>
    /// </summary>
    private async Task WaitForExitSafeAsync(CancellationToken cancellationToken)
    {
        const int pollIntervalMs = 200;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (HasExited)
                {
                    // The Exited event callback may not have run yet; publish ExitTime before
                    // returning so result readers never see default.
                    EnsureExitTime();
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                // Process was disposed externally; treat as exited.
                EnsureExitTime();
                return;
            }

            await Task.Delay(pollIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    ///     Marks <paramref name="task"/>'s eventual fault as observed without propagating it to
    ///     this call stack, so it can never surface later as an unobserved
    ///     <see cref="TaskScheduler.UnobservedTaskException"/>. Safe to call on tasks that have
    ///     not completed yet: the continuation observes the fault whenever it arrives.
    /// </summary>
    /// <param name="task">The task whose fault must be observed.</param>
    private static void ObserveFault(Task task)
    {
        _ = task.ContinueWith(
            static faulted => _ = faulted.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    /// <summary>
    ///     Unified critical section for wait+cancel flows. Owns the semaphore acquire/release pair.
    ///     <paramref name="isGraceful"/> selects between pure-cancellation and graceful-timeout behaviour.
    /// </summary>
    private async Task WaitForExitCoreAsync(
        ProcessExitConfiguration processExitConfiguration,
        CancellationToken cancellationToken,
        bool isGraceful,
        bool fallbackToForceful = true)
    {
        if (!await _cancellationSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            // Another cancellation is already in progress, wait for it to complete
            await WaitForExitSafeAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        // Captured before the wait so exception handling measures how far the
        // cancellation resolved from the expected exit time.
        DateTime expectedExitTime =
            CancellationHelper.CalculateExpectedExitTime(processExitConfiguration);

        try
        {
            if (isGraceful)
            {
                // Capture the cancellation task so its completion (and the resulting
                // _cancellationReason assignment) can be awaited explicitly below.
                Task<bool> cancelWithInterruptTask = CancelWithInterrupt(
                    processExitConfiguration.TimeoutPolicy.TimeoutThreshold,
                    processExitConfiguration, cancellationToken);

                Task firstExitWatch = WaitForExitSafeAsync(cancellationToken);
                await Task.WhenAny([firstExitWatch, cancelWithInterruptTask]).ConfigureAwait(false);

                Task interruptGraceDelay = Task.Delay(
                    TimeSpan.FromSeconds(
                        CalculatePostInterruptGracePeriodSeconds((int)processExitConfiguration.TimeoutPolicy.TimeoutThreshold.TotalSeconds)),
                    cancellationToken);
                Task secondExitWatch = WaitForExitSafeAsync(cancellationToken);
                await Task.WhenAny([interruptGraceDelay, secondExitWatch]).ConfigureAwait(false);

                // Task.WhenAny never surfaces candidate faults; observe them without propagating.
                // Policy-approved propagation is the interrupt task's job (below), so the default
                // CancellationThrowsException=false configuration still does not throw OCE.
                ObserveFault(firstExitWatch);
                ObserveFault(interruptGraceDelay);
                ObserveFault(secondExitWatch);

                // Await the interrupt/timeout resolution so _cancellationReason is persisted
                // before the caller reads Cancelled, and so a cancellation fault CancellationHelper
                // approved for propagation surfaces every time rather than depending on timing.
                // A completed task is always safe to await; skipping a still-running one is
                // reserved for the natural-exit race, where the wait succeeded unrelated to
                // cancellation and the late fault is only observed.
                if (cancelWithInterruptTask.IsCompleted
                    || !HasExited
                    || cancellationToken.IsCancellationRequested)
                {
                    await cancelWithInterruptTask.ConfigureAwait(false);
                }
                else
                {
                    ObserveFault(cancelWithInterruptTask);
                }

                if (!HasExited && fallbackToForceful)
                    ForcefulExit();
            }
            else
            {
                await WaitForExitSafeAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!isGraceful)
        {
            await CancelWithInterrupt(TimeSpan.Zero,
                processExitConfiguration, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (!isGraceful)
        {
            CancellationHelper.HandleCancellationExceptions(
                expectedExitTime,
                CancellationReason.RequestedCancellation, processExitConfiguration,
                exception);
        }
        finally
        {
            if (!HasExited && !isGraceful)
                ForcefulExit();

            // ForcefulExit (Kill) returns before the OS has reaped the child, so a
            // caller reading ExitCode immediately after this method returns could
            // otherwise observe "process must exit before requested information can
            // be determined". Poll for actual termination (bounded) before returning;
            // re-issue the forceful kill if the first attempt did not take.
            bool exited;
            try { exited = HasExited; }
            catch (ObjectDisposedException) { exited = true; }
            catch (InvalidOperationException) { exited = false; }

            if (!exited)
            {
                try { ForcefulExit(); }
                catch (InvalidOperationException) { /* already exited */ }
                catch (Win32Exception) { /* access denied / no such process */ }
                DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                while (true)
                {
                    try { exited = HasExited; }
                    catch (ObjectDisposedException) { exited = true; }
                    catch (InvalidOperationException) { exited = false; }
                    if (exited || DateTime.UtcNow >= deadline)
                        break;
                    try { await Task.Delay(25, CancellationToken.None).ConfigureAwait(false); }
                    catch { break; }
                }
            }

            // Belt-and-braces for returns that skipped the exit watches (e.g. a swallowed
            // exception): once the process is known exited, publish ExitTime.
            EnsureExitTime();

            _cancellationSemaphore.Release();
        }
    }
    
    
    /// <summary>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    internal async Task WaitForExitOrForcefulTimeoutAsync(
        ProcessExitConfiguration exitConfiguration,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(
            exitConfiguration.TimeoutPolicy.TimeoutThreshold, TimeSpan.Zero);

        DateTime expectedExitTime =
            DateTime.UtcNow.Add(exitConfiguration.TimeoutPolicy.TimeoutThreshold);

        CancellationTokenSource cts =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // A disabled timeout policy never arms the timer, so the wait below is bounded
            // only by the caller's token and the process's own exit, the same behaviour
            // ProcessTimeoutPolicy.None already exhibited for a zero threshold.
            if (exitConfiguration.TimeoutPolicy.Enabled
                && exitConfiguration.TimeoutPolicy.TimeoutThreshold > TimeSpan.Zero)
                cts.CancelAfter(exitConfiguration.TimeoutPolicy.TimeoutThreshold);

            CancellationToken actualCancellationToken = cts.Token;

            if (!await _cancellationSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            {
                // Another cancellation is already in progress, wait for it to complete
                await WaitForExitSafeAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            try
            {
                await WaitForExitSafeAsync(actualCancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                CancellationReason cancellationReason =
                    CancellationHelper.GetCancellationReason(expectedExitTime, cancellationToken);

                // Persist the reason BEFORE the propagation decision; see CancelWithInterrupt
                // for why the assignment must not sit after HandleCancellationExceptions.
                _cancellationReason = cancellationReason;

                CancellationHelper.HandleCancellationExceptions(expectedExitTime,
                    cancellationReason, exitConfiguration, exception);
            }
            finally
            {
                try
                {
                    ForcefulExit();
                }
                catch (Exception)
                {
                    // Best-effort kill; swallow any exception to avoid masking the original.
                }

                // The wait can end via the timeout/cancel exception path rather than a
                // confirmed exit; publish ExitTime once the kill has landed.
                EnsureExitTime();

                _cancellationSemaphore.Release();
            }
        }
        finally
        {
            // Disposed on every exit path, including a throw from the semaphore wait with an
            // already-cancelled token, so the linked token registration never leaks.
            cts.Dispose();
        }
    }
    
    /// <summary>
    ///     Sends an interrupt signal to the child process.
    ///     <remarks>Caller must already hold <c>_cancellationSemaphore</c>.</remarks>
    /// </summary>
    /// <exception cref="PlatformNotSupportedException"></exception>
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    private async Task<bool> CancelWithInterrupt(TimeSpan timeoutThreshold,
        ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken)
    {
        bool cancellationSuccess;

        // Captured before the wait so exception handling measures how far the
        // cancellation resolved from the expected exit time.
        DateTime expectedExitTime = DateTime.UtcNow.Add(timeoutThreshold);

        try
        {
            await Task.Delay(timeoutThreshold, cancellationToken).ConfigureAwait(false);

            if (HasExited)
                return true;

            // Reaching this point means the delay elapsed without the token being
            // cancelled, i.e. a graceful timeout. Persist the resolved reason before
            // sending the interrupt so Cancelled can be computed from it afterward.
            _cancellationReason = CancellationReason.Timeout;

            return await ProcessControlAdapter.SendInterruptSignalAsync(this,
                CancellationReason.Timeout, exitConfiguration, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Compute the cancellation reason at the catch point where the token is known-cancelled.
            CancellationReason cancellationReason =
                CancellationHelper.GetCancellationReason(expectedExitTime, cancellationToken);

            // Persist the reason BEFORE the propagation decision: on approved-throw
            // configurations HandleCancellationExceptions below throws, and an assignment
            // placed after it would be unreachable — leaving Cancelled false even though
            // the cancellation machinery performed the termination.
            _cancellationReason = cancellationReason;

            cancellationSuccess = await HandleCancellationMode(exitConfiguration, cancellationReason).ConfigureAwait(false);

            CancellationHelper.HandleCancellationExceptions(expectedExitTime,
                cancellationReason,
                exitConfiguration, exception);
        }
        finally
        {
            if (!HasExited)
                ForcefulExit();
        }

        return cancellationSuccess;
    }
    
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    private Task<bool> HandleCancellationMode(ProcessExitConfiguration exitConfiguration,
        CancellationReason cancellationReason)
    {
        switch (cancellationReason)
        {
            case CancellationReason.Timeout:
            {
                switch (exitConfiguration.TimeoutPolicy.TimeoutExitBehaviour)
                {
                    case ProcessExitBehaviour.ForcefulExit:
                    {
                        if (!HasExited)
                            ForcefulExit();

                        return Task.FromResult(true);
                    }
                    default:
                        return Task.FromResult(HasExited);
                }
            }
            case CancellationReason.RequestedCancellation or CancellationReason.NotKnown:
            default:
            {
                switch (exitConfiguration.RequestedCancellationExitBehaviour)
                {
                    case ProcessExitBehaviour.ForcefulExit:
                    {
                        if (!HasExited)
                            ForcefulExit();
                        
                        return Task.FromResult(true);
                    }
                    case ProcessExitBehaviour.GracefulExit:
                        return Task.FromResult(HasExited);
                    case ProcessExitBehaviour.WaitForExit:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            }
        }

        return Task.FromResult(false);
    }
    #endregion
}