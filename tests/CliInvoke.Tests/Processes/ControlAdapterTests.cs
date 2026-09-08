using System.Runtime.Versioning;

using CliInvoke.Core;
using CliInvoke.Processes.Internal.ControlAdapters;

namespace CliInvoke.Tests.Processes;

public class ControlAdapterTests
{
    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_RequireRunningAsAdmin_ThrowsPlatformNotSupportedException()
    {
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        using Process dummyProcess = new Process();
        dummyProcess.StartInfo = new ProcessStartInfo("echo");

        await Assert.That(() => adapter.RequireRunningAsAdmin(dummyProcess))
            .Throws<PlatformNotSupportedException>();
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_SetUserCredential_NonNull_ThrowsPlatformNotSupportedException()
    {
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        using Process dummyProcess = new Process();
        dummyProcess.StartInfo = new ProcessStartInfo("echo");

        UserCredential credential = new UserCredential(null, "testuser", null, null);

        await Assert.That(() => adapter.SetUserCredential(dummyProcess, credential))
            .Throws<PlatformNotSupportedException>();
    }

    [Test]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    public async Task UnixAdapter_SetUserCredential_Null_DoesNotThrow()
    {
        UnixProcessControlAdapter adapter = new UnixProcessControlAdapter();

        using Process dummyProcess = new Process();
        dummyProcess.StartInfo = new ProcessStartInfo("echo");

        await Assert.That(() => adapter.SetUserCredential(dummyProcess, null!))
            .ThrowsNothing();
    }
}
