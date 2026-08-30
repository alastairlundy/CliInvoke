/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using CliInvoke.Core.Extensibility;
using CliInvoke.Extensibility;

namespace CliInvoke.Tests.Factories;

public class RunnerConfigurationFactoryTests
{
    [Test]
    public async Task CreateRunnerConfiguration_RequiresAdministrator_PreservesAdminFlag()
    {
        // Arrange
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        ProcessConfiguration processConfigToBeRun = new ProcessConfigurationBuilder("target.exe")
            .SetArguments("--input value")
            .Build();

        ProcessConfiguration runnerProcessConfig = new ProcessConfigurationBuilder("runner.exe")
            .RequireAdministratorPrivileges()
            .Build();

        // Act
        ProcessConfiguration result = factory.CreateRunnerConfiguration(processConfigToBeRun, runnerProcessConfig);

        // Assert
        await Assert.That(result.RequiresAdministrator).IsTrue();
    }

    [Test]
    public async Task CreateRunnerConfiguration_RequiresAdministrator_PreservesOtherSettings()
    {
        // Arrange
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        var envVars = new Dictionary<string, string> { { "MY_VAR", "hello" } };

        ProcessConfiguration processConfigToBeRun = new ProcessConfigurationBuilder("target.exe")
            .SetArguments("--input value")
            .ConfigureEnvironmentVariables(envSpec =>
            {
                envSpec.SetReadOnlyDictionary(envVars);
            })
            .SetEncoding(System.Text.Encoding.UTF8)
            .Build();

        ProcessConfiguration runnerProcessConfig = new ProcessConfigurationBuilder("runner.exe")
            .RequireAdministratorPrivileges()
            .Build();

        // Act
        ProcessConfiguration result = factory.CreateRunnerConfiguration(processConfigToBeRun, runnerProcessConfig);

        // Assert
        await Assert.That(result.RequiresAdministrator).IsTrue();
        // The factory delivers via ArgumentList (ProcessStartInfo.ArgumentList), not the
        // single Arguments string, so the source-of-truth assertion is on the list.
        await Assert.That(result.ArgumentList).Contains("--input");
        await Assert.That(result.ArgumentList).Contains("value");
        await Assert.That(result.EnvironmentVariables).IsEquivalentTo(envVars);
        await Assert.That(result.StandardInputEncoding).IsEqualTo(System.Text.Encoding.UTF8);
    }

    [Test]
    public async Task CreateRunnerConfiguration_NoAdmin_DoesNotSetAdminFlag()
    {
        // Arrange
        IRunnerConfigurationFactory factory = new RunnerConfigurationFactory();

        ProcessConfiguration processConfigToBeRun = new ProcessConfigurationBuilder("target.exe")
            .SetArguments("--flag")
            .Build();

        ProcessConfiguration runnerProcessConfig = new ProcessConfigurationBuilder("runner.exe")
            .Build();

        // Act
        ProcessConfiguration result = factory.CreateRunnerConfiguration(processConfigToBeRun, runnerProcessConfig);

        // Assert
        await Assert.That(result.RequiresAdministrator).IsFalse();
    }
}
