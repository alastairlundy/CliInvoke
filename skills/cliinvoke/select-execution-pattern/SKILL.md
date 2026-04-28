---
name: select-execution-pattern
description: Guidance on choosing between CliRun, IProcessInvoker, and IExternalProcess based on requirements for simplicity, control, and testability. USE FOR: choosing between CliRun, IProcessInvoker, or IExternalProcess based on DI needs, testability, or lifecycle control. DO NOT USE FOR: implementing the actual process logic.
---
# Select Execution Pattern

## Decision Logic

When deciding which pattern to use, evaluate the requirements against the following criteria:

### 1. Beginner-Friendly / Quickstart (`CliRun`)
**Use when:**
- The user needs to run a simple command quickly.
- There is no need for Dependency Injection (DI).
- Default behaviors (like the 2-minute timeout threshold and graceful exit) are acceptable.
- Minimal boilerplate is preferred over granular control.

**Key Characteristic:**
- Zero setup; call `CliRun.RunAsync` or `CliRun.RunBufferedAsync` directly.

### 2. End-to-End / DI-Friendly (`IProcessInvoker`)
**Use when:**
- The application uses Dependency Injection.
- Testability is a priority (need to mock the invoker).
- Per-invocation control over `ProcessConfiguration` and `ProcessExitConfiguration` is required.
- The user wants to abstract the "how" of process execution from the "what" (configuration).

**Key Characteristic:**
- Requires registering `IProcessInvoker` in the DI container.

### 3. Flexible / Process-User Familiar (`IExternalProcess` & `IExternalProcessFactory`)
**Use when:**
- Granular control over the process lifecycle (Start -> Interact -> Stop) is required.
- The user needs to interact with the process while it is running.
- The scenario mirrors the `System.Diagnostics.Process` workflow but requires the safety and rich API of CliInvoke.
- High control over the start/stop sequence is needed.

**Key Characteristic:**
- Uses a factory to create an instance of `IExternalProcess`.

## Summary Table

| Requirement | `CliRun` | `IProcessInvoker` | `IExternalProcess` |
| :--- | :--- | :--- | :--- |
| **Boilerplate** | Minimal | Moderate | Significant |
| **DI Support** | None | Native | Native |
| **Testability** | Low | High | High |
| **Lifecycle Control** | Low | Moderate | High |
| **Ideal For** | Scripting/Prototypes | Enterprise Apps | Power Users/Complex Lifecycles |

For detailed implementation examples on creating external processes, see the [references](./references/) directory.

## Common Pitfalls

| Pitfall | Solution |
| :--- | :--- |
| Using `CliRun` in a service that already utilizes DI | Switch to `IProcessInvoker` to leverage existing DI registrations and improve testability. |
| Using `IProcessInvoker` for interactive shells | Switch to `IExternalProcess` to allow real-time interaction with the process. |

This is a pure knowledge skill and does not invoke external tools.