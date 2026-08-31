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
[The Three Disposable Types](#the-three-disposable-types) and the
[Disposal Patterns](#disposal-patterns) summary.

## Terminology

A **Resource-Owning Type** is any CliInvoke type that holds, directly
or transitively, an unmanaged resource or a sensitive managed resource
that must be deterministically released. The library exposes exactly
three of them. Every other public type in the library is a
value-bearing immutable, an enum, or an interface contract and
requires no disposal.

> [!IMPORTANT]
> `ProcessConfiguration` is **not** a Resource-Owning Type. As of 3.0 it is
> a plain immutable value object that does not implement `IDisposable`, and
> CliInvoke never disposes the objects you place inside it. The
> `StandardInput` (`StreamWriter`) and `UserCredential` you supply are
> owned and disposed by **you**, the caller (see
> [Caller-owned resources](#processconfiguration--caller-owned-resources)).

## The Three Disposable Types

| # | Type | Disposal contract | Resources owned |
|---|------|-------------------|-----------------|
| 1 | [`IExternalProcess`](#1-iexternalprocess) | `IDisposable` | The underlying `System.Diagnostics.Process` (pipes, handles, threads) |
| 2 | [`UserCredential`](#2-usercredential) | `IDisposable` | `SecureString` password buffer |
| 3 | [`UserCredentialSpec`](#3-usercredentialspec) | `IDisposable` | `SecureString` password buffer staged for `Build()` |

No other public CliInvoke type implements `IDisposable`. If a type is
not in the table above, it does not need to be disposed.

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

ProcessConfiguration config = new ProcessConfigurationBuilder("cmd")
    .SetArguments("/c echo hello")
    .SetStandardInput(stdin)
    .SetCredential(credential)
    .Build();

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
`WaitForExitAsync` / `CaptureBufferedResultAsync` completes. The
invoker does not retain a
reference after returning.

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

ProcessConfiguration config = new ProcessConfigurationBuilder("cmd")
    .SetArguments("/c echo hello")
    .SetCredential(credential)
    .Build();
// credential is disposed by its own `using` declaration; the
// ProcessConfiguration never disposes it.
```

### Pattern B — `await using` (preferred on .NET 8+)

Use for `IExternalProcess`, which surfaces async-disposable streams and
exposes the live `StandardOutput` / `StandardError` streams for
streaming consumption.

```csharp
var factory = provider.GetRequiredService<IExternalProcessFactory>();
await using var process = factory.CreateExternalProcess(config);
await process.StartAsync(ct);

// Stream stdout directly from the live process:
string output = await new StreamReader(process.StandardOutput).ReadToEndAsync();
await process.WaitForExitOrTimeoutAsync(ct);
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

1. **Always dispose** the three resource-owning types listed above
   (`IExternalProcess`, `UserCredential`, `UserCredentialSpec`).
   `ProcessConfiguration` is not among them.
2. **Dispose caller-supplied `StandardInput` and `UserCredential`
   yourself.** `ProcessConfiguration` does not dispose them, and
   neither does the invocation pipeline. Hold them in your own `using`
   declarations.
3. **Never dispose a child resource owned by the library**. The
   `SecureString` inside a `UserCredential` and the stream inside a
   `UserCredentialSpec` are released by their parent. Calling `Dispose`
   on them directly is a double-dispose.
4. **Prefer `await using`** for `IExternalProcess` on .NET 8+. The
   streams are async-disposable.
5. **Disposal is the caller's responsibility**. The invoker does not
   retain references to the configuration, the process, or the
   result after returning. The caller that received the object owns
   it.
6. **Reuse is allowed** for `ProcessConfiguration`. Dispose any
   `StandardInput` stream or `UserCredential` you supplied only after
   the final invocation that referenced them.

## Disposal Checklist

Before submitting code that uses CliInvoke, verify each of the
following:

- [ ] Every `IExternalProcess` returned from `StartAsync` is wrapped
  in `await using` or `try/finally`.
- [ ] Every standalone `UserCredential` is wrapped in `using`.
- [ ] Every standalone `UserCredentialSpec` you create and own is wrapped in `using`, and the
  `UserCredential` it produces is wrapped in a separate `using`. A `UserCredentialSpec` configured
  through `ProcessConfigurationBuilder.ConfigureUserCredential` is owned and disposed by the
  builder, so do not dispose it yourself.
- [ ] Any `StreamWriter` you pass as `ProcessConfiguration.StandardInput`
  is disposed by your own `using` (the configuration will not dispose it).
- [ ] No `SecureString`, `StandardOutput`, or `StandardError` is disposed
  directly — only their parents.
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
