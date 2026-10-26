/*
    CliInvoke.Tests
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using CliInvoke.Processes.Internal;

namespace CliInvoke.Tests.Processes;

/// <summary>
///     Verifies that environment variables, user credentials, and the admin verb
///     configured on <see cref="ProcessConfiguration"/> survive the control adapter's
///     <c>ApplyConfiguration</c> mutation order: the assembled
///     <see cref="ProcessStartInfo"/> must be assigned to the process
///     <em>before</em> the adapter applies them, so they reach the instance used to
///     start the process instead of being discarded with the wrapper's default instance.
///     It also pins the null-value contract: a null-valued entry removes the variable
///     from the child process (BCL semantics), while an empty string sets an empty value.
/// </summary>
public class AdapterConfigurationDeliveryTests
{
    [Test]
    public async Task ProcessWrapper_EnvironmentVariables_AppliedToStartInfo()
    {
        const string variableName = "CLIINVOKE_ADAPTER_TEST";
        const string variableValue = "adapter-delivered";

        ProcessConfiguration configuration = new(ProcessTestHelper.GetTargetFilePath(), string.Empty)
        {
            EnvironmentVariables = new Dictionary<string, string>
            {
                [variableName] = variableValue
            }
        };

        FilePathResolver resolver = new();
        ProcessWrapper process = new(configuration, resolver.ResolveFilePath(configuration.TargetFilePath));

        await Assert.That(process.StartInfo.Environment[variableName]).IsEqualTo(variableValue);
    }

    [Test]
    public async Task RunBufferedAsync_EnvironmentVariables_AreDeliveredToChildProcess()
    {
        // Runs a real child process that prints an environment variable configured on the
        // ProcessConfiguration, through the full CliRun buffered pipeline.
        string helperPath = ProcessTestHelper.GetSignalTrappingHelperPath();

        const string variableName = "CLIINVOKE_ENV_E2E_TEST";
        const string variableValue = "delivered-value-17654";

        ProcessConfiguration configuration = new(helperPath, ["echo-env", variableName])
        {
            EnvironmentVariables = new Dictionary<string, string>
            {
                [variableName] = variableValue
            }
        };

        BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration,
            ProcessExitConfiguration.CreateGraceful()).ConfigureAwait(true);

        // Exit code 1 means the child could not find the variable at all.
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).IsEqualTo(variableValue);
    }

    [Test]
    public async Task ProcessWrapper_NullEnvironmentVariableValue_OverridesInheritedValue()
    {
        // L6: a null value must reach StartInfo.Environment as null (BCL removal semantics)
        // instead of being silently skipped, which would leave the inherited parent value.
        const string variableName = "CLIINVOKE_L6_NULL_UNIT";

        Environment.SetEnvironmentVariable(variableName, "inherited-parent-value");
        try
        {
            ProcessConfiguration configuration = new(ProcessTestHelper.GetTargetFilePath(), string.Empty)
            {
                EnvironmentVariables = new Dictionary<string, string>
                {
                    [variableName] = null!
                }
            };

            FilePathResolver resolver = new();
            ProcessWrapper process = new(configuration, resolver.ResolveFilePath(configuration.TargetFilePath));

            await Assert.That(process.StartInfo.Environment[variableName]).IsNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [Test]
    public async Task RunBufferedAsync_NullEnvironmentVariableValue_RemovesInheritedVariableFromChildProcess()
    {
        // L6 end to end: the parent process defines the variable, the configuration nulls it,
        // and the child must not see it (echo-env exits 1 for "absent"; an inherited leak would
        // exit 0 and print the parent's value).
        string helperPath = ProcessTestHelper.GetSignalTrappingHelperPath();

        const string variableName = "CLIINVOKE_L6_NULL_E2E";
        const string inheritedValue = "inherited-secret-4821";

        Environment.SetEnvironmentVariable(variableName, inheritedValue);
        try
        {
            ProcessConfiguration configuration = new(helperPath, ["echo-env", variableName])
            {
                EnvironmentVariables = new Dictionary<string, string>
                {
                    [variableName] = null!
                }
            };

            BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration,
                ProcessExitConfiguration.CreateGraceful()).ConfigureAwait(true);

            await Assert.That(result.ExitCode).IsEqualTo(1);
            await Assert.That(result.StandardOutput).IsEqualTo(string.Empty);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [Test]
    public async Task RunBufferedAsync_EmptyEnvironmentVariableValue_SetsEmptyValueInsteadOfRemoving()
    {
        // Companion to the null case: an empty string is "set to empty", not "removed" -
        // echo-env exits 0 (present) with empty output, versus exit 1 for absent.
        string helperPath = ProcessTestHelper.GetSignalTrappingHelperPath();

        const string variableName = "CLIINVOKE_L6_EMPTY_E2E";

        Environment.SetEnvironmentVariable(variableName, "inherited-value");
        try
        {
            ProcessConfiguration configuration = new(helperPath, ["echo-env", variableName])
            {
                EnvironmentVariables = new Dictionary<string, string>
                {
                    [variableName] = string.Empty
                }
            };

            BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration,
                ProcessExitConfiguration.CreateGraceful()).ConfigureAwait(true);

            await Assert.That(result.ExitCode).IsEqualTo(0);
            await Assert.That(result.StandardOutput).IsEqualTo(string.Empty);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [Test]
    public async Task ProcessWrapper_RequiresAdministrator_SetsRunAsVerbOnStartInfo()
    {
        if (!OperatingSystem.IsWindows())
            return;

        ProcessConfiguration configuration = new(ProcessTestHelper.GetTargetFilePath(), string.Empty)
        {
            RequiresAdministrator = true
        };

        FilePathResolver resolver = new();
        ProcessWrapper process = new(configuration, resolver.ResolveFilePath(configuration.TargetFilePath));

        await Assert.That(process.StartInfo.Verb).IsEqualTo("runas");
    }

    [Test]
    public async Task ProcessWrapper_Credential_AppliedToStartInfo()
    {
        if (!OperatingSystem.IsWindows())
            return;

        ProcessConfiguration configuration = new(ProcessTestHelper.GetTargetFilePath(), string.Empty)
        {
            Credential = new UserCredential("SOMEDOMAIN", "someuser", null, true)
        };

        FilePathResolver resolver = new();
        ProcessWrapper process = new(configuration, resolver.ResolveFilePath(configuration.TargetFilePath));

#pragma warning disable CA1416
        await Assert.That(process.StartInfo.Domain).IsEqualTo("SOMEDOMAIN");
        await Assert.That(process.StartInfo.LoadUserProfile).IsTrue();
#pragma warning restore CA1416
        await Assert.That(process.StartInfo.UserName).IsEqualTo("someuser");
    }
}
