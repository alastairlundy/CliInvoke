using System;
using System.Linq;
using System.Threading.Tasks;
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using CliInvoke.Factories;
using CliInvoke.Piping;
using CliInvoke.Tests.Internal.Constants;
using CliInvoke.Tests.Internal.Helpers;
using Xunit;

namespace CliInvoke.Tests.Resolvers;

public class FilePathResolverTests
{
    [Fact]
    public async Task Resolve_Dotnet_PathEnv_Executable()
    {
        string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        IFilePathResolver filePathResolver = new FilePathResolver();

        string actual = filePathResolver.ResolveFilePath(executable);

        string expected;

        if (OperatingSystem.IsWindows())
        {
            string? winExpected = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            
            if(winExpected is not null)
                expected = winExpected;
            else
            {
                IProcessConfigurationFactory processConfigurationFactory = new ProcessConfigurationFactory();
                using ProcessConfiguration configuration = processConfigurationFactory.Create("where.exe", "dotnet.exe");

                IProcessInvoker processInvoker = new ProcessInvoker(new FilePathResolver(), new ProcessPipeHandler());

                BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration, cancellationToken: TestContext.Current.CancellationToken);

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
        string expected = OperatingSystem.IsWindows() ? TargetFilePaths.CmdFilePath : TargetFilePaths.LinuxEchoFilePath;
        
        IFilePathResolver filePathResolver = new FilePathResolver();

        string actual = filePathResolver.ResolveFilePath(ProcessTestHelper.GetTargetFilePath());
        
        Assert.Equal(expected, actual);
    }
}