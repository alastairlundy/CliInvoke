using CliInvoke.Core;
using CliInvoke.Piping;
using DotExtensions.IO.Directories;
using DotPrimitives.IO.Drives;

#nullable enable

namespace CliInvoke.Benchmarking.Data;

public class BufferedTestHelper
{
    public BufferedTestHelper()
    {
        TargetFilePath = ""; 
        
        TargetFilePath = GetMockDataSimExePath();
    }

    private string GetMockDataSimExePath()
    {
        string mockDataToolExe = OperatingSystem.IsWindows() ? "CliInvokeMockDataSimTool.exe" : "CliInvokeMockDataSimTool";
        
        FileInfo? executable = StorageDrives.Shared.EnumeratePhysicalDrives()
            .Where(d => d.IsReady)
            .Select(d => d.RootDirectory)
            .SelectMany(d =>
                d.SafelyEnumerateFiles(mockDataToolExe, SearchOption.AllDirectories))
            .FirstOrDefault();

        if (executable is not null)
            return executable.FullName;

        throw new ArgumentException("Could not find CliInvoke Mock Data Sim Tool executable");
    }

    public string TargetFilePath
    {
        get;
        private set => field = value;
    }

    public string Arguments => "gen-fake-text";
}
