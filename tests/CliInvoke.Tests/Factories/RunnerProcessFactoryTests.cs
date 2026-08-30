/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Text;

using CliInvoke.Builders;
using CliInvoke.Core.Extensibility.Factories;
using CliInvoke.Extensibility.Factories;

using Xunit;

namespace CliInvoke.Tests.Factories;

/// <summary>
///     Backport of the v2.10.x admin-flag preservation regression tests.
///     Confirms that <see cref="RunnerProcessFactory"/> continues to forward
///     the runner's <c>RequiresAdministrator</c> flag (and other settings)
///     onto the produced <see cref="ProcessConfiguration"/> when the wrapped
///     command is composed from discrete argument tokens.
/// </summary>
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

        Dictionary<string, string> envVars = new() { { "MY_VAR", "hello" } };

        ProcessConfiguration processConfigToBeRun = new ProcessConfigurationBuilder("target.exe")
            .SetArguments("--input value")
            .SetEnvironmentVariables(envVars)
            .SetStandardInputEncoding(Encoding.UTF8)
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
        Assert.Equal(Encoding.UTF8, result.StandardInputEncoding);
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
