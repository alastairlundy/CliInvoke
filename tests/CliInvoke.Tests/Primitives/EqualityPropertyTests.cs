using CliInvoke.Core;
using FsCheck;

namespace CliInvoke.Tests.Primitives;

/// <summary>
///     Property-based tests verifying the equality contracts (reflexivity, symmetry,
///     transitivity, and hash consistency) for the library's core primitives via FsCheck.
/// </summary>
public class EqualityPropertyTests
{
    private static ProcessResult ArbitraryProcessResult()
        => new("foo.exe", 0, 1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2),
            canceled: false, signal: null);

    private static BufferedProcessResult ArbitraryBufferedProcessResult()
        => new("foo.exe", 0, 1, "out", "err", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2),
            canceled: false, signal: null);

    [Test]
    public async Task ProcessResult_Equality_IsReflexive()
    {
        ProcessResult a = ArbitraryProcessResult();

        await Assert.That(a.Equals(a)).IsTrue();
    }

    [Test]
    public async Task ProcessResult_Equality_IsSymmetric()
    {
        ProcessResult a = ArbitraryProcessResult();
        ProcessResult b = ArbitraryProcessResult();

        await Assert.That(a.Equals(b)).IsEqualTo(b.Equals(a));
    }

    [Test]
    public async Task ProcessResult_Equality_IsTransitive()
    {
        ProcessResult a = ArbitraryProcessResult();
        ProcessResult b = ArbitraryProcessResult();
        ProcessResult c = ArbitraryProcessResult();

        await Assert.That(a.Equals(c)).IsTrue();
    }

    [Test]
    public async Task ProcessResult_EqualInstances_HaveEqualHashCodes()
    {
        ProcessResult a = ArbitraryProcessResult();
        ProcessResult b = ArbitraryProcessResult();

        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task BufferedProcessResult_Equality_IsReflexive()
    {
        BufferedProcessResult a = ArbitraryBufferedProcessResult();

        await Assert.That(a.Equals(a)).IsTrue();
    }

    [Test]
    public async Task BufferedProcessResult_Equality_IsSymmetric()
    {
        BufferedProcessResult a = ArbitraryBufferedProcessResult();
        BufferedProcessResult b = ArbitraryBufferedProcessResult();

        await Assert.That(a.Equals(b)).IsEqualTo(b.Equals(a));
    }

    [Test]
    public async Task BufferedProcessResult_Equality_IsTransitive()
    {
        BufferedProcessResult a = ArbitraryBufferedProcessResult();
        BufferedProcessResult b = ArbitraryBufferedProcessResult();
        BufferedProcessResult c = ArbitraryBufferedProcessResult();

        await Assert.That(a.Equals(c)).IsTrue();
    }

    [Test]
    public async Task BufferedProcessResult_EqualInstances_HaveEqualHashCodes()
    {
        BufferedProcessResult a = ArbitraryBufferedProcessResult();
        BufferedProcessResult b = ArbitraryBufferedProcessResult();

        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task ProcessConfiguration_Equality_IsReflexive()
    {
        ProcessConfiguration a = new("foo.exe", "arg1");

        await Assert.That(a.Equals(a)).IsTrue();
    }

    [Test]
    public async Task ProcessConfiguration_Equality_IsSymmetric()
    {
        ProcessConfiguration a = new("foo.exe", "arg1");
        ProcessConfiguration b = new("foo.exe", "arg1");

        await Assert.That(a.Equals(b)).IsEqualTo(b.Equals(a));
    }

    [Test]
    public async Task ProcessConfiguration_Equality_IsTransitive()
    {
        ProcessConfiguration a = new("foo.exe", "arg1");
        ProcessConfiguration b = new("foo.exe", "arg1");
        ProcessConfiguration c = new("foo.exe", "arg1");

        await Assert.That(a.Equals(c)).IsTrue();
    }

    [Test]
    public async Task ProcessConfiguration_EqualInstances_HaveEqualHashCodes()
    {
        ProcessConfiguration a = new("foo.exe", "arg1");
        ProcessConfiguration b = new("foo.exe", "arg1");

        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task ShellInformation_Equality_IsReflexive()
    {
        ShellInformation a = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));

        await Assert.That(a.Equals(a)).IsTrue();
    }

    [Test]
    public async Task ShellInformation_Equality_IsSymmetric()
    {
        ShellInformation a = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));
        ShellInformation b = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));

        await Assert.That(a.Equals(b)).IsEqualTo(b.Equals(a));
    }

    [Test]
    public async Task ShellInformation_Equality_IsTransitive()
    {
        ShellInformation a = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));
        ShellInformation b = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));
        ShellInformation c = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));

        await Assert.That(a.Equals(c)).IsTrue();
    }

    [Test]
    public async Task ShellInformation_EqualInstances_HaveEqualHashCodes()
    {
        ShellInformation a = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));
        ShellInformation b = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));

        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task ShellInformation_CaseDifferingPaths_AreEqualWithEqualHashCodes()
    {
        ShellInformation a = new("bash", new FileInfo("C:\\SHELLS\\PWSH.EXE"), new Version(5, 1));
        ShellInformation b = new("bash", new FileInfo("c:\\shells\\pwsh.exe"), new Version(5, 1));

        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(b.GetHashCode()).IsEqualTo(a.GetHashCode());
    }

    [Test]
    public async Task ShellInformation_VersionDifference_DistinguishesEquality()
    {
        ShellInformation a = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));
        ShellInformation b = new("bash", new FileInfo("/bin/bash"), new Version(5, 2));

        await Assert.That(a.Equals(b)).IsFalse();
    }
}
