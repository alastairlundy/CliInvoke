# Implementation Blueprint — CliInvoke bug audit fixes

- Status: ready for implementation
- Date: 2026-09-07
- Spec (audit findings — input data, not binding): `C:\Users\alast\AppData\Local\Temp\opencode\cliinvoke-bug-audit-handoff-2026-09-07.md`
- Decision Ledger: `docs/decisions/DECISIONS-CliInvoke-bug-audit-fixes.md` (D001, T001–T029)
- All inline citations use the form `DECISIONS-CliInvoke-bug-audit-fixes.md#<Txxx>`.

## Scope Binding

- Session goal: decide per finding whether to fix it and what behavior the fix implements (`DECISIONS-CliInvoke-bug-audit-fixes.md#D001`).
- Foundation: C#/.NET 10, the three shipping projects plus existing test projects, no new projects or packages (`DECISIONS-CliInvoke-bug-audit-fixes.md#T001`).
- Out of scope: v4 capability items; the v4 init conversion of `ProcessConfiguration` (v4 ledger T005) — T014 and T023 defer to it; exception-retry classification (T007 leaves it open for a future feature decision).
- Traversal: High → Medium → Low, with per-finding inclusion decided case by case (`DECISIONS-CliInvoke-bug-audit-fixes.md#D001`).

## Fixes by file

### src/CliInvoke/ShellDetector.cs

- T002 — Delete the throwing `cancellationToken.Register` at line 67; add `ThrowIfCancellationRequested` checkpoints before/after the awaits in `ResolveDefaultShellOnUnixAsync` (`DECISIONS-CliInvoke-bug-audit-fixes.md#T002`).
- T017 — Narrow the bare catch at line 137 to the expected resolve/start failures with a justification comment; `OperationCanceledException` must propagate instead of falling through to the cmd fallback (`DECISIONS-CliInvoke-bug-audit-fixes.md#T017`).

### src/CliInvoke/Processes/Internal/ControlAdapters/UnixProcessControlAdapter.cs

- T003 — `RequireRunningAsAdmin` throws `PlatformNotSupportedException` (reached only when `RequiresAdministrator` is set, `BaseProcessControlAdapter.cs:72-73`); `SetUserCredential` throws `PlatformNotSupportedException` for a non-null credential (guarded — `ApplyConfiguration` calls it unconditionally at `BaseProcessControlAdapter.cs:76`) (`DECISIONS-CliInvoke-bug-audit-fixes.md#T003`).
- T018 — Remove the dead `|| OperatingSystem.IsWindows()` term at line 71 (`DECISIONS-CliInvoke-bug-audit-fixes.md#T018`).

### src/CliInvoke/Processes/Internal/ControlAdapters/WindowsProcessControlAdapter.cs

- T018 — Verify by direct read, then remove the dead `IsLinux()` term in `SetResourcePolicy` (audit-reported site) (`DECISIONS-CliInvoke-bug-audit-fixes.md#T018`).

### src/CliInvoke/FilePathResolver.cs

- T004 — Match predicate at lines 229-230 compares `f.Name` against `fileName` on both platforms (`Ordinal` on Unix, `OrdinalIgnoreCase` on Windows); add XML remarks documenting the inferred-directory semantics for relative subpaths (`DECISIONS-CliInvoke-bug-audit-fixes.md#T004`).
- T017 — Narrow the bare catch at lines 187-189 with a justification comment (`DECISIONS-CliInvoke-bug-audit-fixes.md#T017`).

### src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- T015 — Delete both Register callbacks (lines 642-647 and 704-709); compute the cancellation reason via `CancellationHelper.GetCancellationReason(expectedExitTime, cancellationToken)` inside each catch, where the token is known-canceled (`DECISIONS-CliInvoke-bug-audit-fixes.md#T015`).
- T020 — `ForcefulExit` (lines 428-438) checks `HasExited` and no-ops on an exited process; the unguarded finally call at line 675 becomes safe on normal exits; the early-return path's deferral to the active cancellation holder stays (`DECISIONS-CliInvoke-bug-audit-fixes.md#T020`).
- T017 — Narrow the bare catches at lines 119-123 and 143-147 (expected: `InvalidOperationException` from suspend/resume on an exited process) and the bare catch inside `ForcefulExit` (lines 434-436); with T020's self-guard the fallback may reduce to a single guarded kill — implementer discretion within T017's rule (`DECISIONS-CliInvoke-bug-audit-fixes.md#T017`).
- T018 — Remove the unreachable `!HasStarted` re-check at line 177 (`DECISIONS-CliInvoke-bug-audit-fixes.md#T018`).
- T019 — Raise the Started event via `Started?.Invoke(...)` at line 213 (`DECISIONS-CliInvoke-bug-audit-fixes.md#T019`).
- T021 — `ReadStreamCappedAsync`: `maxBytes is null or < 0` means no cap; `0` is a valid zero-byte cap (empty text, truncated flag set); XML docs spell out all three spellings (`DECISIONS-CliInvoke-bug-audit-fixes.md#T021`).
- T022 — Decode the capped bytes incrementally with `encoding.GetDecoder()` so a split trailing multibyte sequence is held back and dropped cleanly instead of becoming U+FFFD (decode step at line 423) (`DECISIONS-CliInvoke-bug-audit-fixes.md#T022`).

### src/CliInvoke/Processes/ExternalProcess.cs

- T016 — A private lock serializes the start gate and the `_processWrapper` swap; readers capture the wrapper reference under the lock and operate on the snapshot; long awaits stay outside the lock (`DECISIONS-CliInvoke-bug-audit-fixes.md#T016`).

### src/CliInvoke.Core/Primitives (equality contracts)

- T012 — `ShellInformation.Equals` includes `Version`, matching `GetHashCode` (`DECISIONS-CliInvoke-bug-audit-fixes.md#T012`).
- T010 — `ProcessConfiguration` `==`/`!=` (lines 341-361) become null-safe: `null == null` true, `null == x` false (`DECISIONS-CliInvoke-bug-audit-fixes.md#T010`).
- T011 — `UserCredential.GetHashCode` (lines 175-189) hashes `Domain`, `UserName`, and `LoadUserProfile` only; `Password` never enters the hash; XML docs note possible collisions for distinct passwords with the same user/domain (`DECISIONS-CliInvoke-bug-audit-fixes.md#T011`).
- T008 — `ProcessResult` `==`/`!=` (lines 195-212) become null-safe (`DECISIONS-CliInvoke-bug-audit-fixes.md#T008`).
- T008 + T009 — `BufferedProcessResult`: null-safe operators (lines 168-178) and `Equals`/`GetHashCode` include `ProcessId` (lines 99-151), matching the base contract (`DECISIONS-CliInvoke-bug-audit-fixes.md#T008`, `DECISIONS-CliInvoke-bug-audit-fixes.md#T009`).
- T010 — `ProcessExceptionInfo<TProcessResult>` `==`/`!=` (lines 178-199) become null-safe (`DECISIONS-CliInvoke-bug-audit-fixes.md#T010`).

### src/CliInvoke.Specializations/Configurations/PowershellProcessConfiguration.cs

- T014 — Replace the never-assigned auto-property (line 88) with `public new string TargetFilePath => base.TargetFilePath;` — the base ctor already resolves pwsh per OS; must not preclude the v4 init conversion (`DECISIONS-CliInvoke-bug-audit-fixes.md#T014`).

### src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs

- T006 — `ComputeDelay` (lines 94-108) computes exponential/linear delays with overflow-checked arithmetic clamping to `MaxTaskDelay` on would-be overflow; any (BaseDelay, MaxAttempts) pair yields a valid non-negative delay (`DECISIONS-CliInvoke-bug-audit-fixes.md#T006`).
- T007 — XML docs state that retries apply to classifier-approved results and that exceptions from the pipeline propagate without retry; no classification code ships (`DECISIONS-CliInvoke-bug-audit-fixes.md#T007`).

### src/CliInvoke/Extensions/Configuration/ConfigurationExtensions.cs

- T023 — XML remark on `FromProcessStartInfo` stating that per-stream redirect flags collapse via OR into the configuration's single `OutputRedirection` flag (line 69). Binding constraint: no public surface change (`DECISIONS-CliInvoke-bug-audit-fixes.md#T023`).

### src/CliInvoke/Internal/IO/PathEnvironmentVariable.cs

- T024 — Restructure `EnumerateDirectories` (lines 56-82): expand `~` first, then locate `$HOME` on the expanded string in one sequential pass; one `GetFolderPath` fetch per entry (`DECISIONS-CliInvoke-bug-audit-fixes.md#T024`).

### src/CliInvoke/Extensions/Caching/CachingFilePathResolver.cs

- T025 — Normalize cache keys per-OS casing rules (case-insensitive on Windows, as-is elsewhere); document the benign concurrent-compute race in XML remarks; the per-hit `File.Exists` re-verification stays (`DECISIONS-CliInvoke-bug-audit-fixes.md#T025`).

### src/CliInvoke/Extensions/DependencyInjection/AddCustomResultValidatorsExtensions.cs

- T026 — Replace `TryAdd{Lifetime}` with `Add{Lifetime}` in both methods (lines 54-69 and 109-123); XML docs state the single-threaded registration convention (`DECISIONS-CliInvoke-bug-audit-fixes.md#T026`).

## Cross-cutting work

### ConfigureAwait sweep + CI guard (T013)

- Add `ConfigureAwait(false)` at every await site in `src` library code (today: 6 usages — 5 in `ProcessWrapper.cs`, 1 in `RetryMiddleware.cs`) (`DECISIONS-CliInvoke-bug-audit-fixes.md#T013`).
- Add a package-free CI guard that fails on bare awaits in `src`, whitelisting `await foreach`/`await using` (`DECISIONS-CliInvoke-bug-audit-fixes.md#T013`).

### Test plan (T027)

- Regression tests per behavior-changing fix, extending existing suites: `Primitives/ProcessResultEqualityTests.cs`, `BufferedProcessResultEqualityTests.cs`, `Primitives/UserCredentialTests.cs`, `Primitives/ProcessConfigurationTests.cs` (equality, T008-T012); `Resolvers/FilePathResolverTests.cs` (T004); `Invokers/Cancellation/GracefulCancellationTests.cs` and `Helpers/Processes/ProcessCancellationTests.cs` (T002, T015, T020); `Processes/TruncationTests.cs` plus `Fuzzing/TruncationCapFuzzTests.cs` (T021, T022 — include multibyte boundary cases); `Processes/ExternalProcessNoMutationTests.cs` (T016, plus concurrency tests); `DependencyInjection/DependencyInjectionExtensionTests.cs` (T026); retry overflow boundary tests (T006); Unix adapter throw tests (T003).
- FsCheck property tests for the equality contracts (T008-T012), extending v4-ledger T007's property-test scope (`DECISIONS-CliInvoke-bug-audit-fixes.md#T027`).
- Unix-path fixes (T003, T004) verify on Ubuntu CI — tests must be OS-conditional or CI-verified; Windows-local runs cannot cover them (`DECISIONS-CliInvoke-bug-audit-fixes.md#T027`).
- Docs-only fixes (T007, T023) have no runtime tests (`DECISIONS-CliInvoke-bug-audit-fixes.md#T027`).

### Release-notes / docs surface

- Breaking changes to document: Unix admin/credential now throws (T003); `null == null` operator semantics (T008, T010); `ProcessId` in BufferedProcessResult equality (T009); UserCredential hash change (T011); truncation `0`-cap semantics (T021); narrowed catches surfacing previously swallowed failures (T017).
- Docs updates: retry promise (T007); redirect collapse (T023); validator registration convention (T026); resolver subpath semantics (T004); truncation spellings (T021).

## Implementation notes

- Repo conventions: follow `CONTRIBUTING.md`; CI runs tests from `tests/CliInvoke.Tests/` on Ubuntu-latest (see `.github/workflows/test.yml`); use the `cliinvoke-inner-loop`, `cliinvoke-pattern-validator`, and `run-tests` skills during implementation (per the audit handoff's suggested-skills table).
- Pattern guard: middleware changes (T006, T007) apply to the `ProcessInvoker` pattern only — `IExternalProcess` bypasses middleware (AGENTS.md middleware asymmetry).

## Ledger Reference

- `docs/decisions/DECISIONS-CliInvoke-bug-audit-fixes.md` — D001 (session goal), T001 (foundation), T002–T029 (fix decisions), I001–I012 (interactions).
