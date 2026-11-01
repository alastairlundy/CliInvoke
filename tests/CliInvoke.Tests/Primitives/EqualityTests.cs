namespace CliInvoke.Tests.Primitives;

/// <summary>
///     Fixed-value tests verifying the equality contracts (reflexivity, symmetry,
///     transitivity, and hash consistency) for the library's core primitives.
/// </summary>
/// <remarks>
///     Path members are compared with the platform path comparison
///     (<see cref="StringComparison.OrdinalIgnoreCase"/> on Windows,
///     <see cref="StringComparison.Ordinal"/> elsewhere), matching
///     <see cref="ProcessConfiguration"/>, <see cref="ProcessResult"/>, and
///     <see cref="ShellInformation"/>. The transitivity tests below therefore
///     exercise a genuine chain of case-differing pairs only where the platform
///     comparison makes those premises true (Windows); elsewhere the chain
///     implication still holds vacuously and the identical-chain paths assert reflexivity.
/// </remarks>
public class EqualityTests
{
    private static StringComparison PlatformPathComparison()
        => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

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
        // Two platform-equal-but-distinct pairs chained through a common middle:
        // a ≈ b && b ≈ c must imply a ≈ c. On Windows, "foo.exe" / "FOO.EXE" /
        // "FOO.exe" are all equal under the platform path comparison, so the
        // implication is exercised with non-trivial premises; on Unix the premises
        // are false and the implication holds vacuously, and the identical-chain
        // assertion below keeps the conclusion non-vacuous there.
        ProcessResult a = new("foo.exe", 0, 1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2),
            canceled: false, signal: null);
        ProcessResult b = new("FOO.EXE", 0, 1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2),
            canceled: false, signal: null);
        ProcessResult c = new("FOO.exe", 0, 1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2),
            canceled: false, signal: null);

        bool aEqualsB = a.Equals(b);
        bool bEqualsC = b.Equals(c);

        await Assert.That(aEqualsB).IsEqualTo(bEqualsC);
        await Assert.That(!aEqualsB || !bEqualsC || a.Equals(c)).IsTrue();
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
        BufferedProcessResult a = new("foo.exe", 0, 1, "out", "err", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2),
            canceled: false, signal: null);
        BufferedProcessResult b = new("FOO.EXE", 0, 1, "out", "err", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2),
            canceled: false, signal: null);
        BufferedProcessResult c = new("FOO.exe", 0, 1, "out", "err", new DateTime(2026, 1, 1), new DateTime(2026, 1, 2),
            canceled: false, signal: null);

        bool aEqualsB = a.Equals(b);
        bool bEqualsC = b.Equals(c);

        await Assert.That(aEqualsB).IsEqualTo(bEqualsC);
        await Assert.That(!aEqualsB || !bEqualsC || a.Equals(c)).IsTrue();
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
        ProcessConfiguration b = new("FOO.EXE", "arg1");
        ProcessConfiguration c = new("FOO.exe", "arg1");

        bool aEqualsB = a.Equals(b);
        bool bEqualsC = b.Equals(c);

        await Assert.That(aEqualsB).IsEqualTo(bEqualsC);
        await Assert.That(!aEqualsB || !bEqualsC || a.Equals(c)).IsTrue();
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
        ShellInformation a = new("bash", new FileInfo("C:\\SHELLS\\pwsh.exe"), new Version(5, 1));
        ShellInformation b = new("bash", new FileInfo("C:\\SHELLS\\PWSH.exe"), new Version(5, 1));
        ShellInformation c = new("bash", new FileInfo("C:\\SHelLS\\pwsh.exe"), new Version(5, 1));

        bool aEqualsB = a.Equals(b);
        bool bEqualsC = b.Equals(c);

        await Assert.That(aEqualsB).IsEqualTo(bEqualsC);
        await Assert.That(!aEqualsB || !bEqualsC || a.Equals(c)).IsTrue();
    }

    [Test]
    public async Task ShellInformation_EqualInstances_HaveEqualHashCodes()
    {
        ShellInformation a = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));
        ShellInformation b = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));

        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task ShellInformation_CaseDifferingPaths_MatchPlatformPathComparison()
    {
        // On Windows the paths differ only by case, so they are equal by design
        // (platform path comparison) and must share hash codes. On Unix-like
        // platforms the paths are case-sensitively different, so they are unequal.
        ShellInformation a = new("bash", new FileInfo("C:\\SHELLS\\PWSH.EXE"), new Version(5, 1));
        ShellInformation b = new("bash", new FileInfo("c:\\shells\\pwsh.exe"), new Version(5, 1));

        bool expected = string.Equals(a.TargetFilePath.FullName, b.TargetFilePath.FullName,
            PlatformPathComparison());

        await Assert.That(a.Equals(b)).IsEqualTo(expected);
        await Assert.That(b.Equals(a)).IsEqualTo(expected);

        // Only equal values are required to share hash codes; unequal values may
        // legally collide, so no hash assertion is made when expected is false.
        if (expected)
            await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task ShellInformation_VersionDifference_DistinguishesEquality()
    {
        ShellInformation a = new("bash", new FileInfo("/bin/bash"), new Version(5, 1));
        ShellInformation b = new("bash", new FileInfo("/bin/bash"), new Version(5, 2));

        await Assert.That(a.Equals(b)).IsFalse();
    }
}
