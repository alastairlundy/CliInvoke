/*
    CliInvoke v2 Simulation Sample - IProcessInvoker with DI-style construction
    Pinned to CliInvoke 2.11.0 for gate repeatability.

    Demonstrates v2-style code using the deprecated ProcessInvoker
    constructor that takes IFilePathResolver + IProcessPipeHandler —
    a pattern that will need migration in v3.
*/

#pragma warning disable CS0618 // Type or member is obsolete — deliberate v2 usage

using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Piping;

// v2 style: use the deprecated two-argument ProcessInvoker constructor
// that takes IFilePathResolver and IProcessPipeHandler directly.
// In v3, this constructor was removed; callers must pass an
// IExternalProcessFactory instead.
ProcessInvoker invoker = new(FilePathResolver.Shared, ProcessPipeHandler.Shared);

// v2 style: build a ProcessConfiguration with the settable constructor.
// TargetFilePath is a regular { get; set; } property in v2 (not init-only).
ProcessConfiguration configuration = new(
    "dotnet",
    "--version",
    redirectOutputs: true);

// v2 style: ExecuteBufferedAsync with the disposeOfConfig parameter.
// In v3, the disposeOfConfig parameter was removed.
BufferedProcessResult result = await invoker.ExecuteBufferedAsync(configuration);

Console.WriteLine($"iprocessinvoker-sample: exit={result.ExitCode} output={result.StandardOutput.Trim()}");
