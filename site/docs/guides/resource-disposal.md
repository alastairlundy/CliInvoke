---
title: Resource Disposal
layout: simple
---

# Resource Disposal Guide

This is the canonical reference for resource management in CliInvoke. It
documents the library's four resource-owning types, the unmanaged
resources they own, and the exact disposal patterns callers must
follow. No type in CliInvoke implements `IAsyncDisposable` — the
library is `IDisposable`-only.

The goal of this page is to prevent two failure modes:

1. **Resource leaks** — files, pipes, kernel handles, and
   `SecureString` buffers left in memory after an invocation has
   finished.
2. **Handle exhaustion** — the platform error state reached when a
   process accumulates open handles faster than it releases them.

If you only read one section, read
[The Disposable Types](#the-disposable-types) and the
[Disposal Patterns](#disposal-patterns) summary.

## Terminology

A **Resource-Owning Type** is any CliInvoke type that holds, directly
or transitively, an unmanaged resource or a sensitive managed resource
that must be deterministically released. The library exposes exactly
four of them. Most other public types are value-bearing immutables,
enums, or interface contracts that require no disposal; where any
other type implements `IDisposable` as a secondary contract, its own
documentation describes that contract.

> [!IMPORTANT]
> `ProcessConfiguration` is **not** a Resource-Owning Type. As of 3.0 it is
> a plain immutable value object that does not implement `IDisposable`, and
> CliInvoke never disposes the objects you place inside it. The
> `StandardInput` (`StreamWriter`) and `UserCredential` you supply are
> owned and disposed by **you**, the caller (see
> [Caller-owned resources](#processconfiguration--caller-owned-resources)).

## The Disposable Types

| # | Type | Disposal contract | Resources owned |
|---|------|-------------------|-----------------|
| 1 | [`IExternalProcess`](#1-iexternalprocess) | `IDisposable` | The underlying `System.Diagnostics.Process` (pipes, handles, threads) |
| 2 | [`UserCredential`](#2-usercredential) | `IDisposable` | `SecureString` password buffer |
| 3 | [`UserCredentialSpec`](#3-usercredentialspec) | `IDisposable` | `SecureString` password buffer staged for `Build()` |
| 4 | [`ProcessConfigurationBuilder`](#4-processconfigurationbuilder) | `IDisposable` | The `UserCredentialSpec` it creates |

These four are the types that own unmanaged handles or sensitive
memory. For `ProcessConfigurationBuilder`, you remain responsible for
disposing any `Credential` and `StandardInput` you supply to it — the
builder does not dispose caller-provided credential/stdin resources
(its `Dispose()` only releases the `UserCredentialSpec` it created).
A type not listed in the table above may still implement
`IDisposable`, but it does not own pipes, kernel handles, or password
buffers; check its own documentation before assuming no disposal is
needed.

### `ProcessConfiguration` — caller-owned resources

Defined in `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`.

```csharp
public class ProcessConfiguration : IEquatable<ProcessConfiguration>
```

`ProcessConfiguration` is an immutable value object. It does **not**
implement `IDisposable`, and the invocation pipeline never adopts
ownership of the disposable objects it references. Two of its
properties are therefore your responsibility:

- **`StandardInput`** — a `StreamWriter` you supplied (or the default
  `StreamWriter.Null`). If you provide a real stream, you must dispose
  it once every invocation that referenced it has completed.
- **`Credential`** — a `UserCredential` you supplied (or the default
  `UserCredential.Null`). If you provide a real `UserCredential`, you
  must dispose it.

CliInvoke will not close your `StandardInput` stream or wipe your
`SecureString` for you. The recommended pattern is to hold these
resources in their own `using` declarations so their lifetime is
independent of the configuration:

```csharp
using var stdin = new StreamWriter(new MemoryStream());
using var credential = new UserCredential("domain", "user", password, false);

ProcessConfiguration config = new ProcessConfiguration("cmd", "/c echo hello")
{
    StandardInput = stdin,
    Credential = credential
};

BufferedProcessResult result = await CliRun.RunBufferedAsync(config);
// config falls out of scope without disposal; stdin and credential are
// disposed by their own `using` declarations.
```

**Ownership rule**: The caller that constructs (or supplies the
disposable parts of) a `ProcessConfiguration` owns those disposable
parts. Reusing a single configuration across multiple invocations is
allowed; dispose `StandardInput` and `UserCredential` only after the
final invocation.

### 1. `IExternalProcess`

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
`WaitForExitOrTimeoutAsync` / `CaptureBufferedResultAsync` completes.
An `IExternalProcess` created inside the invoker's pipeline follows a
different rule: the pipeline creates it, runs it, and disposes it in a
`finally` before returning the result — it is never handed to the
caller.

### 2. `UserCredential`

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
   The `ProcessConfiguration` does **not** dispose it — the credential
   remains a standalone disposable that **you** must dispose. Do not
   rely on the configuration to clean it up.
2. The credential is used standalone (e.g., returned from a factory).
   The caller must dispose it.

### 3. `UserCredentialSpec`

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

### 4. `ProcessConfigurationBuilder`

Defined in `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`.

```csharp
public sealed class ProcessConfigurationBuilder : IProcessConfigurationBuilder, IDisposable
```

**What it owns**

- The `UserCredentialSpec` it creates internally. The spec (and the
  staged `SecureString` it holds) exists for the builder's lifetime and
  is not exposed for caller disposal.

**`Dispose()` behaviour** (line 532):

```csharp
public void Dispose()
{
    _userCredentialSpec.Dispose();
}
```

**Ownership rule**: The caller owns the builder; disposing it disposes
the builder-owned `UserCredentialSpec`. Disposing the builder does
**not** dispose the produced `ProcessConfiguration`, any `UserCredential`
inside it, or any `StandardInput` stream the caller supplied — those
follow the [caller-owned rules](#processconfiguration--caller-owned-resources).

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
these limits. Each leaked `IExternalProcess` pins three. Each
leaked `UserCredential` retains a pinned `SecureString` buffer. At a
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

Use for `UserCredential` and `UserCredentialSpec`.

```csharp
using var credential = new UserCredential("domain", "user", password, false);
using var spec = new UserCredentialSpec();

ProcessConfiguration config = new ProcessConfiguration("cmd", "/c echo hello")
{
    Credential = credential
};
// credential is disposed by its own `using` declaration; the
// ProcessConfiguration never disposes it.
```

### Pattern B — scope-bound `using` declaration

Use for a factory-created `IExternalProcess`. It implements
`IDisposable` only — there is no `IAsyncDisposable` in CliInvoke — so a
plain `using` declaration is the contract. Output is captured into the
result object; `IExternalProcess` does not expose live `StandardOutput`
/ `StandardError` streams to read from.

```csharp
var factory = provider.GetRequiredService<IExternalProcessFactory>();
using IExternalProcess process = factory.CreateExternalProcess(config);
await process.StartAsync(ct);

// Buffered path: capture output into the result (result.StandardOutput /
// result.StandardError hold the captured text):
BufferedProcessResult result = await process.CaptureBufferedResultAsync(ct);

// Raw path: exit code only, via ProcessResult instead:
// ProcessResult result = await process.WaitForExitOrTimeoutAsync(ct);

// process is disposed when leaving scope
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

1. **Always dispose** the four resource-owning types listed above
   (`IExternalProcess`, `UserCredential`, `UserCredentialSpec`,
   `ProcessConfigurationBuilder`).
   `ProcessConfiguration` is not among them.
2. **Dispose caller-supplied `StandardInput` and `UserCredential`
   yourself.** `ProcessConfiguration` does not dispose them, and
   neither does the invocation pipeline. Hold them in your own `using`
   declarations.
3. **Never dispose a child resource owned by the library**. The
   `SecureString` inside a `UserCredential` and the staged
   `SecureString` inside a `UserCredentialSpec` are released by their
   parent. Calling `Dispose`
   on them directly is a double-dispose.
4. **Dispose `IExternalProcess` with a plain `using` or `try/finally`.**
   It implements `IDisposable` only — no type in CliInvoke implements
   `IAsyncDisposable` — and disposing it releases the redirected
   streams and process handles it owns.
5. **Disposal is the caller's responsibility for what the caller
   receives.** The invoker does not retain references to the
   configuration or the result after returning; the caller that
   received them owns them. The `IExternalProcess` the pipeline
   created is disposed by the pipeline itself (in a `finally`) before
   the result is returned — only processes you create yourself need
   your `Dispose()`.
6. **Reuse is allowed** for `ProcessConfiguration`. Dispose any
   `StandardInput` stream or `UserCredential` you supplied only after
   the final invocation that referenced them.

## Disposal Checklist

Before submitting code that uses CliInvoke, verify each of the
following:

- [ ] Every `IExternalProcess` you create via `IExternalProcessFactory`
  is wrapped in `using` or `try/finally`. `StartAsync` returns `Task`
  (not a process), and an `IExternalProcess` created by the invoker's
  pipeline is created *and* disposed by that pipeline
  (`ProcessInvocationPipeline` releases it in a `finally`) — the
  invoker never returns it, so there is nothing for you to dispose.
- [ ] Every standalone `UserCredential` is wrapped in `using`.
- [ ] Every standalone `UserCredentialSpec` you create and own is wrapped in `using`, and the
  `UserCredential` it produces is wrapped in a separate `using`. A `UserCredentialSpec` configured
  through `ProcessConfigurationBuilder.ConfigureUserCredential` is owned and disposed by the
  builder, so do not dispose it yourself.
- [ ] Any `StreamWriter` you pass as `ProcessConfiguration.StandardInput`
  is disposed by your own `using` (the configuration will not dispose it).
- [ ] No `SecureString` or caller-supplied `StandardInput` stream is
  disposed directly — only by the object that owns it.
- [ ] `IDisposable` is not implemented on any custom wrapper that
  owns an `IExternalProcess` without also disposing the owned
  resource in its own `Dispose`.

## Cross-References

- README — Resource Disposal summary
- Issue tracker reference: #348
- Source files:
  - `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`
  - `src/CliInvoke.Core/Processes/IExternalProcess.cs`
  - `src/CliInvoke.Core/Primitives/UserCredential.cs`
  - `src/CliInvoke.Core/Configuration/UserCredentialSpec.cs`
