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
}
