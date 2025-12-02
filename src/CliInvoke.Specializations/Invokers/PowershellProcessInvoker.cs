using System;
using System.Threading;
using System.Threading.Tasks;

using CliInvoke.Core;
using CliInvoke.Core.Extensibility;
using CliInvoke.Core.Extensibility.Factories;
using CliInvoke.Specializations.Configurations;
using CliInvoke.Specializations.Internal.Localizations;

namespace CliInvoke.Specializations;

public class PowershellProcessInvoker : RunnerProcessInvokerBase
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="processInvoker"></param>
    /// <param name="runnerProcessFactory"></param>
    /// <param name="filePathResolver"></param>
    /// <param name="windowCreation"></param>
    /// <param name="redirectOutputs"></param>
    public PowershellProcessInvoker(IProcessInvoker processInvoker,
        IRunnerProcessFactory runnerProcessFactory, IFilePathResolver filePathResolver,
        bool windowCreation = true, bool redirectOutputs = true)
        : base(processInvoker, runnerProcessFactory, new PowershellProcessConfiguration(
            filePathResolver, "", false, redirectOutputs, redirectOutputs,
            windowCreation: windowCreation))
    {
        
    }

    public override Task<ProcessResult> ExecuteAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null, bool disposeOfConfig = true,
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException();
        
        return base.ExecuteAsync(processConfiguration, processExitConfiguration, disposeOfConfig, cancellationToken);
    }

    public override Task<BufferedProcessResult> ExecuteBufferedAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null, bool disposeOfConfig = true,
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException(Resources.Exceptions_Powershell_OnlySupportedOnDesktop);
        
        return base.ExecuteBufferedAsync(processConfiguration, processExitConfiguration, disposeOfConfig, cancellationToken);
    }

    public override Task<PipedProcessResult> ExecutePipedAsync(ProcessConfiguration processConfiguration,
        ProcessExitConfiguration? processExitConfiguration = null, bool disposeOfConfig = true,
        CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
            OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException(Resources.Exceptions_Powershell_OnlySupportedOnDesktop);

        return base.ExecutePipedAsync(processConfiguration, processExitConfiguration, disposeOfConfig, cancellationToken);
    }
}