---
title: Relocate ShellArgumentEscaper to Specializations; rework ArgumentsSpec escaping
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-DECISIONS-CliInvoke-v3-internals-visibility.md
---

## Goal

Eliminate the CliInvoke.Core to CliInvoke.Specializations coupling by moving the PowerShell/Cmd escaping logic into Specializations as an internal helper, and making `ArgumentsSpec` (Core) self-contained with a new platform-aware escaper that is strictly less restrictive than Cmd escaping. After this, Specializations consumes no CliInvoke.Core internal, so the Core to Specializations grants (TK002) become removable.

## What to build

- Delete `src/CliInvoke.Core/Internal/ShellArgumentEscaper.cs`.
- Create `src/CliInvoke.Core/Internal/ArgumentEscaper.cs` — `internal static class ArgumentEscaper` with:
  - `public static string EscapeInner(string? argument)` — platform-aware inner escaping: Windows doubles `"`, leaves shell metacharacters unescaped, drops newlines; Unix escapes `'`.
  - `public static bool NeedsQuoting(string? argument)` — true when the argument requires double-quote wrapping on the current OS.
- Edit `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs`:
  - `EscapeCharacters`: `var e = ArgumentEscaper.EscapeInner(v); return ArgumentEscaper.NeedsQuoting(v) ? $"\"{e}\"" : e;`
  - `EscapeCharactersWithoutWrapping`: call `ArgumentEscaper.EscapeInner(v)` per value before the existing group-wrap.
  - Remove `using CliInvoke.Core.Internal;` if it was only for `ShellArgumentEscaper`.
- Create `src/CliInvoke.Specializations/Internal/ShellArgumentEscaper.cs` — `internal static class ShellArgumentEscaper` with `EscapeForPowerShell` and `EscapeForCmd` ported from Core.
- Edit `src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs` — use the new Specializations `ShellArgumentEscaper.EscapeForPowerShell`.
- Edit `src/CliInvoke.Specializations/Middleware/CmdMiddleware.cs` — use the new Specializations `ShellArgumentEscaper.EscapeForCmd`.
- Edit `tests/CliInvoke.Specializations.Tests/Internal/ShellArgumentEscaperTests.cs` — reference the new Specializations-internal escaper.

## Size

- Files - 7 (1 deleted, 2 created, 4 edited)
- Large Files to be created - omitted
- Large Edits required - omitted

## Recommended Workflow

### Step 1 - Add Core ArgumentEscaper

Where: src/CliInvoke.Core/Internal/ArgumentEscaper.cs (new)

- Implement `EscapeInner` and `NeedsQuoting` branching on the runtime OS (Windows C-runtime quoting; POSIX shell single-quote on Unix).

Verify: the class is `internal` and compiles in CliInvoke.Core.

### Step 2 - Rework ArgumentsSpec

Where: src/CliInvoke.Core/Configuration/ArgumentsSpec.cs

- Update `EscapeCharacters` and `EscapeCharactersWithoutWrapping` per the shapes above.
- Drop the `using CliInvoke.Core.Internal;` import if only used for `ShellArgumentEscaper`.

Verify: ArgumentsSpec builds; no reference to `ShellArgumentEscaper`.

### Step 3 - Delete Core ShellArgumentEscaper

Where: src/CliInvoke.Core/Internal/ShellArgumentEscaper.cs

- Delete the file.

Verify: grep for `ShellArgumentEscaper` in `src/CliInvoke.Core` returns no definitions.

### Step 4 - Add Specializations ShellArgumentEscaper

Where: src/CliInvoke.Specializations/Internal/ShellArgumentEscaper.cs (new)

- Port `EscapeForPowerShell` and `EscapeForCmd` from the deleted Core file; keep `internal`.

Verify: the class compiles as `internal` in CliInvoke.Specializations.

### Step 5 - Repoint middleware

Where: src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs, CmdMiddleware.cs

- Replace `ShellArgumentEscaper.EscapeForPowerShell(...)` / `EscapeForCmd(...)` with the Specializations-internal `ShellArgumentEscaper`.

Verify: the middleware files build; no reference to CliInvoke.Core internals.

### Step 6 - Update Specializations tests

Where: tests/CliInvoke.Specializations.Tests/Internal/ShellArgumentEscaperTests.cs

- Point tests at the new Specializations `ShellArgumentEscaper`.

Verify: the test file compiles and passes.

### Step 7 - Build and test (Windows plus Unix behavior)

Where: N/A

- Build the solution; run ArgumentsSpec builder tests and Specializations escaper tests.

Verify: existing Windows builder tests (drop newlines, double `"`, pass through `\`/`\t`) stay green; Unix branch is new behavior covered by tests.

## Context pointers

##### Files

- src/CliInvoke.Core/Internal/ShellArgumentEscaper.cs — to delete.
- src/CliInvoke.Core/Internal/ArgumentEscaper.cs — new self-contained Core escaper.
- src/CliInvoke.Core/Configuration/ArgumentsSpec.cs — reworked escaping.
- src/CliInvoke.Specializations/Internal/ShellArgumentEscaper.cs — new internal escaper.
- src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs, CmdMiddleware.cs — repointed.
- tests/CliInvoke.Specializations.Tests/Internal/ShellArgumentEscaperTests.cs — updated.

##### ADRs

- None constrain this ticket.

##### Domain terms

- Cross-package coupling point — this ticket removes the Core to Specializations coupling.
- Polyfill leakage — keeping the escaper in Core internal (not IVT-exposed to Specializations) avoids leaking Polyfill helpers.

##### Ledger records

- DECISIONS-CliInvoke-v3-internals-visibility.md#T003 — relocate shell escaping to Specializations; rework ArgumentsSpec to be self-contained and less restrictive than Cmd.
- DECISIONS-CliInvoke-v3-internals-visibility.md#T006 — platform-aware escaper (Windows C-runtime; Unix POSIX shell).
- DECISIONS-CliInvoke-v3-internals-visibility.md#T007 — API shape EscapeInner plus NeedsQuoting; ArgumentsSpec keeps escape-then-wrap.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D002 — minimize public surface; escaper stays internal.
- DECISIONS-CliInvoke-v3-internals-visibility.md#D009 — after this, Core to Specializations grants are unused (TK002).

## Acceptance criteria

- [ ] `CliInvoke.Core.Internal.ShellArgumentEscaper` is deleted; `ArgumentEscaper` (internal) exists with `EscapeInner` and `NeedsQuoting`.
- [ ] `ArgumentsSpec.EscapeCharacters` / `EscapeCharactersWithoutWrapping` use `ArgumentEscaper`; no `ShellArgumentEscaper` reference remains in Core.
- [ ] Specializations `ShellArgumentEscaper` (internal) provides `EscapeForPowerShell` / `EscapeForCmd`; middleware uses it.
- [ ] The new Core escaper is a strict subset of `EscapeForCmd`'s metacharacter set and is platform-aware.
- [ ] Existing Windows ArgumentsSpec builder tests stay green; Unix branch covered.
- [ ] Specializations no longer references any CliInvoke.Core internal (enables TK002).

## Dependencies

Blocked by - None - can start immediately
