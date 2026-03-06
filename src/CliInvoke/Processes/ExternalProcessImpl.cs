namespace CliInvoke.Processes;

internal partial class ExternalProcessImpl
{
    internal int ProcessId { get; private set; }
    
    internal string TargetFilePath { get; set; }
    
    internal DateTime StartTime { get; private set; }
    internal DateTime ExitTime { get; private set; }

    internal event EventHandler? Started;
    internal event EventHandler? Exited;

    ProcessConfiguration Configuration { get; }

    internal ExternalProcessImpl(ProcessConfiguration configuration)
    {
        Configuration = configuration;

        TargetFilePath = configuration.TargetFilePath;
        
        Started += OnStarted;
        Exited += OnExited;
    }

    private void OnExited(object? sender, EventArgs e)
    {
        ExitTime = DateTime.Now;
    }

    private void OnStarted(object? sender, EventArgs e)
    {
        StartTime = DateTime.Now;
        SetResourcePolicy(Configuration.ResourcePolicy);
    }

    internal void Exit(uint exitCode)
    {
        if (OperatingSystem.IsWindows())
        {
            ExitOnWindows(exitCode);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() ||
                 OperatingSystem.IsMacCatalyst() || OperatingSystem.IsFreeBSD())
        {
            
        }
    }

    internal void SetResourcePolicy(ProcessResourcePolicy resourcePolicy)
    {
        
    }
    
    internal Task WaitForExitOrTimeoutAsync(CancellationToken cancellationToken)
    {
        
    }

    internal Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        
    }
}