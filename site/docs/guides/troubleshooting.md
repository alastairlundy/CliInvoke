---
title: Troubleshooting
layout: simple
---

# Troubleshooting

Use this guide to identify the root cause of a CliInvoke failure by category
rather than by surface symptom. Each section names the failure mode, lists
the most common causes observed in this codebase, and gives a concrete
detection method.

## How to use this page

1. Identify which category matches the symptom (process hangs, exception on
   shutdown, exit code mismatch, file not found, etc.).
2. Walk the **Common causes** list top-to-bottom; each cause is a single,
   specific mistake with a fix.
3. Use the **Detection** subsection to confirm the cause with a tool
   before changing code.

---

## Resource Management

CliInvoke exposes exactly five [Resource-Owning Types](guides-resource-disposal.md#terminology)
that hold unmanaged handles or sensitive memory: `ProcessConfiguration`,
`IExternalProcess`, `PipedProcessResult`, `UserCredential`, and
`UserCredentialSpec`. Every reported leak in this library traces back
to one of these five.

### Symptoms

- `System.IO.IOException: Too many open files` (Linux/macOS) or
  `ERROR_HANDLE_DISK_FULL` / `0x80070005` Access Denied (Windows).
- `ObjectDisposedException` on `StandardOutput`, `StandardError`, or
  `StandardInput`.
- Heap grows monotonically; `SecureString` content still present in
  process memory after the configuration is logically done.
- Child processes outlive the parent (zombie processes on Unix).

### Common causes

1. **`IExternalProcess` is not disposed.** The invoker returns an
   `IExternalProcess` from `StartAsync`; the caller owns the lifetime.
   Wrap in `await using` (preferred) or call `Dispose()` in a `finally`
   block.
2. **`PipedProcessResult` is read but not disposed.** `StandardOutput` and
   `StandardError` are live streams that own OS handles. Dispose the
   result (or use `await using`) once reading is complete.
3. **Disposing a library-owned stream.** `StandardInput` on
   `ProcessConfiguration`, and `StandardOutput` / `StandardError` on
   `IExternalProcess`, are owned by the library. Disposing them
   independently corrupts the parent's state.
4. **A `UserCredential`, a standalone `UserCredentialSpec`, or a builder-owned `UserCredentialSpec` is not disposed.**
   All three hold a `SecureString`. A standalone `UserCredential` or a `UserCredentialSpec` you create
   and own must be wrapped in `using` (the spec and the `UserCredential` it builds have independent
   lifetimes). A `UserCredentialSpec` configured through `ProcessConfigurationBuilder.ConfigureUserCredential`
   is owned and disposed by the builder, so do not dispose it yourself; the library disposes any
   credential it places on the `ProcessConfiguration` when that configuration is disposed.
5. **Reusing a `ProcessConfiguration` after `Dispose()`.** Once disposed,
   the internal `StreamWriter` is closed. Create a new configuration for
   each invocation.
6. **Capturing a `ProcessConfiguration` into a closure** that extends
   beyond the invocation, leaking the `SecureString` until the closure
   is collected.

### Detection

| Platform | Command | What to look for |
|----------|---------|------------------|
| Any | `dotnet-counters monitor --process-id <pid> --counters System.Runtime` | Rising `gc-handle-count` or `allocated-bytes` with no plateau. |
| Any | `dotnet-counters monitor --process-id <pid> --counters Microsoft.AspNetCore.Hosting` (if hosted) | Open file handles via the `file-descriptors` counter on .NET 8+. |
| Linux | `ls /proc/<pid>/fd \| wc -l` and `ls -l /proc/<pid>/fd` | Handle count grows across invocations; pipes to defunct children. |
| macOS | `lsof -p <pid> \| wc -l` | Same as Linux, including Unix domain sockets to children. |
| Windows | Process Explorer → View → Lower Pane View → Handles | `Pipe` handles with no matching `Process` close; `File` handles to `cmd.exe` / `pwsh.exe`. |
| Windows | `Get-Process -Id <pid> \| Select-Object Handles, NonpagedSystemMemorySize64` | `Handles` rising; nonzero `ChildCount` after parent exit. |
| Any | `dotnet-gcdump collect -p <pid>` then `dotnet-gcdump analyze <file>` | Retained graph contains `SecureString` or `StreamWriter` after disposal should have run. |

For the canonical disposal contracts, see
[Resource Disposal](guides-resource-disposal.md).

---

## OS-Specifics

CliInvoke selects control adapters, shells, and path-matching behavior
based on `OperatingSystem.IsWindows()` /
`OperatingSystem.IsLinux()` / `OperatingSystem.IsMacOS()`. Code that
worked on one OS may fail on another when the developer assumed a
specific platform behavior.

### Symptoms

- `FileNotFoundException` for an executable that exists on disk.
- `Win32Exception` on Linux for a process-control call.
- Process exits immediately with no error output.
- Process refuses to terminate on cancellation; or terminates, but child
  processes remain.

### Common causes

1. **Case-sensitive path or executable name on Linux/macOS.**
   `FilePathResolver` uses `MatchCasing.CaseSensitive` on Unix; passing
   `/usr/local/bin/MyTool` will not resolve to `/usr/local/bin/mytool`.
   Verify exact case with `which <name>` (Unix) or `where.exe <name>`
   (Windows).
2. **Windows-only shells invoked on Unix.** `cmd`, `cmd.exe`, and
   Windows PowerShell's `powershell.exe` are not present on Unix. Use
   `CliInvoke.Specializations` configurations: `CmdProcessConfiguration`
   is Windows-only, but `PowershellProcessConfiguration` and
   `PowershellProcessInvoker` resolve to `pwsh` (PowerShell Core) and
   are supported on Windows, macOS, Mac Catalyst, Linux, and FreeBSD.
   Note that the PowerShell team's own support for FreeBSD is
   unofficial; CliInvoke does not require any PowerShell-side
   guarantees beyond `pwsh` being installed and runnable on the
   target host. For other Unix shells, supply the shell executable
   explicitly.
3. **macOS signal differences.** `SIGSTOP` is signal 17 on macOS and 19 on
   Linux. `UnixProcessControlAdapter` already accounts for this; do not
   hardcode signal numbers in caller code.
4. **Process-group vs. process-only termination.** On Unix, killing the
   parent PID does not signal its children. Use
   `ProcessExitBehaviour.ForcefulExit` to ensure child processes are
   terminated together.
5. **Domain credential passed on Linux.** `UserCredential.Domain` and
   `LoadUserProfile` are Windows-only concepts; the
   `WindowsProcessControlAdapter` ignores them on Unix and the value is
   silently lost.
6. **Path-separator assumptions.** Backslash-separated paths fail on
   Unix. Use `Path.Combine` or pass arguments as separate `string[]`
   entries rather than concatenating with `\`.

### Detection

| Platform | Command | What to look for |
|----------|---------|------------------|
| Any | `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)` in a scratch app | Confirms runtime detection matches expectation. |
| Linux | `which <executable>` | Missing executable; non-zero exit indicates the name is not on `PATH`. |
| Linux | `test -x <path> && echo executable` | Confirms the execute bit is set. |
| Windows | `where.exe <executable>` | PATH resolution matches the executable passed to `ProcessConfiguration`. |
| Linux | `ps -o stat= -p <pid>` | Single-character state code; `Z` = zombie, `T` = stopped, `R` = running. No header to parse. |
| macOS | `ps -o stat= -p <pid>` | Same as Linux. |
| Linux / macOS | `pgrep -P <ppid> -o stat=` | Lists child PIDs of `<ppid>` and their state codes; identifies orphaned children. |

Supported platforms and their status are listed in
[Supported Operating Systems](Supported-OperatingSystems.md). iOS, tvOS,
and Browser targets are not supported because `System.Diagnostics.Process`
is unavailable on those platforms.

---

## Async & Threading

CliInvoke is async-first. Most threading failures in caller code come
from misusing `CancellationToken`, mixing sync and async disposal, or
blocking on a task in a way that deadlocks under a captured
`SynchronizationContext`.

### Symptoms

- `OperationCanceledException` thrown even though cancellation was not
  requested.
- Process appears to hang at shutdown; the application does not exit.
- `TaskCanceledException` wrapping a `TimeoutException` at an unexpected
  point.
- Deadlock in a UI app (WinForms / WPF / MAUI) or ASP.NET (pre-Core)
  when the code blocks on `.Result` / `.Wait()`.
- `AggregateException` containing `ObjectDisposedException` after
  `await using` completes.

### Common causes

1. **Not passing `CancellationToken` to invoker methods.** Without a
   token, cancellation has no effect; the invoker ignores
   `ProcessExitConfiguration.RequestedCancellationExitBehaviour` until
   the process exits on its own.
2. **Confusing timeout cancellation with user cancellation.** A
   `ProcessTimeoutPolicy` that elapses triggers cancellation. If
   `cancellationThrowsException: true` is set, callers must catch
   `OperationCanceledException` regardless of whether the user or the
   timer caused it.
3. **Disposing a process from the wrong thread.** `IExternalProcess`
   implements `IAsyncDisposable`. In an async path, prefer `await
   using`; calling `Dispose()` synchronously from a thread-pool thread
   can starve the disposal work.
4. **Blocking on `.Result` or `.Wait()` under a captured context.**
   WinForms, WPF, and legacy ASP.NET install a single-threaded
   `SynchronizationContext`. The default `await` resumes on that
   context, which is blocked. Fix with `.ConfigureAwait(false)` in
   library code, or refactor the UI handler to `await` end-to-end.
5. **Capturing `CancellationToken.None` into a long-lived field.** A
   token captured at startup cannot be cancelled. Either accept the
   token as a parameter or build a `CancellationTokenSource` whose
   lifetime matches the operation.
6. **Cancellation after `IExternalProcess.Dispose()`.** Cancelling after
   the process is disposed raises `ObjectDisposedException`. Register
   the cancellation callback via `CancellationToken.Register` and check
   `IsCancellationRequested` before touching the process.

### Detection

| Tool | Command | What to look for |
|------|---------|------------------|
| `dotnet-trace` (cross-platform via EventPipe) | `dotnet-trace collect -p <pid> --providers Microsoft-DotNETCore:0x14C14FCC80` (Task / ThreadPool keywords) | ThreadPool starvation; tasks queued but never scheduled. Works on Windows, Linux, and macOS. |
| `dotnet-counters` | `dotnet-counters monitor -p <pid> --counters System.Threading` | `threadpool-thread-count` pinned at the minimum, `queue-length` rising. |
| `dotnet-dump` | `dotnet-dump collect -p <pid>` then `clrthreads` | Threads in `(GC)` or `(debugger)` for long periods; deadlock suspected if all worker threads are blocked on the same Monitor. |
| ETW (Windows only) | `dotnet-trace collect -p <pid> --providers Microsoft-Windows-DotNETRuntime:0x14C14FCC80:Verbose` | Task wait chains showing circular wait. The `Microsoft-Windows-DotNETRuntime` provider is ETW and exists only on Windows; use the EventPipe row above on Linux and macOS. |
| Code review | `rg -n '\.Result\b\|\.Wait()\b\|\.GetAwaiter\(\)\.GetResult\(\)' src/` (ripgrep is not preinstalled; install it separately, or use your editor's "Find in Files" on the same pattern) | Any of these inside library code (under `src/CliInvoke*`) is a defect — library code must be awaitable end-to-end. |

If the failure involves the process itself, see
[Resource Management](#resource-management) for disposal-related
deadlocks.

---

## Configuration & Timeouts

These failures are not bugs in caller threading or disposal — they are
configurations of `ProcessExitConfiguration`, `ProcessTimeoutPolicy`,
and `ProcessExitBehaviour` that produce surprising exit behavior.

### Symptoms

- Process never exits despite the configured timeout elapsing.
- Process is killed before it can write a partial result.
- `TimeoutException` thrown for a process that completed in time.

### Common causes

1. **`ProcessTimeoutPolicy.None` used by accident.** `None` is a
   zero-second, disabled policy. Use
   `ProcessTimeoutPolicy.FromTimeSpan(...)` or
   `ProcessTimeoutPolicy.Default` (3 minutes; the parameterless
   `ProcessTimeoutPolicy()` constructor returns a 2-minute policy with
   cancellation enabled).
2. **`ProcessExitBehaviour.WaitForExit` with cancellation.** When the
   behaviour is `WaitForExit`, the process is not signalled on
   cancellation; the caller must terminate the process manually.
3. **`ProcessExitConfiguration.CreateGraceful` paired with a very short
   timeout.** Graceful exit sends `SIGTERM`/`Ctrl+C` and waits. If the
   timeout is shorter than the application's cleanup time, the
   configuration forces a forceful exit and the buffered output is
   truncated.
4. **Timeout that is shorter than process startup.** Long-starting
   executables (e.g. JVM-based tools) trip the timeout before producing
   any output, producing a misleading "process hung" diagnosis.

Note: invokers return one of three result types — `ProcessResult`,
`BufferedProcessResult`, and `PipedProcessResult`. None of them
represent an exit *policy*; the policy is always on
`ProcessExitConfiguration` and is consulted before the result is
constructed.

### Detection

- Log the resolved `ProcessExitConfiguration` at the start of each
  invocation: `ILogger.LogDebug("Exit config: {Timeout}, {Behaviour}",
  policy.TimeoutThreshold, policy.TimeoutExitBehaviour)`.
- Reproduce locally with a longer timeout and `ProcessExitBehaviour.ForcefulExit`
  to distinguish "process is slow" from "process is hung".

---

## Diagnostics Tooling Reference

> The tools listed here are suggestions for further investigation. CliInvoke
> does not endorse, maintain, or warrant any of these third-party tools. Verify
> each tool's current version, license, and security posture before installing.
>
> Commands throughout this guide reference the diagnostic tooling shown below.
> Review any command before running it in your terminal emulator.

| Tool | Install | Purpose |
|------|---------|---------|
| `dotnet-counters` | `dotnet tool install -g dotnet-counters` | Real-time runtime metrics: handles, threads, allocations. Cross-platform. |
| `dotnet-trace` | `dotnet tool install -g dotnet-trace` | `EventPipe`-based traces for performance and deadlock analysis. Cross-platform (Windows, Linux, macOS). |
| `dotnet-dump` | `dotnet tool install -g dotnet-dump` | Memory dump collection and `clrthreads`, `dumpheap`, `gcroot` commands. Cross-platform. |
| `dotnet-gcdump` | `dotnet tool install -g dotnet-gcdump` | Heap snapshot to identify retained objects and `SecureString` leaks. Cross-platform. |
| `dotnet-symbol` | `dotnet tool install -g dotnet-symbol` | Symbol resolution for the above tools' output. Cross-platform. |
| ETW (Windows only) | Built into Windows; `xperf` from Windows SDK | Native Event Tracing for Windows. Use only on Windows. Prefer `dotnet-trace` (EventPipe) for portability. |
| Process Explorer (Windows) | `https://learn.microsoft.com/sysinternals/downloads/process-explorer` | Windows handle and DLL inspection. |
| `lsof` | Preinstalled on macOS; `apt install lsof` on Linux | Per-process file / handle listing. Not installed by default on most Linux distributions. |
| `pgrep` / `pkill` | `procps` package on Linux; preinstalled on macOS | POSIX process lookup; safer than parsing `ps` output. |

For environment-level process inspection on Linux, `/proc/<pid>/{fd,status,task}`
is always available without extra tooling.
