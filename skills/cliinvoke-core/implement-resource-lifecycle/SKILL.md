---
name: implement-resource-lifecycle
description: Guidance on auditing and managing the lifecycle of disposable types in CliInvoke to prevent resource leaks and secure sensitive data. USE FOR auditing and managing disposable types (ProcessConfiguration, IExternalProcess, etc.) to prevent resource leaks. DO NOT USE FOR general C# memory management.
compatibility: Requires one or more CliInvoke NuGet packages (such as CliInvoke.Core, CliInvoke, or CliInvoke.Specialization)
---
# Implement Resource Lifecycle

## Mandatory Disposable Types

The following five types MUST be disposed of. Failure to do so can lead to handle leaks, memory pressure, or sensitive data remaining in memory.

### 1. `ProcessConfiguration`
- **Reason**: Manages `StandardInput` (StreamWriter) and potentially a `SecureString` credential.
- **Pattern**: Use `using` or `await using`.
- **Ownership**: The creator of the configuration is responsible for its disposal.
- **Example**: See [ProcessConfiguration.md](./references/ProcessConfiguration.md)

### 2. `IExternalProcess`
- **Reason**: Wraps `System.Diagnostics.Process` and its associated OS handles.
- **Pattern**: Use `await using` (preferred) or `using`.
- **Timing**: Dispose immediately after the process is captured or the monitoring period ends.
- **Example**: See [IExternalProcess.md](./references/IExternalProcess.md)

### 3. `PipedProcessResult`
- **Reason**: Owns the `StandardOutput` and `StandardError` streams.
- **Pattern**: Use `await using` or `using`.
- **Timing**: Dispose after all data has been read from the streams.
- **Example**: See [PipedProcessResult.md](./references/PipedProcessResult.md)

### 4. `UserCredential`
- **Reason**: Contains a `SecureString` for passwords; needs to be cleared from memory.
- **Pattern**: `using` or explicit `.Dispose()`.
- **Note**: If assigned to a `ProcessConfiguration`, the configuration's disposal will also dispose the credential.
- **Example**: See [UserCredential.md](./references/UserCredential.md)

### 5. `UserCredentialBuilder`
- **Reason**: Holds a `SecureString` during the construction of credentials.
- **Pattern**: Always wrap the builder in a `using` block.
- **Timing**: Dispose immediately after calling `.Build()`.
- **Example**: See [UserCredentialBuilder.md](./references/UserCredentialBuilder.md)

## Implementation Checklist

- [ ] Every instance of the 5 types above is wrapped in a `using` or `await using` block.
- [ ] `IExternalProcess` and `PipedProcessResult` use `await using` in async methods.
- [ ] `UserCredentialBuilder` is disposed of after `.Build()` is called.
- [ ] `ProcessConfiguration` is disposed of after the process has completed and results are processed.

For detailed usage examples, see the following references:
* [IExternalProcess example](./references/IExternalProcess.md)
* [PipedProcessResult examples](./references/PipedProcessResult.md)
* [ProcessConfiguration examples](./references/ProcessConfiguration.md)
* [UserCredential examples](./references/UserCredential.md)
* [UserCredentialBuilder examples](./references//UserCredentialBuilder.md)

## Common Pitfalls

| Pitfall | Solution |
| :--- | :--- |
| Ignoring `PipedProcessResult` disposal | Ensure `PipedProcessResult` is disposed of after reading all data to release associated streams. |
| Using `using` instead of `await using` for processes | Use `await using` for `IExternalProcess` and `PipedProcessResult` in async contexts to ensure non-blocking resource cleanup. |

This is a pure knowledge skill and does not invoke external tools.