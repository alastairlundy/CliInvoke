---
name: implement-resource-lifecycle
description: Guidance on auditing and managing the lifecycle of disposable types in CliInvoke to prevent resource leaks and secure sensitive data. USE FOR auditing and managing disposable types (ProcessConfiguration, IExternalProcess, etc.) to prevent resource leaks. DO NOT USE FOR general C# memory management.
compatibility: Requires one or more CliInvoke NuGet packages (such as CliInvoke.Core, CliInvoke, or CliInvoke.Specialization)
targets: CliInvoke 3.0 (spec API — see skills/README.md for version note)
---
# Implement Resource Lifecycle

## When to Use
- When auditing code for proper disposal of CliInvoke's mandatory disposable types: `ProcessConfiguration`, `IExternalProcess`, `PipedProcessResult`, `UserCredential`, and the credential-build type for your version — `UserCredentialBuilder` on 2.x or `UserCredentialSpec` on 3.0.
- When deciding between `using` and `await using` in async methods.
- When investigating a suspected handle leak, memory pressure, or `SecureString` retention issue.
- When the user asks about `SecureString` cleanup or process/stream lifetime in CliInvoke.

## When not to use
- When the question is about general C#/.NET memory management, GC tuning, or `IDisposable` mechanics outside CliInvoke's specific types.
- When building or executing a configuration — load `generate-process-configuration` or `select-execution-pattern` instead.
- When migrating from V1 to V2 — load `cliinvoke-v1-to-v2-migration` for any disposal-related API renames.

## Middleware and Disposal

If you configure `ProcessInvoker` with middleware (`UseLogging`, `UsePostExitValidation`, `UsePowerShell`, `UseCmd`), **the disposal contract is unchanged**. Middleware returns the process result **un-disposed** to the caller — identical to a non-middleware invoker. You remain responsible for disposing `PipedProcessResult` (and its streams) and the `ProcessConfiguration` you created. The same checklist below applies whether or not middleware is in the chain.

## Mandatory Disposable Types

The following types MUST be disposed of (five on the 2.x API, six once `UserCredentialSpec` ships in 3.0). Failure to do so can lead to handle leaks, memory pressure, or sensitive data remaining in memory.

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

### 5. `UserCredentialSpec` (CliInvoke 3.0)
- **Reason**: A sealed configuration seam that holds a `SecureString` for passwords during credential construction. Implements `IDisposable` to clear the secure string from memory.
- **Pattern**: Always wrap the spec in a `using` block.
- **Timing**: Dispose immediately after calling `.Build()`.
- **Note**: This is the **3.0** replacement for the 2.x `UserCredentialBuilder`. Prefer it in new code **once you are on CliInvoke 3.0** (see the version note in `skills/README.md`).
- **Example**:
  ```csharp
  UserCredential credential;
  using (var spec = new UserCredentialSpec())
  {
      spec.SetUsername("admin").SetPassword(securePassword);
      credential = spec.Build();
  }
  // spec disposed here; SecureString cleared from memory
  ```

### 6. `UserCredentialBuilder` (CliInvoke 2.x — current released API)
- **Reason**: Holds a `SecureString` during the construction of credentials.
- **Pattern**: Always wrap the builder in a `using` block.
- **Timing**: Dispose immediately after calling `.Build()`.
- **Note**: This is the **current released (2.x)** type. It is superseded by `UserCredentialSpec` in 3.0; use `UserCredentialSpec` once you move to 3.0.
- **Example**: See [UserCredentialBuilder.md](./references/UserCredentialBuilder.md)

## Implementation Checklist

- [ ] Every instance of the 6 types above is wrapped in a `using` or `await using` block.
- [ ] `IExternalProcess` and `PipedProcessResult` use `await using` in async methods.
- [ ] The credential-build type is disposed of after `.Build()` is called — `UserCredentialSpec` on 3.0, `UserCredentialBuilder` on 2.x.
- [ ] `ProcessConfiguration` is disposed of after the process has completed and results are processed.

For detailed usage examples, see the following references:
* [IExternalProcess example](./references/IExternalProcess.md)
* [PipedProcessResult examples](./references/PipedProcessResult.md)
* [ProcessConfiguration examples](./references/ProcessConfiguration.md)
* [UserCredential examples](./references/UserCredential.md)
* [UserCredentialSpec and UserCredentialBuilder examples](./references/UserCredentialBuilder.md)

## Common Pitfalls

| Pitfall | Solution |
| :--- | :--- |
| Ignoring `PipedProcessResult` disposal | Ensure `PipedProcessResult` is disposed of after reading all data to release associated streams. |
| Using `using` instead of `await using` for processes | Use `await using` for `IExternalProcess` and `PipedProcessResult` in async contexts to ensure non-blocking resource cleanup. |
| Assuming middleware disposes the result | Middleware returns the result un-disposed; dispose `PipedProcessResult` (and its streams) and the `ProcessConfiguration` yourself, even when `Use*` middleware is configured on the invoker. |

This is a pure knowledge skill and does not invoke external tools.