---
title: Resource Disposal
layout: simple
---

# Resource Disposal Guide

This is the canonical reference for resource management in CliInvoke. It
documents every public type in the library that implements `IDisposable`
(and, where applicable, `IAsyncDisposable`), the unmanaged resources
they own, and the exact disposal patterns callers must follow.

The goal of this page is to prevent two failure modes:

1. **Resource leaks** — files, pipes, kernel handles, and
   `SecureString` buffers left in memory after an invocation has
   finished.
2. **Handle exhaustion** — the platform error state reached when a
   process accumulates open handles faster than it releases them.

If you only read one section, read
[The Five Disposable Types](#the-five-disposable-types) and the
[Disposal Patterns](#disposal-patterns) summary.

## Terminology

A **Resource-Owning Type** is any CliInvoke type that holds, directly
or transitively, an unmanaged resource or a sensitive managed resource
that must be deterministically released. The library exposes exactly
five of them. Every other public type in the library is a
value-bearing immutable, an enum, or an interface contract and
requires no disposal.

## The Five Disposable Types

| # | Type | Disposal contract | Resources owned |
|---|------|-------------------|-----------------|
| 1 | [`ProcessConfiguration`](#1-processconfiguration) | `IDisposable` | `StreamWriter` (StandardInput), optional `UserCredential` |
| 2 | [`IExternalProcess`](#2-iexternalprocess) | `IDisposable` | The underlying `System.Diagnostics.Process` (pipes, handles, threads) |
| 3 | [`PipedProcessResult`](#3-pipedprocessresult) | `IDisposable` + `IAsyncDisposable` | `StandardOutput` and `StandardError` streams |
| 4 | [`UserCredential`](#4-usercredential) | `IDisposable` | `SecureString` password buffer |
| 5 | [`UserCredentialSpec`](#5-usercredentialspec) | `IDisposable` | `SecureString` password buffer staged for `Build()` |

No other public CliInvoke type implements `IDisposable`. If a type is
not in the table above, it does not need to be disposed.

### 1. `ProcessConfiguration`

Defined in `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`.

```csharp
public class ProcessConfiguration : IEquatable<ProcessConfiguration>, IDisposable
```

**What it owns**

- A `StreamWriter` for `StandardInput` — backed by a pipe handle
  created by the OS.
- An optional `UserCredential` — itself a `SecureString`-owning type
  (see [Type 4](#4-usercredential)).

**`Dispose()` behaviour** (line 201):

```csharp
public void Dispose()
{
    Credential?.Dispose();
    StandardInput?.Dispose();
}
```

Note that `Dispose()` is **synchronous**. The streams owned by
`ProcessConfiguration` are configured for synchronous writing, so
`IAsyncDisposable` is not required. Callers awaiting
`IProcessInvoker.ExecuteAsync` must still dispose the configuration
after the await completes — the invocation does **not** adopt
ownership.

**Ownership rule**: The caller that constructs the
`ProcessConfiguration` owns it. Reusing a single configuration across
multiple invocations is allowed; call `Dispose()` only after the
final invocation.

### 2. `IExternalProcess`

Defined in `src/CliInvoke.Core/Processes/IExternalProcess.cs`.

```csharp
public interface IExternalProcess : IDisposable
```

**What it owns**

The interface is a thin wrapper around a `System.Diagnostics.Process`
that is still attached to the OS. The `System.Diagnostics.Process`
holds three classes of native resources at once:

- Anonymous pipe handles for stdin/stdout/stderr redirection.
- A process handle (`HANDLE` on Windows, `pid_t` plus `/proc` entries
  on Unix) for the child process.
- A thread-pool wait handle and an output read thread used for stream
  pumping.

These are all allocated in the OS kernel, not in the managed heap,
and the GC cannot reclaim them.

**`Dispose()` behaviour**: Concrete implementations call
`Process.Dispose()`, which kills the wait handle, disposes the
redirected streams, and releases the kernel handle.

**Ownership rule**: Returned from
`IExternalProcessFactory.CreateExternalProcess(ProcessConfiguration)`. The caller owns
the returned `IExternalProcess` and must dispose it after
`WaitForExitAsync` / `CaptureBufferedResultAsync` /
`CapturePipedResultAsync` completes. The invoker does not retain a
reference after returning.

### 3. `PipedProcessResult`

Defined in `src/CliInvoke.Core/Primitives/Results/PipedProcessResult.cs`.

```csharp
public class PipedProcessResult : IDisposable, IAsyncDisposable
```

**What it owns**

- `StandardOutput` — a `Stream` that owns the read end of the
  redirected stdout pipe.
- `StandardError` — a `Stream` that owns the read end of the
  redirected stderr pipe.

Unlike `ProcessConfiguration`, the streams here are consumed by
`StreamReader.ReadToEndAsync` and similar, which is why the type
implements **both** `IDisposable` and `IAsyncDisposable`. The async
dispose path is preferred on .NET 8+.

**`Dispose()` behaviour** (line 70):

```csharp
public async ValueTask DisposeAsync()
{
    await StandardOutput.DisposeAsync();
    await StandardError.DisposeAsync();
}

public void Dispose()
{
    StandardOutput.Dispose();
    StandardError.Dispose();
}
```

**Ownership rule**: Returned from
`IExternalProcess.CapturePipedResultAsync`. The caller owns the
result. A `PipedProcessResult` that is not disposed will pin the read
ends of the stdout and stderr pipes open for the lifetime of the
`PipedProcessResult` object — even after the process has exited.

### 4. `UserCredential`

Defined in `src/CliInvoke.Core/Primitives/UserCredential.cs`.

```csharp
public class UserCredential : IEquatable<UserCredential>, IDisposable
```

**What it owns**

- A `SecureString` password. `SecureString` is a managed wrapper
  around an unmanaged, encrypted, pinned buffer. Disposal calls
  `SecureString.Dispose()`, which zeroes the buffer before releasing
  it.

**`Dispose()` behaviour** (line 92):

```csharp
public void Dispose()
{
    Password?.Dispose();
}
```

**Ownership rule**: Two cases.

1. The credential is assigned to a `ProcessConfiguration.Credential`.
   The `ProcessConfiguration` will dispose it (see
   [Type 1](#1-processconfiguration)). Do **not** also dispose the
   credential in user code, or you will double-dispose the
   `SecureString`.
2. The credential is used standalone (e.g., returned from a factory).
   The caller must dispose it.

### 5. `UserCredentialSpec`

Defined in `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs`.

```csharp
public sealed class UserCredentialSpec : IDisposable
```

**What it owns**

- A staged `SecureString` password held in a private field, used to
  assemble the `UserCredential` that `Build()` returns.

**`Dispose()` behaviour** (line 116):

```csharp
public void Dispose()
{
    _userPassword?.Dispose();
}
```

**Ownership rule**: The caller owns the builder. The `Build()` method
copies the password into a new `UserCredential`; the original
`SecureString` held by the builder is still owned by the builder and
must be disposed when the builder is no longer needed. Disposing the
builder does **not** dispose the produced `UserCredential` — the two
lifetimes are independent.

## Handle Exhaustion: Why Explicit Disposal Is Required

Managed memory in .NET is reclaimed by the garbage collector. The
resources in this library are not managed memory — they are kernel
handles, pipe handles, and `SecureString` buffers. The GC is allowed
to collect any object at any time, but it has no way to release a
kernel handle; only the type that allocated it can do that, and only
via `Dispose()`.

### What "handle exhaustion" means in practice

Every time a `Process` is started with redirected streams, the OS
allocates a pair of anonymous pipe handles. On Windows these are
backed by entries in the kernel's handle table, which is
per-process and finite. The default per-process handle limit on
Windows is `2^24` (about 16.7 million). On Linux the limits are
similar in spirit: each open file descriptor consumes a slot in the
process's file descriptor table, with a typical soft limit of 1024
per process and 4096 for the entire system for unprivileged users.

CliInvoke invocations in long-running services can easily reach
these limits. Each leaked `PipedProcessResult` pins two file
descriptors open. Each leaked `IExternalProcess` pins three. Each
leaked `ProcessConfiguration` pins at least one writer pipe. At a
few thousand leaked invocations the process hits the soft limit and
the next `Process.Start` throws `IOException("Too many open files")`
on Linux or `Win32Exception("Not enough quota")` on Windows.

`SecureString` is a different resource but the failure mode is
similar: the buffer is pinned in unmanaged memory and is not movable
by the GC. A long-lived process that leaks many `UserCredential`
objects retains every password buffer in pinned memory for the rest
of its lifetime.

### Why the GC alone is insufficient

The GC is allowed to delay collection arbitrarily. The `Dispose`
pattern exists precisely because the GC **cannot** call `Dispose`
itself — that would require non-deterministic ordering between
finalization and the consumer's last use of the resource. CliInvoke
follows the standard `IDisposable` contract: the type implements
`Dispose()`, and the caller is contractually obligated to call it.

The library does not implement finalizers (`~Type()`) on its
disposable types. This is intentional: a finalizer would add
non-deterministic latency to handle release and would not help the
caller, which still needs to release the resource in a timely
manner. The `Dispose` contract is the supported way to release
resources in CliInvoke.

## Disposal Patterns

### Pattern A — synchronous `using`

Use for `ProcessConfiguration`, `UserCredential`, and
`UserCredentialSpec`.

```csharp
using var config = new ProcessConfiguration("cmd", "/c echo hello");
using var credential = new UserCredential("domain", "user", password, false);

config.Credential = credential; // config will dispose credential
await invoker.ExecuteAsync(config);
// config.Dispose() runs here; credential is disposed by config
```

### Pattern B — `await using` (preferred on .NET 8+)

Use for `IExternalProcess` and `PipedProcessResult`, which surface
async-disposable streams.

```csharp
var factory = provider.GetRequiredService<IExternalProcessFactory>();
await using var process = factory.CreateExternalProcess(config);
await process.StartAsync(ct);
await using var result = await process.CapturePipedResultAsync(ct);

string output = await new StreamReader(result.StandardOutput).ReadToEndAsync();
// process and result are both disposed when leaving scope
```

### Pattern C — explicit `Dispose` in a `try/finally`

Use when the disposable crosses a scope that is not a `using`
declaration, or when working in a `try/catch` block where the
disposable must outlive the catch.

```csharp
var factory = provider.GetRequiredService<IExternalProcessFactory>();
var process = factory.CreateExternalProcess(config);
await process.StartAsync(ct);
try
{
    var result = await process.CaptureBufferedResultAsync(ct);
}
finally
{
    process.Dispose();
}
```

### Pattern D — spec + built credential

`UserCredentialSpec` and the `UserCredential` it produces have
**independent lifetimes**. Both must be disposed.

```csharp
UserCredential credential;
using (var spec = new UserCredentialSpec())
{
    credential = spec
        .SetUsername("user")
        .SetPassword(securePassword)
        .Build();
}
// spec disposed; now dispose the credential
using (credential)
{
    // use credential
}
```

## Disposal Rules

These rules are normative for every consumer of the library.

1. **Always dispose** the five resource-owning types listed above.
   No other CliInvoke type implements `IDisposable`.
2. **Never double-dispose**. When a `UserCredential` is assigned to
   a `ProcessConfiguration.Credential`, the configuration owns the
   credential. Disposing both will throw `ObjectDisposedException`
   from the second dispose.
3. **Never dispose a child resource owned by the library**. The
   `SecureString` inside a `UserCredential`, the `StreamWriter`
   inside a `ProcessConfiguration.StandardInput`, and the
   stdout/stderr streams inside a `PipedProcessResult` are released
   by their parent. Calling `Dispose` on them directly is a
   double-dispose.
4. **Prefer `await using`** for `IExternalProcess` and
   `PipedProcessResult` on .NET 8+. The streams are
   async-disposable.
5. **Reuse is allowed** for `ProcessConfiguration`. Call `Dispose`
   only after the final invocation.
6. **Disposal is the caller's responsibility**. The invoker does not
   retain references to the configuration, the process, or the
   result after returning. The caller that received the object owns
   it.

## Disposal Checklist

Before submitting code that uses CliInvoke, verify each of the
following:

- [ ] Every `ProcessConfiguration` is wrapped in `using` (or
  `try/finally Dispose`).
- [ ] Every `IExternalProcess` returned from `StartAsync` is wrapped
  in `await using` or `try/finally`.
- [ ] Every `PipedProcessResult` returned from
  `CapturePipedResultAsync` is wrapped in `await using` or
  `try/finally`.
- [ ] Every standalone `UserCredential` is wrapped in `using`.
- [ ] Every standalone `UserCredentialSpec` you create and own is wrapped in `using`, and the
  `UserCredential` it produces is wrapped in a separate `using`. A `UserCredentialSpec` configured
  through `ProcessConfigurationBuilder.ConfigureUserCredential` is owned and disposed by the
  builder, so do not dispose it yourself.
- [ ] No `StreamWriter`, `SecureString`, `StandardOutput`, or
  `StandardError` is disposed directly — only their parents.
- [ ] `IDisposable` is not implemented on any custom wrapper that
  owns a `ProcessConfiguration` or `IExternalProcess` without also
  disposing the owned resource in its own `Dispose`.

## Cross-References

- README — Resource Cleanup summary
- Issue tracker reference: #348
- Source files:
  - `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`
  - `src/CliInvoke.Core/Processes/IExternalProcess.cs`
  - `src/CliInvoke.Core/Primitives/Results/PipedProcessResult.cs`
  - `src/CliInvoke.Core/Primitives/UserCredential.cs`
  - `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs`
