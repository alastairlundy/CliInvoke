---
title: "Migration Guide: No-Mutation Contract on ExternalProcess (v3 Pre-Release)"
---

# Migration Guide: No-Mutation Contract on ExternalProcess

> **v3 Pre-Release** — This guide covers breaking changes introduced in the v3 pre-release that enforce a no-mutation contract on `ProcessConfiguration` and `ExternalProcess`. These changes are part of the ExternalProcess configuration seam redesign.

## Overview

In CliInvoke v3, `ProcessConfiguration` is **never mutated** after construction. Previously, `ExternalProcess.Start()` and `StartAsync()` would rewrite `Configuration.TargetFilePath` with the runtime-resolved file path. This mutation is now removed.

**What changed:**

- `ProcessConfiguration.TargetFilePath` is now `init`-only (was `set`).
- `ExternalProcess.Configuration` and `IExternalProcess.Configuration` are now `init`-only (was `set`).
- `PowershellProcessConfiguration`, `PowershellProcessInvoker`, and `PowerShellMiddleware` constructors no longer accept an `IFilePathResolver` parameter — file resolution is handled internally by `ExternalProcess`.
- The resolved file path is available on the result objects, not on the configuration.

## 1. No-Mutation Contract

`ProcessConfiguration` is not mutated after construction. When `ExternalProcess.Start()` or `StartAsync()` is called, the file path is resolved internally and passed to the process wrapper — the caller's `Configuration` instance remains unchanged.

To obtain the resolved file path, use the `ExecutedFilePath` property on the result (see [Section 6](#6-obtaining-the-resolved-file-path)).

## 2. Init-Only `ProcessConfiguration.TargetFilePath`

`ProcessConfiguration.TargetFilePath` changed from a read-write property to an init-only property. Post-construction assignment no longer compiles.

**Before (v2):**
```csharp
ProcessConfiguration config = new ProcessConfiguration("dotnet");
config.TargetFilePath = @"C:\resolved\dotnet.exe"; // compiled in v2
```

**After (v3):**
```csharp
ProcessConfiguration config = new ProcessConfiguration("dotnet");
// Post-construction assignment is a compile error in v3.

// Use object-initializer syntax instead:
ProcessConfiguration config2 = new ProcessConfiguration("dotnet")
{
    TargetFilePath = @"C:\resolved\dotnet.exe"
};
```

## 3. Init-Only `ExternalProcess.Configuration` and `IExternalProcess.Configuration`

`ExternalProcess.Configuration` and the `IExternalProcess.Configuration` interface property changed from read-write to init-only.

**Before (v2):**
```csharp
IFilePathResolver resolver = new FilePathResolver();
ExternalProcess process = new ExternalProcess(resolver, config);
process.Configuration = new ProcessConfiguration("dotnet"); // compiled in v2
```

**After (v3):**
```csharp
IFilePathResolver resolver = new FilePathResolver();
ExternalProcess process = new ExternalProcess(resolver, config);
// Post-construction assignment is a compile error in v3.

// Pass the configuration via the constructor instead:
ExternalProcess process2 = new ExternalProcess(resolver, new ProcessConfiguration("dotnet"));
```

## 4. Dropped Constructor Parameter from `PowershellProcessConfiguration`

The `IFilePathResolver` parameter was removed from the `PowershellProcessConfiguration` constructor. The constructor now takes only configuration-related parameters. The PowerShell executable path (`pwsh.exe` on Windows, `pwsh` on Unix) is resolved internally by `ExternalProcess` at start time.

**Before (v2):**
```csharp
IFilePathResolver resolver = new FilePathResolver();
PowershellProcessConfiguration config = new PowershellProcessConfiguration(
    resolver,
    "-Command \"Write-Host Hello\"",
    redirectStandardInput: false);
```

**After (v3):**
```csharp
PowershellProcessConfiguration config = new PowershellProcessConfiguration(
    "-Command \"Write-Host Hello\"",
    redirectStandardInput: false);
```

## 5. Dropped Constructor Parameters from `PowershellProcessInvoker` and `PowerShellMiddleware`

The `IFilePathResolver` parameter was removed from both `PowershellProcessInvoker` and `PowerShellMiddleware` constructors. File path resolution is now handled internally by `ExternalProcess`.

**Before (v2):**
```csharp
IFilePathResolver resolver = new FilePathResolver();
PowershellProcessInvoker invoker = new PowershellProcessInvoker(
    resolver,
    externalProcessFactory);
```

**After (v3):**
```csharp
PowershellProcessInvoker invoker = new PowershellProcessInvoker(
    externalProcessFactory);
```

**Before (v2) — PowerShellMiddleware:**
```csharp
IFilePathResolver resolver = new FilePathResolver();
PowerShellMiddleware middleware = new PowerShellMiddleware(resolver);
```

**After (v3):**
```csharp
PowerShellMiddleware middleware = new PowerShellMiddleware();
// Or with options:
PowerShellMiddleware middleware = new PowerShellMiddleware(
    new PowerShellMiddlewareOptions { WindowCreation = true });
```

## 6. Obtaining the Resolved File Path

The resolved file path is no longer written back to `Configuration.TargetFilePath`. Instead, it is available on the result objects after starting the process:

- **`ProcessResult.ExecutedFilePath`** — the resolved file path that was actually executed.
- **`BufferedProcessResult.ExecutedFilePath`** — same, for buffered process results.

```csharp
IFilePathResolver resolver = new FilePathResolver();
ExternalProcess process = new ExternalProcess(resolver, new ProcessConfiguration("dotnet"));

// Start the process, then capture the result:
process.Start();
BufferedProcessResult result = await process.CaptureBufferedResultAsync(CancellationToken.None);

// The resolved file path:
string resolvedPath = result.ExecutedFilePath;
// e.g. "C:\Program Files\dotnet\dotnet.exe"

// Configuration.TargetFilePath is still "dotnet" (unchanged):
string originalPath = process.Configuration.TargetFilePath;
// "dotnet"
```

## 7. `ProcessWrapper` Constructor Change (Internal)

> This section is for completeness. `ProcessWrapper` is an internal type and most consumers will not interact with it directly.

The `ProcessWrapper` constructor changed from:

```csharp
internal ProcessWrapper(ProcessConfiguration configuration, ProcessResourcePolicy? resourcePolicy)
```

to:

```csharp
internal ProcessWrapper(ProcessConfiguration configuration, FileInfo resolvedFilePath)
```

The `ProcessResourcePolicy` is now sourced from `configuration.ResourcePolicy` internally. The resolved `FileInfo` is passed in by `ExternalProcess` after file path resolution.

## Notes

- The shipping window (v3 pre-release vs v3 stable) is a release-time decision.
