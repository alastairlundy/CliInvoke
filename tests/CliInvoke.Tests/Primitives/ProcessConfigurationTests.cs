namespace CliInvoke.Tests.Primitives;

public class ProcessConfigurationTests
{
    [Test]
    public async Task Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new ProcessConfiguration("foo.exe", (string)null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_WithValidArguments_DoesNotThrow()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new ProcessConfiguration("foo.exe", "arg1"))
            .ThrowsNothing();
    }

    [Test]
    public async Task Constructor_WithDefaultArguments_DoesNotThrow()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new ProcessConfiguration("foo.exe"))
            .ThrowsNothing();
    }

    [Test]
    public async Task Constructor_WithNullTargetFilePath_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new ProcessConfiguration(null!, "arg1"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task NullEqualsNull_IsTrue()
    {
        ProcessConfiguration? a = null;
        ProcessConfiguration? b = null;

        await Assert.That(a == b).IsTrue();
        await Assert.That(a != b).IsFalse();
    }

    [Test]
    public async Task NullEqualsNonNull_IsFalse()
    {
        ProcessConfiguration? a = null;
        ProcessConfiguration b = new("foo.exe");

        await Assert.That(a == b).IsFalse();
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task NonNullEqualsNull_IsFalse()
    {
        ProcessConfiguration a = new("foo.exe");
        ProcessConfiguration? b = null;

        await Assert.That(a == b).IsFalse();
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task Equals_TargetFilePathDifferingOnlyByCase_MatchesPlatformPathComparison()
    {
        ProcessConfiguration upper = new(@"C:\Tools\X.EXE");
        ProcessConfiguration lower = new(@"C:\tools\x.exe");

        bool expected = OperatingSystem.IsWindows();

        await Assert.That(upper.Equals(lower)).IsEqualTo(expected);
        await Assert.That(upper == lower).IsEqualTo(expected);
        await Assert.That(upper != lower).IsEqualTo(!expected);
    }

    [Test]
    public async Task StaticEquals_BothNull_IsFalse()
    {
        // Kept behaviour, intentionally different from the == operator: the static
        // Equals(left, right) returns false when either operand is null (both null included).
        ProcessConfiguration? left = null;
        ProcessConfiguration? right = null;

        await Assert.That(ProcessConfiguration.Equals(left, right)).IsFalse();
    }
}
