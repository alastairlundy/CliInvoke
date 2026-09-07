using System.Collections.Generic;
using System.Linq;

namespace CliInvoke.Tests.Primitives;

public class ProcessConfigurationArgumentsListTests
{
    [Test]
    public async Task ArgumentList_CarriesMergedInitValues()
    {
        // Arrange & Act
        ProcessConfiguration config = new()
        {
            TargetFilePath = "foo.exe",
            ArgumentList = ["--verbose", "--output", "result.txt"]
        };

        // Assert
        await Assert.That(config.ArgumentList).Count().IsEqualTo(3);
        await Assert.That(config.ArgumentList[0]).IsEqualTo("--verbose");
        await Assert.That(config.ArgumentList[1]).IsEqualTo("--output");
        await Assert.That(config.ArgumentList[2]).IsEqualTo("result.txt");
    }

    [Test]
    public async Task ArgumentList_SnapshotsInput()
    {
        // Arrange
        List<string> args = ["--verbose", "--debug"];

        // Act
        ProcessConfiguration config = new()
        {
            TargetFilePath = "foo.exe",
            ArgumentList = args
        };

        // Mutate original list
        args.Add("--extra");

        // Assert - configuration should not reflect the mutation
        await Assert.That(config.ArgumentList).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ArgumentList_NullBecomesEmpty()
    {
        // Arrange & Act
        ProcessConfiguration config = new()
        {
            TargetFilePath = "foo.exe",
            ArgumentList = null!
        };

        // Assert
        await Assert.That(config.ArgumentList).IsEmpty();
    }

    [Test]
    public async Task ArgumentList_EmptyListStaysEmpty()
    {
        // Arrange & Act
        ProcessConfiguration config = new()
        {
            TargetFilePath = "foo.exe",
            ArgumentList = Array.Empty<string>()
        };

        // Assert
        await Assert.That(config.ArgumentList).IsEmpty();
    }

    [Test]
    public async Task ArgumentList_DefaultIsEmpty()
    {
        // Arrange & Act
        ProcessConfiguration config = new()
        {
            TargetFilePath = "foo.exe"
        };

        // Assert
        await Assert.That(config.ArgumentList).IsEmpty();
    }

    [Test]
    public async Task Arguments_AndArgumentList_IndependentlyCoexist()
    {
        // Arrange & Act
        ProcessConfiguration config = new()
        {
            TargetFilePath = "foo.exe",
            Arguments = "--version",
            ArgumentList = ["--verbose", "--debug"]
        };

        // Assert
        await Assert.That(config.Arguments).IsEqualTo("--version");
        await Assert.That(config.ArgumentList).Count().IsEqualTo(2);
    }
}
