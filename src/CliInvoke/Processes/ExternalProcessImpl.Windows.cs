using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace CliInvoke.Processes;

internal partial class ExternalProcessImpl
{
    Handle ProcessHandle;
    
    private bool StartOnWindows(ProcessConfiguration processConfiguration)
    {

        bool success = CreateProcessW(, false, );

        if (success)
        {
            Started?.Invoke(this, EventArgs.Empty);
        }
        
        return success;
    }

    [DllImport("Kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true)]
    private static extern bool CreateProcessW(string lpApplicationName,
        [MarshalAs(UnmanagedType.Bool)]
        bool bInheritHandles
        );

    
    private void ExitOnWindows(uint exitCode)
    {
        bool success = TerminateProcess(ProcessHandle, exitCode);

        if (!success)
            KillProcess(exitCode);
        
        Exited?.Invoke(this, EventArgs.Empty);
    }

    [DllImport("Kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true)]
    private static extern bool TerminateProcess(Handle hProcess,uint uExitCode);

    [DllImport("Kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true)]
    private static extern void KillProcess(uint exitCode);
}