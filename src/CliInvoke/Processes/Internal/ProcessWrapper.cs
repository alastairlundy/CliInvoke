/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;

using CliInvoke.Processes.Internal.Cancellation;
using CliInvoke.Processes.Internal.ControlAdapters;

namespace CliInvoke.Processes.Internal;

/// <summary>
///
/// </summary>
internal class ProcessWrapper : Process
{
    /// <summary>
    /// Computes how long, in seconds, the graceful cancellation path should wait after the
    /// interrupt signal has been sent before giving up and falling back to
    /// (optionally) a forceful exit.
    /// </summary>
    /// <param name="timeoutSeconds">The user-supplied timeout threshold, in whole seconds.</param>
    /// <returns>
    /// <c>min(10 + floor(timeoutSeconds * 0.05), 20)</c> — i.e. a fixed 10s base plus 5% of the
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
    internal readonly SemaphoreSlim _cancellationSemaphore = new(1, 1);

    internal ProcessWrapper(ProcessConfiguration configuration,
        ProcessResourcePolicy? resourcePolicy)
    {
        ProcessControlAdapter = ProcessControlAdapterFactory.Create();
        ResourcePolicy = resourcePolicy ?? ProcessResourcePolicy.Default;
        ProcessControlAdapter.ApplyConfiguration(this, configuration);
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

    internal new string ProcessName { get; private set; }

    
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

            // TODO: Replace with ProcessStartInfo.StartSuspended + SafeProcessHandle.Resume()
            // on Windows and macOS when .NET 11 is added as a target framework.
            try
            {
                SuspendProcess();
            }
            catch
            {
                // Process exited before we could suspend it, or suspend failed.
                return;
            }

            try
            {
#pragma warning disable CA1416
                ProcessControlAdapter.SetResourcePolicy(this, ResourcePolicy);
#pragma warning restore CA1416
            }
            finally
            {
                // Always resume the process — even if SetResourcePolicy throws —
                // to prevent leaving it permanently suspended.
                try
                {
                    ResumeProcess();
                }
                catch
                {
                    // The process may have already exited during SetResourcePolicy.
                    // Swallow the exception — the process is gone, nothing to resume.
                }
            }
        }
    }

    private void OnExited(object? sender, EventArgs e)
    {
        ExitTime = base.ExitTime;
    }

    internal event EventHandler Started;
    
    public new bool Start()
    {
        try
        {
            HasStarted = base.Start();
        }
        catch(Win32Exception exception)
        {
            HasStarted = false;

            throw new UnauthorizedAccessException($"The current user does not have permission to execute the file '{StartInfo.FileName}'.", exception);
        }

        if (!HasStarted)
        {
            throw new InvalidOperationException($"Process with Target File Name of '{StartInfo.FileName}' could not be started.");
        }

        if (!HasStarted) return HasStarted;

        // Cache StandardOutput/StandardError StreamReaders while the process is still
        // guaranteed alive. These properties internally call EnsureState, which in
        // .NET 10 throws InvalidOperationException("process has exited") on
        // fast-exiting processes (e.g. `which dotnet`) that exit before the next
        // line of code runs. Touching them here means downstream code that reads
        // from the cached readers works even if the process has already exited.
        try
        {
            _ = base.StandardOutput;
            _ = base.StandardError;
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
        Started.Invoke(this, EventArgs.Empty);

        return HasStarted;
    }

    /// <summary>
    /// Suspends the current process. Routes to the platform-specific implementation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an attempt is made to suspend a process that has already exited.
    /// </exception>
    /// <remarks>
    /// This method leverages platform-specific mechanisms to suspend a process and is supported
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
    /// This method utilises platform-specific mechanisms to resume a suspended process
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

        ProcessControlAdapter.ResumeProcess(this);
    }

    #region Piping Standard Inputs and Outputs
    /// <summary>
    ///     Asynchronously pipes the standard input from a source stream to a specified process.
    /// </summary>
    /// <param name="source">The stream from which to read the standard input data.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation containing the destination process.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    internal async Task<bool> PipeStandardInputAsync(Stream source,
        CancellationToken cancellationToken)
    {
        if (StartInfo.RedirectStandardInput)
        {
            await StandardInput.FlushAsync(cancellationToken);
            StandardInput.BaseStream.Position = 0;
            await source.CopyToAsync(StandardInput.BaseStream, cancellationToken);

            return source.Equals(StandardInput.BaseStream);
        }

        return false;
    }
    
    /// <summary>
    ///     Asynchronously retrieves the standard output stream from a specified process.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation, containing the standard output stream.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    internal async Task<Stream> PipeStandardOutputAsync(CancellationToken cancellationToken)
    {
        Stream destination = new MemoryStream();

        if (StartInfo.RedirectStandardOutput)
            if (StandardOutput != StreamReader.Null)
                await StandardOutput.BaseStream.CopyToAsync(destination, cancellationToken);

        return destination;
    }

    /// <summary>
    ///     Asynchronously retrieves the standard error stream from a specified process.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation, containing the standard error stream.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    internal async Task<Stream> PipeStandardErrorAsync(CancellationToken cancellationToken)
    {
        Stream destination = new MemoryStream();

        if (StartInfo.RedirectStandardError)
            if (StandardError != StreamReader.Null)
                await StandardError.BaseStream.CopyToAsync(destination, cancellationToken);

        return destination;
    }
    #endregion

    internal void ForcefulExit()
    {
        try
        {
            Kill(true);
        }
        catch
        {
            Kill();
        }
    }
    
    #region Cancellation Methods
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    internal async Task WaitForExitOrTimeoutAsync(
        ProcessExitConfiguration processExitConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (processExitConfiguration.TimeoutPolicy.TimeoutThreshold <= TimeSpan.Zero)
        {
            await WaitForExitOrCancellationAsync(processExitConfiguration,
                cancellationToken);
            return;
        }

        switch (processExitConfiguration.TimeoutPolicy.TimeoutExitBehaviour)
        {
            case ProcessExitBehaviour.WaitForExit:
            {
                await WaitForExitOrCancellationAsync(processExitConfiguration,
                    cancellationToken);
                return;
            }
            case ProcessExitBehaviour.GracefulExit:
            default:
            {
                await WaitForExitOrGracefulTimeoutAsync(processExitConfiguration,
                    cancellationToken);
                return;
            }
            case ProcessExitBehaviour.ForcefulExit:
            {
                await WaitForExitOrForcefulTimeoutAsync(processExitConfiguration,
                    cancellationToken);
                return;
            }
        }
    }

    private async Task WaitForExitOrCancellationAsync(
        ProcessExitConfiguration processExitConfiguration,
        CancellationToken cancellationToken = default)
    {
        // Use semaphore to prevent simultaneous cancellation attempts
        if (!await _cancellationSemaphore.WaitAsync(0, cancellationToken))
        {
            // Another cancellation is already in progress, wait for it to complete
            await WaitForExitAsync(cancellationToken);
            return;
        }

        try
        {
            await WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await CancelWithInterrupt(TimeSpan.Zero,
                processExitConfiguration, cancellationToken);
        }
        catch (Exception exception)
        {
            // Recalculate values in exception handler to avoid using stale values
            DateTime currentExpectedExitTime =
                CancellationHelper.CalculateExpectedExitTime(processExitConfiguration);
                
            CancellationHelper.HandleCancellationExceptions(
                currentExpectedExitTime
                , CancellationReason.RequestedCancellation, processExitConfiguration,
                exception);
        }
        finally
        {
            if (!HasExited) 
                ForcefulExit();
                
            _cancellationSemaphore.Release();
        }
    }
    
    /// <summary>
    ///     Asynchronously waits for the process to exit or for the exit configuration's timeout policy
    ///     threshold to be exceeded, whichever is sooner.
    /// </summary>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="fallbackToForceful"></param>
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

        // Use semaphore to prevent simultaneous cancellation attempts
        if (!await _cancellationSemaphore.WaitAsync(0, cancellationToken))
        {
            // Another cancellation is already in progress, wait for it to complete
            await WaitForExitAsync(cancellationToken);
            return;
        }

        try
        {
            await Task.WhenAny([
                WaitForExitAsync(cancellationToken),
                CancelWithInterrupt(exitConfiguration.TimeoutPolicy.TimeoutThreshold,
                    exitConfiguration, cancellationToken)
            ]);

            await Task.WhenAny([
                Task.Delay(
                    TimeSpan.FromSeconds(
                        CalculatePostInterruptGracePeriodSeconds((int)exitConfiguration.TimeoutPolicy.TimeoutThreshold.TotalSeconds)),
                    cancellationToken),
                WaitForExitAsync(cancellationToken)
            ]);

            if (!HasExited && fallbackToForceful) 
                ForcefulExit();
        }
        finally
        {
            _cancellationSemaphore.Release();
        }
    }
    
    
    /// <summary>
    /// </summary>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken"></param>
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

        if (exitConfiguration.TimeoutPolicy.TimeoutThreshold > TimeSpan.Zero)
            cts.CancelAfter(exitConfiguration.TimeoutPolicy.TimeoutThreshold);

        CancellationToken actualCancellationToken = cts.Token;

        // Use a local variable to store the cancellation reason to avoid race conditions
        CancellationReason cancellationReason = CancellationReason.NotKnown;

        actualCancellationToken.Register(() =>
        {
            cancellationReason =
                CancellationHelper.GetCancellationReason(expectedExitTime,
                    cancellationToken);
        });

        // Use semaphore to prevent simultaneous cancellation attempts
        if (!await _cancellationSemaphore.WaitAsync(0, cancellationToken))
        {
            // Another cancellation is already in progress, wait for it to complete
            await WaitForExitAsync(cancellationToken);
            // Dispose of the linked CTS to prevent resource leaks
            cts.Dispose();
            return;
        }

        try
        {
            await WaitForExitAsync(actualCancellationToken);
        }
        catch (Exception exception)
        {
            // Recalculate expected exit time in exception handler to avoid using stale values
            DateTime currentExpectedExitTime =
                CancellationHelper.CalculateExpectedExitTime(exitConfiguration);
            CancellationHelper.HandleCancellationExceptions(currentExpectedExitTime,
                cancellationReason, exitConfiguration, exception);
        }
        finally
        {
            ForcefulExit();
            // Dispose of the linked CTS to prevent resource leaks

            cts.Dispose();
            _cancellationSemaphore.Release();
        }
    }
    
    /// <summary>
    /// </summary>
    /// <param name="timeoutThreshold"></param>
    /// <param name="exitConfiguration"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="PlatformNotSupportedException"></exception>
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    private async Task<bool> CancelWithInterrupt(TimeSpan timeoutThreshold,
        ProcessExitConfiguration exitConfiguration, CancellationToken cancellationToken)
    {
        // Use semaphore to prevent simultaneous cancellation attempts
        if (!await _cancellationSemaphore.WaitAsync(0, cancellationToken))
        {
            // Another cancellation is already in progress, wait for it to complete
            await WaitForExitAsync(cancellationToken);
            return HasExited;
        }

        try
        {
            DateTime expectedExitTime =
                CancellationHelper.CalculateExpectedExitTime(exitConfiguration);

            // Use a local variable to store the cancellation reason to avoid race conditions
            CancellationReason cancellationReason = CancellationReason.NotKnown;

            // Register the callback to update the cancellation reason
            cancellationToken.Register(() =>
            {
                cancellationReason =
                    CancellationHelper.GetCancellationReason(expectedExitTime,
                        cancellationToken);
            });

            bool cancellationSuccess;

            try
            {
                await Task.Delay(timeoutThreshold, cancellationToken);

                if (HasExited)
                    return true;

                return  await ProcessControlAdapter.SendInterruptSignalAsync(this,
                    cancellationReason, exitConfiguration, cancellationToken);
            }
            catch (Exception exception)
            {
                // Recalculate expected exit time in exception handler to avoid using stale values
                DateTime currentExpectedExitTime =
                    CancellationHelper.CalculateExpectedExitTime(exitConfiguration);
                
                cancellationSuccess = await HandleCancellationMode(exitConfiguration, cancellationReason);
                
                CancellationHelper.HandleCancellationExceptions(currentExpectedExitTime,
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
        finally
        {
            _cancellationSemaphore.Release();
        }
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