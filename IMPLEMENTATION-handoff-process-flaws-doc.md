# Implementation Blueprint — Process-flaw mitigations

## Scope Binding

- **Linked Spec:** `C:\Users\alast\AppData\Local\Temp\opencode\handoff-process-flaws-doc.md` (§Minor code changes identified but NOT implemented)
- **Decision Ledger:** `docs/decisions/DECISIONS-CliInvoke-process-flaw-mitigations.md`
- This blueprint is a context pointer valid ONLY for the linked spec and must not be applied to other specifications without explicit authorization.

## Ledger Reference

`DECISIONS-CliInvoke-process-flaw-mitigations.md#D001`, `#T001`, `#T002`, `#T003`, `#T004`, `#T005`, `#T006`, `#T007`

## Part 1 — Code changes (2 items)

### 1.1 Start-failure exception taxonomy [`DECISIONS-CliInvoke-process-flaw-mitigations.md#T001`]

**File:** `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` (`Start()`, lines 159–174)

Current state: catches `Win32Exception`; codes 2/3 → `FileNotFoundException` with path context; **all other codes → `UnauthorizedAccessException`**, which mislabels e.g. `ERROR_BAD_EXE_FORMAT` (193).

Change:
- Keep the 2/3 → `FileNotFoundException` mapping unchanged.
- Add explicit mappings for known codes:
  - `5` (`ERROR_ACCESS_DENIED`) → `UnauthorizedAccessException` (preserves today's correct behavior for the common case).
  - `193` (`ERROR_BAD_EXE_FORMAT`) → `BadImageFormatException` with the file path in the message.
- Fallback for unknown codes: a descriptive exception (keep `UnauthorizedAccessException` or widen the message) that includes the file path **and** the `NativeErrorCode`, so unknown cases are diagnosable.
- No new public exception type (T001 constraint).

### 1.2 Windows-gated command-line length check [`DECISIONS-CliInvoke-process-flaw-mitigations.md#T004`]

**File:** `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs` (`ApplyConfiguration`, where `Arguments`/`ArgumentList` are applied to `ProcessStartInfo`) — or a pre-start guard in `ProcessWrapper.Start()`; prefer the adapter so the check runs once where the command line is assembled.

Change:
- When `OperatingSystem.IsWindows()`, compute the total command-line length (executable path + quoted/joined arguments as `CreateProcess` will see it) and throw `ArgumentException` with the measured length once it exceeds ~32,767 characters.
- The limit is approximate; use 32,767 as the threshold and say so in the message.
- Non-Windows: no check (Unix limits are far higher).
- T001's native-code mapping remains the backstop for over-limit cases that slip through (e.g. error 206).

## Part 2 — Documentation-only outcomes (3 items)

### 2.1 Encoding default [`DECISIONS-CliInvoke-process-flaw-mitigations.md#T002`]

No code change. Document on the three `Encoding` properties of `ProcessConfiguration` (XML docs) and in `site/docs/`: the default is `Encoding.Default`, which is UTF-8 on .NET 10; callers can override per-stream via the init properties, which the control adapter applies explicitly.

### 2.2 Working-directory semantics [`DECISIONS-CliInvoke-process-flaw-mitigations.md#T003`]

No code change. Document that an empty `WorkingDirectoryPath` means the child inherits the caller's current directory (consistent on Windows and Unix), and correct the corresponding claim in `site/docs/everything-wrong-with-process-in-csharp.md` so it doesn't imply CliInvoke normalizes the value. The `Directory.Exists` validation on non-empty values (v4 ledger D006/T005) is unchanged.

### 2.3 Kill-tree best-effort [`DECISIONS-CliInvoke-process-flaw-mitigations.md#T005`]

No code change. Document that forceful exit uses `Kill(entireProcessTree: true)` and is best-effort: descendants spawned while the tree is being killed may survive, matching .NET's own semantics. No post-kill delay or descendant re-kill ships.

## Part 3 — Verification

- Build + test via the `cliinvoke-inner-loop` skill (`guard-configureawait.sh`, `dotnet build src/CliInvoke.sln`, `dotnet test` in `tests/CliInvoke.Tests/`).
- T001: unit tests for the 193 → `BadImageFormatException` mapping and the fallback message carrying `NativeErrorCode`.
- T004: Windows-only test asserting `ArgumentException` past the limit (skip on non-Windows, matching repo test conventions).
- Doc changes: verify the three doc claims against the code before committing (the handoff's own "human fact-check pass" note).
