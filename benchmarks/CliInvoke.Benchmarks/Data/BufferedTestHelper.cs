using DotExtensions.IO.Directories;
using DotPrimitives.IO.Drives;

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
        string mockDataToolExe = OperatingSystem.IsWindows() ? "CliInvokeBenchMockData.exe" : "CliInvokeBenchMockData";
        
        FileInfo? executable = StorageDrives.Shared.EnumerateLogicalDrives()
            .Where(d => d.IsReady)
            .Select(d => d.RootDirectory)
            .SelectMany(d =>
                d.SafelyEnumerateFiles("*", SearchOption.AllDirectories))
            .FirstOrDefault(f => f.Name.Equals(mockDataToolExe, StringComparison.InvariantCultureIgnoreCase));

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
