---
name: generate-process-configuration
description: Guidance on correctly utilizing IProcessConfigurationBuilder to create ProcessConfiguration objects, ensuring proper build and redirection setup. USE FOR guidelines on utilizing IProcessConfigurationBuilder to create ProcessConfiguration objects, including build and redirection setup. DO NOT USE FOR executing the resulting configuration.
compatibility: Requires one or more CliInvoke NuGet packages (such as CliInvoke.Core, CliInvoke, or CliInvoke.Specialization)
targets: CliInvoke 3.0 (spec API — see skills/README.md for version note)
---
# Generate Process Configuration

## When to Use
- When building a `ProcessConfiguration` instance via `IProcessConfigurationBuilder` to pass to an invoker.
- When configuring arguments, working directory, target file path, encoding, or output redirection.
- When the user is unsure how to set up piped (`SetStandardInputPipe`) versus buffered (`SetOutputRedirection(true)`) result capture.
- When auditing existing builder code for missing `.Build()` calls or missing redirection setup.

## When not to use
- When executing the resulting configuration — this skill covers *building* the configuration, not running it. For execution, load `select-execution-pattern` to choose an invoker.
- When managing disposal of `ProcessConfiguration` — load `implement-resource-lifecycle` instead.
- When migrating from V1 builder methods (`With*`) to V2 (`Set*`/`Configure*`) — load `cliinvoke-v1-to-v2-migration`.

## Mental Model: Stage 1 of the Process Invocation Pipeline

In CliInvoke v3, process invocation is modelled as a four-stage pipeline: **Configuration → Invoke → OS Process → Result**.
`ProcessConfiguration` is **Stage 1** — pure, value-bearing data describing *what* to run and *how* to start it. The invoker (Stage 2) reads it and never mutates it, with the single exception of platform middleware (`UsePowerShell` / `UseCmd`), which substitutes the command that runs inside the wrapped shell. Build the configuration correctly here and the rest of the pipeline is deterministic.

## Core Workflow

The standard workflow for creating a configuration is:
1. Instantiate an `IProcessConfigurationBuilder` (typically via `ProcessConfigurationBuilder`).
2. Apply configurations fluently using the builder's methods.
3. Call `.Build()` to produce the final `ProcessConfiguration` object.

## Key Implementation Guidelines

### 1. Always Call `.Build()`
The builder is a factory for the configuration model. You cannot pass the builder itself to an invoker; you must call `.Build()` to generate the `ProcessConfiguration` instance required for execution.

### 2. Setting Arguments
- Use `.SetArguments(string)` or `.SetArguments(IEnumerable<<stringstring>)` to specify the command-line arguments.
- When providing a list of arguments, CliInvoke handles the correct escaping/quoting based on the platform.

### 3. Setting Redirection
If you intend to capture output, you must explicitly configure redirection:
- **For Buffered Results:** Use `.SetOutputRedirection(true)`.
- **For Piped Results:** Use `.SetStandardInputPipe(StreamWriter)` to provide input to the process.

### 4. Working Directory and Target File
- Specify the execution context using `.SetWorkingDirectory(string)`.
- If the target executable is not in the system PATH, specify the full path in the builder's constructor or use `.SetTargetFilePath(string)`.

## Common Pitfalls

| Pitfall | Solution |
| :--- | :--- |
| Forgetting `.Build()` | Call `.Build()` to generate the `ProcessConfiguration` instance required for execution; you cannot pass the builder itself to an invoker. |
| Missing Redirection | Set `SetOutputRedirection(true)` in the configuration when using `ExecuteBufferedAsync` to avoid empty outputs or errors. |
| Improper String Concatenation | Use `IEnumerable<string>` for arguments instead of manually concatenating them into a single string to ensure safer and more idiomatic platform-handling. |

For implementation examples, see:
* [How to configure using Builders](./references/ConfiguringWithBuilders.md)
* [How to set values](./references/SettingValues.md)

This is a pure knowledge skill and does not invoke external tools.
