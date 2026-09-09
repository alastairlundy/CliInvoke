namespace CliInvoke.Tests.Primitives;

public class ProcessConfigurationInitConstructionTests
{
    [Test]
    public async Task Init_WithNullTargetFilePath_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.That(() =>
        {
            ProcessConfiguration _ = new()
            {
                TargetFilePath = null!
            };
        }).Throws<ArgumentException>();
    }

    [Test]
    public async Task Init_WithEmptyTargetFilePath_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.That(() =>
        {
            ProcessConfiguration _ = new()
            {
                TargetFilePath = string.Empty
            };
        }).Throws<ArgumentException>();
    }

    [Test]
    public async Task Init_WithNonExistentWorkingDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        string nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");

        // Act & Assert
        await Assert.That(() =>
        {
            ProcessConfiguration _ = new()
            {
                TargetFilePath = "foo.exe",
                WorkingDirectoryPath = nonExistentPath
            };
        }).Throws<DirectoryNotFoundException>();
    }

    [Test]
    public async Task Init_WithValidWorkingDirectory_DoesNotThrow()
    {
        // Arrange
        string tempDir = Path.GetTempPath();

        // Act & Assert
        await Assert.That(() =>
        {
            ProcessConfiguration _ = new()
            {
                TargetFilePath = "foo.exe",
                WorkingDirectoryPath = tempDir
            };
        }).ThrowsNothing();
    }

    [Test]
    public async Task Init_WithoutWorkingDirectory_DoesNotThrow()
    {
        // Arrange & Act & Assert
        await Assert.That(() =>
        {
            ProcessConfiguration _ = new()
            {
                TargetFilePath = "foo.exe"
            };
        }).ThrowsNothing();
    }

    [Test]
    public async Task ConvenienceConstructor_WithValidTarget_DoesNotThrow()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new ProcessConfiguration("foo.exe")).ThrowsNothing();
    }

    [Test]
    public async Task ConvenienceConstructor_WithNullTarget_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.That(() => new ProcessConfiguration(null!)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Init_WithValidTarget_SetsRequiredProperty()
    {
        // Arrange & Act
        ProcessConfiguration config = new()
        {
            TargetFilePath = "foo.exe"
        };

        // Assert
        await Assert.That(config.TargetFilePath).IsEqualTo("foo.exe");
    }

    [Test]
    public async Task Init_DefaultProperties_HaveExpectedValues()
    {
        // Arrange & Act
        ProcessConfiguration config = new()
        {
            TargetFilePath = "foo.exe"
        };

        // Assert
        await Assert.That(config.Arguments).IsEmpty();
        await Assert.That(config.OutputRedirection).IsFalse();
        await Assert.That(config.RequiresAdministrator).IsFalse();
        await Assert.That(config.WindowCreation).IsFalse();
        await Assert.That(config.UseShellExecution).IsFalse();
        await Assert.That(config.RedirectStandardInput).IsFalse();
        await Assert.That(config.Credential).IsEqualTo(UserCredential.Null);
        await Assert.That(config.ArgumentList).IsEmpty();
        await Assert.That(config.EnvironmentVariables).IsEmpty();
    }
}
