using System.Collections.Generic;
using CliInvoke.Core.Extensibility.Factories;
using CliInvoke.Extensibility.Factories;
using Assert = Xunit.Assert;

namespace CliInvoke.Tests.Factories;

public class RunnerProcessFactoryTests
{
    [Fact]
    public void CreateRunnerConfiguration_RequiresAdministrator_PreservesAdminFlag()
    {
        // Arrange
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration processConfigToBeRun = new ProcessConfigurationBuilder("target.exe")
            .SetArguments("--input value")
            .Build();

        ProcessConfiguration runnerProcessConfig = new ProcessConfigurationBuilder("runner.exe")
            .RequireAdministratorPrivileges()
            .Build();

        // Act
        ProcessConfiguration result = factory.CreateRunnerConfiguration(processConfigToBeRun, runnerProcessConfig);

        // Assert
        Assert.True(result.RequiresAdministrator);
    }

    [Fact]
    public void CreateRunnerConfiguration_RequiresAdministrator_PreservesOtherSettings()
    {
        // Arrange
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        var envVars = new Dictionary<string, string> { { "MY_VAR", "hello" } };

        ProcessConfiguration processConfigToBeRun = new ProcessConfigurationBuilder("target.exe")
            .SetArguments("--input value")
            .SetEnvironmentVariables(envVars)
            .SetStandardInputEncoding(System.Text.Encoding.UTF8)
            .Build();

        ProcessConfiguration runnerProcessConfig = new ProcessConfigurationBuilder("runner.exe")
            .RequireAdministratorPrivileges()
            .Build();

        // Act
        ProcessConfiguration result = factory.CreateRunnerConfiguration(processConfigToBeRun, runnerProcessConfig);

        // Assert
        Assert.True(result.RequiresAdministrator);
        Assert.Contains("--input value", result.Arguments);
        Assert.Equal(envVars, result.EnvironmentVariables);
        Assert.Equal(System.Text.Encoding.UTF8, result.StandardInputEncoding);
    }

    [Fact]
    public void CreateRunnerConfiguration_NoAdmin_DoesNotSetAdminFlag()
    {
        // Arrange
        IRunnerProcessFactory factory = new RunnerProcessFactory();

        ProcessConfiguration processConfigToBeRun = new ProcessConfigurationBuilder("target.exe")
            .SetArguments("--flag")
            .Build();

        ProcessConfiguration runnerProcessConfig = new ProcessConfigurationBuilder("runner.exe")
            .Build();

        // Act
        ProcessConfiguration result = factory.CreateRunnerConfiguration(processConfigToBeRun, runnerProcessConfig);

        // Assert
        Assert.False(result.RequiresAdministrator);
    }
}
