using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliInvoke.Core.Factories;
using CliInvoke.Factories;
using CliInvoke.Piping;
using WhatExec.Lib;
using WhatExec.Lib.Abstractions;
using WhatExec.Lib.Abstractions.Detectors;
using WhatExec.Lib.Detectors;

namespace CliInvoke.Tests.Resolvers;

public class FilePathResolverTests
{

    public static IExecutableFileResolver CreateFileResolver()
    {
        IExecutableFileDetector fileDetector = new ExecutableFileDetector();
        
       return new ExecutableFileResolver(fileDetector, new PathEnvironmentVariableResolver(new PathEnvironmentVariableDetector(), fileDetector));
    }
    
    [Fact]
    public async Task Resolve_Dotnet_PathEnv_Executable()
    {
        string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
        
        IExecutableFileResolver filePathResolver = CreateFileResolver();

        FileInfo actual = await filePathResolver.LocateExecutableAsync(executable, SearchOption.AllDirectories, CancellationToken.None);

        FileInfo expected;

        if (OperatingSystem.IsWindows())
        {
            string? winExpected = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            
            if(winExpected is not null)
                expected = new FileInfo(winExpected);
            else
            {
                IProcessConfigurationFactory processConfigurationFactory = new ProcessConfigurationFactory();
                using ProcessConfiguration configuration = processConfigurationFactory.Create("where", "dotnet.exe");

                IProcessInvoker processInvoker = new ProcessInvoker(filePathResolver, new ProcessPipeHandler());

                BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration, cancellationToken: TestContext.Current.CancellationToken);

                expected = new FileInfo(task.StandardOutput.Split(Environment.NewLine).First());
            }
        }
        else
        {
            expected = new FileInfo("/usr/bin/dotnet");
        }
       
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Resolve_CrossPlatform_PathEnv_Executable()
    {
        FileInfo expected = new FileInfo(ProcessTestHelper.GetTargetFilePath());

        IExecutableFileResolver filePathResolver = CreateFileResolver();

        FileInfo actual = await filePathResolver.LocateExecutableAsync(expected.Name, SearchOption.AllDirectories, CancellationToken.None);
        
        Assert.Equal(expected, actual);
    }
}