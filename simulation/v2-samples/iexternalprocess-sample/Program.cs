/*
    CliInvoke v2 Simulation Sample - IExternalProcess direct lifecycle
    Pinned to CliInvoke 2.11.0 for gate repeatability.

    Demonstrates v2-style code using ExternalProcess with settable
    Configuration and ExitConfiguration properties — a pattern that
    will need migration in v3 where properties became read-only.
*/

#pragma warning disable CS0618 // Type or member is obsolete — deliberate v2 usage

using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Processes;
using CliInvoke.Piping;

// v2 style: build a ProcessConfiguration with the v2 constructor.
// In v2, TargetFilePath was a regular { get; set; } property (not init-only).
ProcessConfiguration configuration = new(
    "dotnet",
    "--version",
    redirectOutputs: true);

// v2 style: create ExternalProcess with the resolver + pipeHandler + targetPath constructor.
// In v3, this constructor was removed; callers must use the
// (IFilePathResolver, ProcessConfiguration, ProcessExitConfiguration?) form.
ExternalProcess process = new(
    FilePathResolver.Shared,
    ProcessPipeHandler.Shared,
    configuration.TargetFilePath);

// v2 style: set ExitConfiguration via the property setter.
// In v3, ExitConfiguration became get-only (init-only).
process.ExitConfiguration = ProcessExitConfiguration.Default;

// v2 style: replace the entire Configuration object after construction.
// In v2, Configuration was a { get; set; } property on IExternalProcess.
// In v3, Configuration became init-only.
process.Configuration = configuration;

// v2 style: start the process and wait for buffered exit.
await process.StartAsync(CancellationToken.None);
BufferedProcessResult result = await process.WaitForBufferedExitOrTimeoutAsync(
    CancellationToken.None);

Console.WriteLine($"iexternalprocess-sample: exit={result.ExitCode} output={result.StandardOutput.Trim()}");
