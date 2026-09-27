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

        ProcessConfiguration configuration = new(helperPath, new[] { "echo-env", variableName })
        {
            EnvironmentVariables = new Dictionary<string, string>
            {
                [variableName] = variableValue
            }
        };

        BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration,
            ProcessExitConfiguration.CreateGraceful());

        // Exit code 1 means the child could not find the variable at all.
        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.StandardOutput).IsEqualTo(variableValue);
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
