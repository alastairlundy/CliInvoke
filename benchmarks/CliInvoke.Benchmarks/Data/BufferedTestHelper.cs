namespace CliInvoke.Benchmarking.Data;

public class BufferedTestHelper
{
    public BufferedTestHelper()
    {
        TargetFilePath = GetMockDataSimExePath();
    }

    private string GetMockDataSimExePath()
    {
        string mockDataToolExe = OperatingSystem.IsWindows() ? "CliInvokeBenchMockData.exe" : "CliInvokeBenchMockData";

        try
        {
            return new FilePathResolver().ResolveFilePath(mockDataToolExe);
        }
        catch (Exception)
        {
            // Fallback to searching upwards for the project structure
            DirectoryInfo? currentDir = new DirectoryInfo(AppContext.BaseDirectory);

            while (currentDir != null)
            {
                // Check if the current directory contains the tool project directly or inside a 'benchmarks' folder
                string[] potentialProjectPaths =
                {
                    Path.Combine(currentDir.FullName, "CliInvoke.Benchmarking.MockDataSimulationTool"),
                    Path.Combine(currentDir.FullName, "benchmarks", "CliInvoke.Benchmarking.MockDataSimulationTool")
                };

                foreach (string projectPath in potentialProjectPaths)
                {
                    if (Directory.Exists(projectPath))
                    {
                        string binPath = Path.Combine(projectPath, "bin");
                        if (Directory.Exists(binPath))
                        {
                            FileInfo[] files = new DirectoryInfo(binPath).GetFiles(mockDataToolExe, SearchOption.AllDirectories);
                            if (files.Length > 0)
                            {
                                // Prioritize Release over Debug, and newer .NET versions
                                return files
                                    .OrderByDescending(f => f.FullName.Contains("Release", StringComparison.OrdinalIgnoreCase))
                                    .ThenByDescending(f => f.FullName.Contains("net10.0", StringComparison.OrdinalIgnoreCase))
                                    .ThenByDescending(f => f.FullName.Contains("net9.0", StringComparison.OrdinalIgnoreCase))
                                    .First().FullName;
                            }
                        }
                    }
                }

                // Check if the executable is in the current directory itself (for published benchmarks)
                string localExe = Path.Combine(currentDir.FullName, mockDataToolExe);
                if (File.Exists(localExe))
                {
                    return Path.GetFullPath(localExe);
                }

                currentDir = currentDir.Parent;
            }
        }

        throw new ArgumentException($"Could not find {mockDataToolExe} executable in PATH or project structure.");
    }

    public string TargetFilePath
    {
        get;
        private set => field = value;
    }

    public string Arguments => "gen-fake-text";
}
