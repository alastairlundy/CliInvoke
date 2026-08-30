---
name: select-execution-pattern
description: Guidance on choosing between CliRun, IProcessInvoker, and IExternalProcess based on requirements for simplicity, control, and testability. USE FOR choosing between CliRun, IProcessInvoker, or IExternalProcess based on DI needs, testability, lifecycle control, or middleware-based cross-cutting concerns. DO NOT USE FOR implementing the actual process logic.
---

# Select Execution Pattern

## When to Use
- When starting a new CliInvoke integration and choosing between `CliRun`, `IProcessInvoker`, and `IExternalProcess`.
- When refactoring existing code that uses the wrong pattern (e.g., `CliRun` in a DI-heavy service, or `IProcessInvoker` for an interactive shell).
- When evaluating trade-offs around testability, DI support, boilerplate, and lifecycle control.
- When the user needs cross-cutting concerns (logging, post-exit validation, platform wrapping) applied to every invocation — see the **Middleware-augmented `IProcessInvoker`** branch below.
- When the user's requirements mention scripting/prototyping, enterprise DI, or interactive process control.

## When not to use
- When implementing the actual process logic — this skill only guides the choice of pattern. Once chosen, load a skill specific to the pattern (e.g., `generate-process-configuration` for building configurations, or the execution reference docs).
- When the choice is already made and the question is about a specific API call or builder method.
- When the user wants to migrate between CliInvoke major versions — load `cliinvoke-v1-to-v2-migration` instead.

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

### 4. Middleware-augmented `IProcessInvoker` (Cross-cutting concerns)
**Use when:**
- You are already using `IProcessInvoker` (or `ProcessInvoker`) and want cross-cutting concerns applied to *every* invocation without changing call sites.
- The needs include logging process entry/exit and output, validating the result after exit (e.g., non-zero exit code), or transparently wrapping the command in PowerShell Core / Windows `cmd.exe`.
- You want a composable chain: `LoggingMiddleware`, `PostExitValidationMiddleware`, `PowerShellMiddleware`/`CmdMiddleware`.

**Key Characteristic:**
- Built with the `ProcessInvoker` constructor that accepts `IEnumerable<IProcessMiddleware>` (or a `MiddlewareItems? sharedItems` seed bag), or — more idiomatically — via the fluent `Use*` extension methods. Each `Use*` method returns a **new** `ProcessInvoker`, so they compose:
  - `UseLogging()` (`CliInvoke.Extensions.Middleware`)
  - `UsePostExitValidation(PostExitValidation.ExitCodeIsZero())` (`CliInvoke.Extensions.Middleware.Validation`)
  - `UsePowerShell()` / `UseCmd()` (`CliInvoke.Specializations.Middleware`)
- The terminal **Process Invocation Pipeline** (Configuration → Invoke → OS Process → Result) remains the leaf that actually starts and waits on the process; middleware wraps it in registration order. **Call sites are unchanged**: `ExecuteAsync` and `ExecuteBufferedAsync` work exactly as without middleware.
- Middleware is an invoker-level concern only. `CliRun` does **not** support middleware — if you need middleware, choose `IProcessInvoker`/`ProcessInvoker` instead. At the `IExternalProcess` layer there is no middleware either; middleware operates above the invoker.

**Key Characteristic:**
- Adds a thin, declarative cross-cutting layer on top of the `IProcessInvoker` pattern; combines well with DI (register the configured `ProcessInvoker`).

## Summary Table

| Requirement | `CliRun` | `IProcessInvoker` | `IExternalProcess` | `IProcessInvoker` + Middleware |
| :--- | :--- | :--- | :--- | :--- |
| **Boilerplate** | Minimal | Moderate | Significant | Moderate (fluent `Use*`) |
| **DI Support** | None | Native | Native | Native |
| **Testability** | Low | High | High | High |
| **Lifecycle Control** | Low | Moderate | High | Moderate |
| **Cross-cutting (logging/validation/platform)** | None | Manual | Manual | Built-in (`Use*`) |
| **Ideal For** | Scripting/Prototypes | Enterprise Apps | Power Users/Complex Lifecycles | Enterprise Apps needing logging/validation/platform wrap |

For detailed implementation examples on creating external processes, see the following references:
* [IExternalProcess](./references/IExternalProcessCreation.md)
* [IProcessInvoker](./references/IProcessInvoker.md)

## Common Pitfalls

| Pitfall | Solution |
| :--- | :--- |
| Using `CliRun` in a service that already utilizes DI | Switch to `IProcessInvoker` to leverage existing DI registrations and improve testability. |
| Using `IProcessInvoker` for interactive shells | Switch to `IExternalProcess` to allow real-time interaction with the process. |
| Needing logging, result validation, or PowerShell/`cmd` wrapping on every call | Use the fluent `Use*` extension methods on `ProcessInvoker` (e.g., `.UseLogging().UsePostExitValidation(PostExitValidation.ExitCodeIsZero())`) instead of hand-writing the cross-cutting logic in each call site. |
| Reaching for middleware but starting from `CliRun` | `CliRun` has no middleware support; construct a `ProcessInvoker` (optionally with `IEnumerable<IProcessMiddleware>` or `MiddlewareItems`) and use the `Use*` methods instead. |
| Assuming middleware disposes the result for you | Middleware returns the `ProcessResult` **un-disposed** — you remain responsible for disposing the `IExternalProcess` you receive and any `StandardInput`/`UserCredential` you supplied to the `ProcessConfiguration` you created. `ProcessConfiguration` itself is not disposable. |

This is a pure knowledge skill and does not invoke external tools.