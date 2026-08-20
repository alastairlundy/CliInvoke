---
title: Drop IFilePathResolver from PowershellProcessConfiguration ctor; provide executable name per platform
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-externalprocess-config-seam.md
---

## Goal

Stop `PowershellProcessConfiguration` from doing its own executable resolution at construction time. Pass only the platform-appropriate executable name (`pwsh.exe` on Windows, `pwsh` elsewhere) to the base ctor; let `ExternalProcess` resolve the path at Start time per D004.

## What to build

In `src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs`:

1. Drop the `IFilePathResolver filePathResolver` parameter from the ctor (currently at line 50).
2. Drop the ctor body that calls `filePathResolver.ResolveFilePath("pwsh.exe"/"pwsh")` (currently at lines 73-84) and the `TargetFilePath = filePath;` / `base.TargetFilePath = TargetFilePath;` assignments (currently at lines 89-90).
3. Keep the existing base-ctor invocation that passes `OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh"` for the `targetFilePath` argument — just the executable name (currently at line 59).
4. Delete the static `GetInstallLocationOnWindows()` helper (currently at lines 112-132; no longer reachable from the ctor). The blueprint defaults to deletion; behaviour for pwsh-not-in-PATH surfaces as `FileNotFoundException` at `Start()` time.

## Size

- Files: 1

## Recommended Workflow

### Step 1 — Drop the resolver parameter

Where: `src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs`

- Remove `IFilePathResolver filePathResolver` from the ctor parameter list.
- Remove the `using`-like references to the resolver field inside the ctor body.
- Verify: The ctor signature compiles (apart from the expected downstream errors).

### Step 2 — Keep executable name in base ctor

Where: `src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs`

- The base ctor invocation already passes `OperatingSystem.IsWindows() ? "pwsh.exe" : "pwsh"` for `targetFilePath`. No change needed; just confirm it remains after the ctor body is simplified.
- Verify: Base ctor receives only the executable name.

### Step 3 — Delete resolution body and helper

Where: `src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs`

- Remove the ctor body that calls `filePathResolver.ResolveFilePath("pwsh.exe"/"pwsh")` and the `TargetFilePath = filePath;` / `base.TargetFilePath = TargetFilePath;` assignments.
- Delete the static `GetInstallLocationOnWindows()` helper (no longer reachable; the blueprint defaults to deletion).
- Verify: The file builds apart from the expected downstream call-site error.

### Step 4 — Build the specializations package and observe the expected bridge

Where: `src/CliInvoke.Specializations/CliInvoke.Specializations.csproj`

- Run `dotnet build src/CliInvoke.Specializations/CliInvoke.Specializations.csproj`. Build will FAIL because `PowerShellMiddleware` still passes the resolver to this ctor — that's expected and is the bridge to TK007.
- Verify: Build errors are limited to the expected `PowerShellMiddleware.cs:90` call site.

## Context pointers

- Files: `src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs` (target of change)
- Domain terms: "Process Invocation Pipeline" (GLOSSARY.md — specialization config is consumed via the middleware pipeline)
- Ledger records:
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#T006` — drop `IFilePathResolver` from `PowershellProcessConfiguration` ctor
  - `DECISIONS-CliInvoke-externalprocess-config-seam.md#D004` — Specializations provide executable name per platform

## Acceptance criteria

- [ ] `PowershellProcessConfiguration` ctor no longer accepts `IFilePathResolver`.
- [ ] The base ctor receives only the executable name (`pwsh.exe` on Windows, `pwsh` elsewhere).
- [ ] No `ResolveFilePath` call inside `PowershellProcessConfiguration`.
- [ ] `GetInstallLocationOnWindows()` is deleted.
- [ ] Build errors are limited to the expected `PowerShellMiddleware.cs:90` call site (TK007 will resolve).

## Dependencies

All dependencies are tracked via the `Blocked by` field; the `Blocks` field is reserved for forward-looking dependency statements only and shall not be used in tickets produced by this skill.

**Blocked by** — None
