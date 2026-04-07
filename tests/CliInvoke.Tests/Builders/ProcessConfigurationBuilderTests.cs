using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Security;
using System.Text;

namespace CliInvoke.Tests.Builders;

public class ProcessConfigurationBuilderTests
{
    [Test]
    public async Task TestDefaultConfiguration()
    {
        // Arrange
        IProcessConfigurationBuilder processConfigBuilder = new
            ProcessConfigurationBuilder("foo");

        // Act
        ProcessConfiguration builtCommand = processConfigBuilder.Build();

        // Assert 
        await Assert.That(builtCommand.TargetFilePath).IsEqualTo("foo");
        await Assert.That(builtCommand.Arguments).IsEqualTo(string.Empty);
        await Assert.That(builtCommand.WorkingDirectoryPath).IsEqualTo(Directory.GetCurrentDirectory());
        await Assert.That(builtCommand.EnvironmentVariables).IsEmpty();
        await Assert.That(builtCommand.StandardInputEncoding.Equals(Encoding.Default) &&
                          builtCommand.StandardOutputEncoding.Equals(Encoding.Default) &&
                          builtCommand.StandardErrorEncoding.Equals(Encoding.Default)).IsTrue();
        await Assert.That(builtCommand.Credential).IsEqualTo(UserCredential.Null);
        await Assert.That(builtCommand.StandardInput).IsEqualTo(StreamWriter.Null);
        await Assert.That(builtCommand.Credential).IsEqualTo(UserCredential.Null);
        await Assert.That(builtCommand.ResourcePolicy).IsEqualTo(ProcessResourcePolicy.Default);
        await Assert.That(builtCommand.WindowCreation).IsFalse();
        await Assert.That(builtCommand.UseShellExecution).IsFalse();
        await Assert.That(builtCommand.RequiresAdministrator).IsFalse();
    }

    [Test]
    public void WithResourcePolicy_ShouldSetResourcePolicy()
    {
        // TODO: Test WithResourcePolicy method
    }

    [Test]
    public void Build_ShouldReturnConfiguration()
    {
        // TODO: Test Build method
    }

    [Test]
    public async Task TestIncompatiblePipingOptionsThrowsException()
    {
        IProcessConfigurationBuilder processConfigBuilder =
            new ProcessConfigurationBuilder("foo");

        //Assert
        await Assert.That(() =>
        {
            processConfigBuilder.UseShellExecution(true)
                .SetStandardInputPipe(new StreamWriter(Console.OpenStandardInput()));
        }).Throws<ArgumentException>();
    }

    [Test]
    public async Task TestTargetFileReconfigured()
    {
        //Arrange
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo");

        //Act
        processConfigBuilder = processConfigBuilder.SetTargetFilePath("bar");

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.TargetFilePath).IsEqualTo("bar");
    }

    [Test]
    public async Task TestArgumentsReplaced()
    {
        //Arrange
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
            .SetArguments("--arg-value=value");

        //Act
        ProcessConfiguration newArguments = processConfigBuilder.SetArguments("--flag")
            .Build();

        //Assert
        await Assert.That(newArguments).IsNotEqualTo(processConfigBuilder.Build());
    }

    [SupportedOSPlatform("windows")]
    [Test]
    public async Task TestReconfiguredUserCredential()
    {
        //Arrange
        SecureString password = new SecureString();
        password.AppendChar('1');
        password.AppendChar('2');
        password.AppendChar('3');
        password.AppendChar('4');

        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
            .SetUserCredential(new UserCredential("",
                "admin",
                password,
                false));

        //Act
        SecureString password2 = new SecureString();
        password2.AppendChar('9');
        password2.AppendChar('8');
        password2.AppendChar('7');
        password2.AppendChar('6');

        UserCredential userCredential = new UserCredential("",
            "root",
            password2,
            false);

        processConfigBuilder = processConfigBuilder.SetUserCredential(userCredential);

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.Credential).IsEqualTo(userCredential);
    }


    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [Test]
    public async Task TestReconfiguredResourcePolicy()
    {
        //Arrange
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
            .SetProcessResourcePolicy(ProcessResourcePolicy.Default);


        //Arrange
        ProcessResourcePolicy resourcePolicy = new ProcessResourcePolicy(null,
            null,
            null,
            ProcessPriorityClass.AboveNormal);

        processConfigBuilder = processConfigBuilder.SetProcessResourcePolicy(resourcePolicy);

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.ResourcePolicy).IsEqualTo(resourcePolicy);
    }

    [Test]
    public async Task TestReconfiguredAdminPrivileges()
    {
        //Act
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo");

        //Arrange
        processConfigBuilder = processConfigBuilder.RequireAdministratorPrivileges();

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.RequiresAdministrator).IsTrue();
    }

    [Test]
    public async Task TestReconfiguredWorkingDirectory()
    {
        //Act
        IProcessConfigurationBuilder processConfigBuilder = new ProcessConfigurationBuilder("foo")
            .SetWorkingDirectory("dir");

        //Arrange
        processConfigBuilder = processConfigBuilder.SetWorkingDirectory("dir2");

        //Assert
        ProcessConfiguration command = processConfigBuilder.Build();
        await Assert.That(command.WorkingDirectoryPath).IsEqualTo("dir2");
    }
}