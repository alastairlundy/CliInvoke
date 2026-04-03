using System.Linq;
using CliInvoke.Core.Factories;
using CliInvoke.Factories;
using WhatExec.Lib.Abstractions.Detectors;
using WhatExec.Lib.Abstractions.Resolvers;
using WhatExec.Lib.Detectors;
using WhatExec.Lib.Resolvers;

namespace CliInvoke.Tests.Resolvers;

public class FilePathResolverTests
{
    public static IExecutableFileResolver CreateFileResolver()
    {
        IExecutableFileDetector fileDetector = new ExecutableFileDetector();

        return new ExecutableFileResolver(fileDetector,
            new PathEnvironmentVariableResolver(new PathEnvironmentVariableDetector(), fileDetector));
    }

    [Test]
    public async Task Resolve_Dotnet_PathEnv_Executable()
    {
        string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        IExecutableFileResolver filePathResolver = CreateFileResolver();

        FileInfo actual =
            await filePathResolver.LocateExecutableAsync(executable, SearchOption.AllDirectories,
                CancellationToken.None);

        FileInfo expected;

        if (OperatingSystem.IsWindows())
        {
            string? winExpected = Environment.GetEnvironmentVariable("DOTNET_ROOT") + $"{Path.DirectorySeparatorChar}dotnet.exe";

            if (winExpected is not null)
            {
                expected = new FileInfo(winExpected);
            }
            else
            {
                IProcessConfigurationFactory processConfigurationFactory = new ProcessConfigurationFactory();
                using ProcessConfiguration configuration = processConfigurationFactory.Create("where", "dotnet.exe");

                IProcessInvoker processInvoker = new ProcessInvoker(filePathResolver);

                BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration,
                    cancellationToken: CancellationToken.None);

                expected = new FileInfo(task.StandardOutput.Split(Environment.NewLine).First());
            }
        }
        else
        {
            expected = new FileInfo("/usr/bin/dotnet");
        }

        await Assert.That(actual.FullName).IsEqualTo(expected.FullName);
    }

    [Test]
    public async Task Resolve_CrossPlatform_PathEnv_Executable()
    {
        FileInfo expected = new FileInfo(ProcessTestHelper.GetTargetFilePath());

        IExecutableFileResolver filePathResolver = CreateFileResolver();

        FileInfo actual = await filePathResolver.LocateExecutableAsync(expected.Name, SearchOption.AllDirectories,
            CancellationToken.None);

        await Assert.That(actual.FullName).IsEqualTo(expected.FullName);
    }
}