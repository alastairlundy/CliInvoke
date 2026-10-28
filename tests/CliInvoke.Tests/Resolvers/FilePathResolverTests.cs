using System.Linq;
using System.Runtime.Versioning;
using CliInvoke.Core.Factories;
using CliInvoke.Factories;
using CliInvoke.Tests.Internal.Constants;

namespace CliInvoke.Tests.Resolvers;

public class FilePathResolverTests
{
    public static IExternalProcessFactory CreateExternalProcessFactory()
        => new ExternalProcessFactory();
    
    public static IFilePathResolver CreateFileResolver()
        => new FilePathResolver();

    [Test]
    public async Task Resolve_Dotnet_PathEnv_Executable()
    {
        string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        IFilePathResolver filePathResolver = CreateFileResolver();
        IExternalProcessFactory externalProcessFactory = CreateExternalProcessFactory();

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
                ProcessConfiguration configuration = new ProcessConfiguration("where", "dotnet.exe");

                IProcessInvoker processInvoker = new ProcessInvoker(externalProcessFactory);

                BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration,
                    cancellationToken: CancellationToken.None).ConfigureAwait(true);

                expected = new FileInfo(task.StandardOutput.Split(Environment.NewLine).First());
            }
        }
        else
        {
            ProcessConfiguration configuration = new ProcessConfiguration("which", "dotnet");

            IProcessInvoker processInvoker = new ProcessInvoker(externalProcessFactory);

            BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration,
                cancellationToken: CancellationToken.None).ConfigureAwait(true);

            string resolvedPath = task.StandardOutput.Trim().Split(Environment.NewLine).First();
            expected = new FileInfo(resolvedPath);
        }

        await Assert.That(actual.FullName).IsEqualTo(expected.FullName);
    }

    [Test]
    public async Task Resolve_CrossPlatform_PathEnv_Executable()
    {
        string executableName = ProcessTestHelper.GetTargetFilePath();

        // Deriving the unix expectation from the system's own `which` lookup keeps the
        // assertion portable: echo lives at /usr/bin/echo, /bin/echo, or another PATH
        // location depending on the OS and image, and the resolver must agree with the
        // shell rather than with a hardcoded path.
        IFilePathResolver filePathResolver = CreateFileResolver();

        FileInfo actual = filePathResolver.ResolveFilePath(executableName);

        if (OperatingSystem.IsWindows())
        {
            await Assert.That(actual.FullName).IsEqualTo(TargetFilePaths.CmdFilePath);
        }
        else
        {
            IExternalProcessFactory externalProcessFactory = CreateExternalProcessFactory();

            ProcessConfiguration configuration = new ProcessConfiguration("which", executableName);

            IProcessInvoker processInvoker = new ProcessInvoker(externalProcessFactory);

            BufferedProcessResult task = await processInvoker.ExecuteBufferedAsync(configuration,
                cancellationToken: CancellationToken.None).ConfigureAwait(true);

            string expectedPath = task.StandardOutput.Trim().Split(Environment.NewLine).First();

            await Assert.That(actual.FullName).IsEqualTo(expectedPath);
        }
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task Resolve_RelativeSubdirectory_Unix()
    {
        // Regression test: relative subdirectory resolution should work on Unix
        // by comparing f.Name against fileName using Ordinal comparison.
        IFilePathResolver filePathResolver = CreateFileResolver();

        // Create a temporary directory with a file in a subdirectory
        string tempDir = Path.Combine(Path.GetTempPath(), $"cliinvoke-resolver-test-{Guid.NewGuid():N}");
        string subDir = Path.Combine(tempDir, "subdir");
        string testFile = Path.Combine(subDir, "test.txt");

        try
        {
            Directory.CreateDirectory(subDir);
            await File.WriteAllTextAsync(testFile, "test content").ConfigureAwait(true);

            // Resolve using relative subdirectory path
            string relativePath = Path.Combine("subdir", "test.txt");
            FileInfo resolved = filePathResolver.ResolveFilePath(Path.Combine(tempDir, relativePath));

            await Assert.That(resolved.FullName).IsEqualTo(testFile);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
