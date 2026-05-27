/*
    CliInvoke
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

using System.ComponentModel;

using CliInvoke.Helpers;
using CliInvoke.Helpers.Processes.Cancellation;
using CliInvoke.Processes.Internal.ControlAdapters;

namespace CliInvoke.Processes.Internal;

/// <summary>
/// 
/// </summary>
internal class ProcessWrapper : Process
{
    internal const int GracefulTimeoutWaitSeconds = 5;
    
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

    internal ProcessResourcePolicy ResourcePolicy { get; set; }

    internal bool HasStarted { get; private set; }

    internal new DateTime StartTime { get; private set; }

    internal new DateTime ExitTime { get; private set; }

    internal new int Id { get; private set; }

    internal new string ProcessName { get; private set; }

    
    private void OnStarted(object? sender, EventArgs e)
    {
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
            || OperatingSystem.IsFreeBSD())
        {
            // TODO: Replace with ProcessHandle CreateSuspended as part of .NET 11 support.
            try
            {
                SuspendProcess();

#pragma warning disable CA1416
                ProcessControlAdapter.SetResourcePolicy(this, ResourcePolicy);
#pragma warning restore CA1416

                ResumeProcess();
            }
            catch
            {
                // Ignored
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
        
        StartTime = DateTime.UtcNow;
        Started.Invoke(this, EventArgs.Empty);
        Id = base.Id;
        ProcessName = base.ProcessName;

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
                Task.Delay(500, cancellationToken), WaitForExitAsync(cancellationToken)
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
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

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

            bool cancellationSuccess = false;

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
                
                if(!OperatingSystem.IsWindows())
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