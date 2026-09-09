/*
    CliInvoke v2 Simulation Sample - CliRun Static Facade
    Pinned to CliInvoke 2.11.0 for gate repeatability.

    Demonstrates v2-style code using ProcessConfigurationFactory
    and CliRun — patterns that will need migration in v3.
*/

#pragma warning disable CS0618 // Type or member is obsolete — deliberate v2 usage

using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Factories;

// v2 style: use ProcessConfigurationFactory to build configuration.
// In v3, ProcessConfigurationFactory was removed; callers must use
// init-only construction or the builder directly.
ProcessConfigurationFactory factory = new();

// Create a configuration for running `dotnet --version`.
// v2 style: factory.Create returns a disposable ProcessConfiguration
// with settable TargetFilePath and redirect flags.
using ProcessConfiguration configuration = factory.Create(
    "dotnet",
    "--version");

// v2 style: use CliRun.RunBufferedAsync with a ProcessConfiguration object.
// In v3, CliRun no longer uses ProcessConfigurationFactory internally,
// and the method signatures changed.
BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration);

Console.WriteLine($"clirun-sample: exit={result.ExitCode} output={result.StandardOutput.Trim()}");
