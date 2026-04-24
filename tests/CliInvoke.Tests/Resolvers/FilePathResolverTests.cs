using System.Linq;

namespace CliInvoke.Tests.Resolvers;

public class FilePathResolverTests
{
    public static IFilePathResolver CreateFileResolver()
        => new FilePathResolver();

    [Test]
    public async Task Resolve_Dotnet_PathEnv_Executable()
    {
        string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        IFilePathResolver filePathResolver = CreateFileResolver();

        FileInfo actual = filePathResolver.ResolveFilePath(executable);

        FileInfo expected;

        if (OperatingSystem.IsWindows())
        {
            string? winExpected = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            if (winExpected is not null)
            {
                expected = new FileInfo(Path.Combine(winExpected, "dotnet.exe"));
            }
            else
            {
                using ProcessConfiguration configuration = ProcessConfigurationFactory.Create("where", "dotnet.exe");

                IProcessInvoker processInvoker = new ProcessInvoker(filePathResolver);

                BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration,
                    cancellationToken: CancellationToken.None);

                expected = new FileInfo(task.StandardOutput.Split(Environment.NewLine).First());
            }
        }
        else
        {
            using ProcessConfiguration configuration = ProcessConfigurationFactory.Create("which", "dotnet");

            IProcessInvoker processInvoker = new ProcessInvoker(filePathResolver);

            BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration,
                cancellationToken: CancellationToken.None);

            string resolvedPath = task.StandardOutput.Trim().Split(Environment.NewLine).First();
            expected = new FileInfo(resolvedPath);
        }

        await Assert.That(actual.FullName).IsEqualTo(expected.FullName);
    }

    [Test]
    public async Task Resolve_CrossPlatform_PathEnv_Executable()
    {
        FileInfo expected = new FileInfo(ProcessTestHelper.GetTargetFilePath());

        IFilePathResolver filePathResolver = CreateFileResolver();

        FileInfo actual = filePathResolver.ResolveFilePath(expected.Name);

        await Assert.That(actual.FullName).IsEqualTo(expected.FullName);
    }
}