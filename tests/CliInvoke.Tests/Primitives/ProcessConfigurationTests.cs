namespace CliInvoke.Tests.Primitives;

public class ProcessConfigurationTests
{
    [Test]
    public async Task Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new ProcessConfiguration("foo.exe", null!))
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
}
