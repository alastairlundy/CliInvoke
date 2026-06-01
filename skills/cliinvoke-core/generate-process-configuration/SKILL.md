---
name: generate-process-configuration
description: Guidance on correctly utilizing IProcessConfigurationBuilder to create ProcessConfiguration objects, ensuring proper build and redirection setup. USE FOR guidelines on utilizing IProcessConfigurationBuilder to create ProcessConfiguration objects, including build and redirection setup. DO NOT USE FOR executing the resulting configuration.
compatibility: Requires one or more CliInvoke NuGet packages (such as CliInvoke.Core, CliInvoke, or CliInvoke.Specialization)
---
# Generate Process Configuration

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
- **For Piped Results:** Use `.SetStandardOutputPipe(StreamWriter)` or `.SetStandardErrorPipe(StreamReader)`.

### 4. Working Directory and Target File
- Specify the execution context using `.SetWorkingDirectory(string)`.
- If the target executable is not in the system PATH, specify the full path in the builder's constructor or use `.SetTargetFilePath(string)`.

## Common Pitfalls

| Pitfall | Solution |
| :--- | :--- |
| Forgetting `.Build()` | Call `.Build()` to generate the `ProcessConfiguration` instance required for execution; you cannot pass the builder itself to an invoker. |
| Missing Redirection | Set `SetOutputRedirection(true)` in the configuration when using `ExecuteBufferedAsync` to avoid empty outputs or errors. |
| Improper String Concatenation | Use `IEnumerable<string>` for arguments instead of manually concatenating them into a single string to ensure safer and more idiomatic platform-handling. |

For implementation examples, see the [references](./references/) directory.

This is a pure knowledge skill and does not invoke external tools.
