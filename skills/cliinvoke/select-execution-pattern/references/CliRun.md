# CliRun Usage

This reference explains how to use the `CliRun` static class to execute command-line processes with various configurations and behaviors.

## Basic Usage
Show a simple example of executing a process and capturing buffered output:
```csharp
using CliInvoke;

// Execute a process and capture buffered output
BufferedProcessResult result = await CliRun.RunBufferedAsync("dotnet", "--version");

// Handle the result
if (result.ExitCode != 0)
{
    Console.Error.WriteLine($"Process failed with exit code {result.ExitCode}");
    Console.Error.WriteLine(result.StandardError);
}
else
{
    Console.WriteLine(result.StandardOutput);
}
```

## Usage with Process Exit Configuration
Demonstrate how to set timeouts or other exit configurations:
```csharp
using CliInvoke;
using CliInvoke.Core.Processes;

// Create exit configuration with timeout policy
var exitConfig = new ProcessExitConfiguration(
    ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromSeconds(30))
);

// Execute the process with exit configuration
BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "dotnet",
    "build",
    exitConfiguration: exitConfig
);

// Check for errors using the helper method
if (result.HasErrors())
{
    Console.Error.WriteLine("Process encountered errors:");
    Console.Error.WriteLine(result.StandardError);
}
else
{
    Console.WriteLine("Process completed successfully:");
    Console.WriteLine(result.StandardOutput);
}
```

## Available Execute Methods
Detail the three main method groups (each with two overloads):
1. **RunAsync** - Returns a `ProcessResult` with exit code only
   - `RunAsync(string targetFilePath, string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null, CancellationToken cancellationToken = default)`
   - `RunAsync(ProcessConfiguration configuration, ProcessExitConfiguration? exitConfiguration = null, CancellationToken cancellationToken = default)`

2. **RunBufferedAsync** - Returns a `BufferedProcessResult` with exit code, standard output, and standard error
   - `RunBufferedAsync(string targetFilePath, string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null, CancellationToken cancellationToken = default)`
   - `RunBufferedAsync(ProcessConfiguration configuration, ProcessExitConfiguration? exitConfiguration = null, CancellationToken cancellationToken = default)`

3. **RunPipedAsync** - Returns a `PipedProcessResult` for scenarios requiring input/output piping
   - `RunPipedAsync(string targetFilePath, string arguments = "", string? workingDirectory = null, TimeSpan? timeoutTimeSpan = null, CancellationToken cancellationToken = default)`
   - `RunPipedAsync(ProcessConfiguration configuration, ProcessExitConfiguration? exitConfiguration = null, CancellationToken cancellationToken = default)`

## Note on Static Usage
Using the static `CliRun` class involves the following trade-offs:
- **Advantages**: Zero boilerplate – no DI container or factories required; most arguments are optional with sensible defaults for common use cases.
- **Disadvantages**: Limited flexibility – cannot change resource policies, interrupt strategies, or start-logic customisations; harder to replace the underlying invoker for unit testing or alternative back-ends.