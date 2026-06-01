---
name: cliinvoke-v1-to-v2-migration
description: Mapping of V1 types and methods to their V2 equivalents to assist in upgrading CliInvoke installations. USE FOR mapping V1 types and methods to V2 equivalents during an upgrade. DO NOT USE FOR writing new V2 code from scratch without referencing V1.
compatibility: Requires one or more CliInvoke NuGet packages (such as CliInvoke.Core, CliInvoke, CliInvoke.Specialization, or CliInvoke.Extensions)
---

# CliInvoke V1 to V2 Migration

## Core Type Mapping

| V1 Type | V2 Replacement | Notes |
| :--- | :--- | :--- |
| `ICliCommandInvoker` / `CliCommandInvoker` | `IProcessInvoker` / `ProcessInvoker` | Primary successor for running process configurations. |
| `CliCommandConfiguration` | `ProcessConfiguration` | The configuration model has been renamed to be more process-centric. |
| `IProcessFactory` / `ProcessFactory` | `IProcessConfigurationFactory` / `ProcessConfigurationFactory` | Used for simple configuration creation. |
| `IPipedProcessRunner` / `PipedProcessRunner` | `IProcessInvoker` / `ProcessInvoker` | Logic merged into the main invoker. |
| `IProcessRunnerUtility` / `ProcessRunnerUtility` | `IProcessInvoker` / `ProcessInvoker` | Replaced by the more robust invoker pattern. |

## Method Signature Changes

### IProcessInvoker (formerly ICliCommandInvoker)

| V1 Method | V2 Method | Change Description |
| :--- | :--- | :--- |
| `ExecuteProcessAsync(process, config, token)` | `ExecuteAsync(config, exitConfig, disposeOfConfig, token)` | No longer requires a `Process` instance; `ProcessExitConfiguration` added. |
| `ExecuteBufferedProcessAsync(...)` | `ExecuteBufferedAsync(...)` | Renamed and signature updated to match `ExecuteAsync`. |

### IProcessConfigurationBuilder

v1 used `With*` prefix for configuration methods; v2 uses `Set*` or `Configure*`.

| V1 Method | V2 Method |
| :--- | :--- |
| `WithArguments(...)` | `SetArguments(...)` |
| `WithEnvironmentVariables(...)` | `SetEnvironmentVariables(...)` |
| `WithTargetFilePath(...)` | `SetTargetFilePath(...)` |
| `WithUserCredential(...)` | `SetUserCredential(...)` |
| `WithShellExecution(...)` | `ConfigureShellExecution(...)` |
| `WithWindowCreation(...)` | `ConfigureWindowCreation(...)` |

## Removed Functionality & Replacements

### Process Result Validation
- **V1**: `IProcessConfigurationBuilder.WithValidation(ProcessResultValidation)`
- **V2**: Validation is now part of `ProcessExitConfiguration`. Create a `ProcessExitConfiguration` with the desired `ProcessResultValidation` and pass it to the invoker's `Execute*` methods.

### Encoding Setup
- **V1**: `WithEncoding(...)`
- **V2**: Split into specific methods: `SetStandardInputEncoding`, `SetStandardOutputEncoding`, and `SetStandardErrorEncoding`.

## Migration Strategy

1. **Update Namespaces**: Replace older namespaces with the updated namespaces.
2. **Update Types**: Replace all instances of `CliCommand*` with `Process*`.
3. **Refactor Invocation**: Update calls to `ICliCommandInvoker` to use the new `ExecuteAsync`/`ExecuteBufferedAsync` signatures, ensuring `ProcessExitConfiguration` is used for timeouts and validation.
4. **Fix Builder Methods**: Rename `With*` methods to `Set*` or `Configure*`.

## Common Pitfalls

| Pitfall | Solution |
| :--- | :--- |
| Needing a custom timeout or specific result validation | Provide a `ProcessExitConfiguration` to `IProcessInvoker.ExecuteAsync` to override defaults or specify validation rules. |
| Using `With*` methods in V2 | Update builder calls to the new `Set*` or `Configure*` naming convention. |

This is a pure knowledge skill and does not invoke external tools.