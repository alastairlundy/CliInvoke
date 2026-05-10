using System.Linq;
using CliInvoke.Piping;
using Assert = Xunit.Assert;
using TestContext = Xunit.TestContext;

namespace CliInvoke.Tests.Resolvers;

public class FilePathResolverTests
{
    [Fact]
    public async Task Resolve_Dotnet_PathEnv_Executable()
    {
        string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        
        string actual = FilePathResolver.Shared.ResolveFilePath(executable);

        string expected;

        if (OperatingSystem.IsWindows())
        {
            string? winExpected = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            if (winExpected is not null)
            {
                expected = winExpected;
                
                if (OperatingSystem.IsWindows())
                {
                    expected = $"{expected}{Path.DirectorySeparatorChar}dotnet.exe";
                }
            }
            else
            {
                using ProcessConfiguration configuration = new ("where", "dotnet.exe");

                IProcessInvoker processInvoker = new ProcessInvoker(FilePathResolver.Shared, ProcessPipeHandler.Shared);

                BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration,
                    cancellationToken: TestContext.Current.CancellationToken);

                expected = task.StandardOutput.Split(Environment.NewLine).First();
            }
        }
        else
        {
            expected = "/usr/bin/dotnet";
        }
       
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Resolve_CrossPlatform_PathEnv_Executable()
    {
        string expected = ProcessTestHelper.GetTargetFilePath();

        string actual = FilePathResolver.Shared.ResolveFilePath(expected);
        
        Assert.Equal(expected, actual);
    }
}