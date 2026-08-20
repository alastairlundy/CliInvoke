---
title: Drop IFilePathResolver from PowershellProcessInvoker and PowerShellMiddleware; update DI ripple
classification: Independent
blocked_by: ["006-drop-resolver-powershellprocessconfiguration"]
parent: IMPLEMENTATION-externalprocess-config-seam.md
---

## Goal

Remove the now-dead `IFilePathResolver` parameter from the `PowershellProcessInvoker` and `PowerShellMiddleware` ctors, and update the DI registration that constructs them. The default `CliInvoke.FilePathResolver` allocation moves inside `PowerShellMiddleware` (already the case in the existing lazy-init; flip the nullable param to a direct allocation).

## What to build

1. **In `src/CliInvoke.Specializations/Invokers/PowershellProcessInvoker.cs`**, drop the `IFilePathResolver filePathResolver` parameter from the ctor (currently at line 57). Update the inner `new PowerShellMiddleware(filePathResolver)` (currently at line 61) to `new PowerShellMiddleware()`.
2. **In `src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs`**:
   - Drop the `IFilePathResolver? filePathResolver = null` parameter from the ctor (currently at line 53).
   - Replace `_filePathResolver ?? new CliInvoke.FilePathResolver();` (currently at line 55) with `new CliInvoke.FilePathResolver();` directly (preserves behaviour for callers that previously omitted the resolver).
   - Update `new PowershellProcessConfiguration(_filePathResolver, ...)` (currently at line 90) to `new PowershellProcessConfiguration(...)` (no resolver).
3. **In `src/CliInvoke.Extensions/DependencyInjection/DependencyInjectionExtensions.cs`** (NOT `FilePathResolverRegistration.cs` as the blueprint names — the actual call site is at line 233): drop the `sp.GetService<IFilePathResolver>()` argument from the `new PowerShellMiddleware(...)` call. The blueprint's reference to `FilePathResolverRegistration.cs` is incorrect — capture the actual file path.

## Size

- Files: 3

## Recommended Workflow

### Step 1 — Update PowerShellMiddleware ctor

Where: `src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs`

- Remove the `IFilePathResolver? filePathResolver = null` parameter.
- Replace `_filePathResolver ?? new CliInvoke.FilePathResolver();` with `new CliInvoke.FilePathResolver();`.
- Verify: Middleware ctor compiles; default `FilePathResolver` is allocated inside the middleware body.

### Step 2 — Update PowershellProcessConfiguration call site

Where: `src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs`

- Update line 90: `new PowershellProcessConfiguration(_filePathResolver, ...)` → `new PowershellProcessConfiguration(...)`.
- Verify: Call site matches the new ctor signature from TK006.

### Step 3 — Update PowershellProcessInvoker ctor

Where: `src/CliInvoke.Specializations/Invokers/PowershellProcessInvoker.cs`

- Remove the `IFilePathResolver filePathResolver` parameter.
- Update the inner `new PowerShellMiddleware(filePathResolver)` to `new PowerShellMiddleware()`.
- Verify: Invoker ctor compiles.

### Step 4 — Update DI registration

Where: `src/CliInvoke.Extensions/DependencyInjection/DependencyInjectionExtensions.cs`

- Drop the `sp.GetService<IFilePathResolver>()` argument from the `new PowerShellMiddleware(...)` call at line 233. Note: the blueprint names `FilePathResolverRegistration.cs` but that file does not register `PowerShellMiddleware` — the actual call site is in `DependencyInjectionExtensions.cs`.
- Verify: DI registration compiles.

### Step 5 — Build the full solution

Where: `src/CliInvoke.sln`

- Run `dotnet build src/CliInvoke.sln`.
- Verify: Build clean; all Specializations + Extensions call sites updated.

## Context pointers

- Files:
  - `src/CliInvoke.Specializations/Invokers/PowershellProcessInvoker.cs` (target)
  - `src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs` (target)
  - `src/CliInvoke.Extensions/DependencyInjection/DependencyInjectionExtensions.cs` (DI ripple; the actual file is this one, not `FilePathResolverRegistration.cs`)
- Domain terms: "Process Invocation Pipeline" (GLOSSARY.md — both invoker and middleware participate in the pipeline)
- Ledger records:
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T009` — drop resolver from `PowershellProcessInvoker` / `PowerShellMiddleware` ctors

## Acceptance criteria

- [ ] `PowerShellMiddleware` ctor no longer accepts `IFilePathResolver`.
- [ ] `PowershellProcessInvoker` ctor no longer accepts `IFilePathResolver`.
- [ ] The inner `new PowerShellMiddleware(...)` call in `PowershellProcessInvoker` passes no arguments.
- [ ] The DI registration in `DependencyInjectionExtensions.cs:233` constructs `PowerShellMiddleware` with no resolver argument.
- [ ] `dotnet build src/CliInvoke.sln` succeeds.

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — 006-drop-resolver-powershellprocessconfiguration (semantic coupling: `PowerShellMiddleware` constructs `PowershellProcessConfiguration` at line 90; the ctor signature change in TK006 must land first or the build breaks here).
