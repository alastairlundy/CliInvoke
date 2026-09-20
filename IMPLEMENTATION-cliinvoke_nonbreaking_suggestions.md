# Implementation Blueprint — Result ergonomics (item #1, additive)

## Scope Binding

- **Linked Spec**: `C:\Users\alast\Desktop\cliinvoke_nonbreaking_suggestiosn.md` §1 ("Result ergonomics")
- **Decision Ledger**: `docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md` (`#D001`–`#D007`, `#T001`–`#T005`)
- **Notice**: This blueprint is a context pointer valid ONLY for the linked spec above and must not be applied to other specifications without explicit authorization.
- **Deliberate departure from the spec**: the spec's instance members and `IsSuccess`/`EnsureSuccess` names were deliberately narrowed/re-placed by the session — see `#D002`–`#D004`, `#D007`. Do not "restore" the spec's shape; the corrections were user-decided and recorded.

## Change footprint

All changes live in `CliInvoke.Core` and `tests/CliInvoke.Tests/` [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#T001`]. No other project, package, or csproj changes.

## Implementation plan, grouped by file

### 1. `src/CliInvoke.Core/Extensions/ProcessResultHelperExtensions.cs`

Four new extension members, added inside the existing static class (no new types) [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#T002`]:

1. **`IsExitCodeZero()`** — extension over `TProcessResult : ProcessResult`; returns `processResult.ExitCode == 0` — literal semantics, no `Canceled` clause (supersedes the spec's `IsSuccess` name; `!Canceled` dropped as redundant under the recorded invariant and dropped under name-honesty either way) [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#D002`, `#D003`].
2. **`EnsureExitCodeZero()`** — same extension scope; when `!IsExitCodeZero()` throws `ProcessNotSuccessfulException<ProcessResult>` built from `ProcessExceptionInfo<ProcessResult>` so both throw paths share one exception family. Core re-implements rather than delegating to the main package's `ExitCodeIsZero()` factory (which cannot move without relocation cost; rejected during the session) [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#D003`]. Use the existing exception construction path (`ProcessExceptionInfo<T>(result)`); watch the `IDisposable` shape of `ProcessExceptionInfo<T>` in the throw site and keep scope lifetime under the repo's disposable-audit conventions.
3. **`EnumerateOutputLines()`** and **`EnumerateErrorLines()`** — lazy `IEnumerable<string>` over `BufferedProcessResult.StandardOutput` / `.StandardError` [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#D004`]. **Implementation contract**: lines must be produced identically to `Split(Environment.NewLine)` results — do NOT reach for `MemoryExtensions.EnumerateLines`, whose separator semantics differ (it treats `\n` as a separator, diverging from the pinned contract). A lazy iterator that walks consecutive `Environment.NewLine` separators preserves the contract while staying per-enumeration-cheap over the already-resident string.

XML remarks on every member must cross-reference `ThrowIfUnsuccessful` and the validator/`UsePostExitValidation` machinery as the authoritative caller-stated success path (the anti-mooting mitigation agreed during the session; doc-comment validity is build-enforced in this repo via CS1574/1580 erroring) [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#D003`].

### 2. `src/CliInvoke.Core/Primitives/Results/ProcessResult.cs`

Add one member [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#D005`]:

```csharp
public override string ToString() =>
    $"[ExitCode={ExitCode}, Path={ExecutedFilePath}, Runtime={RuntimeDuration}]";
```

Single-line, no multi-line output; the format becomes de facto frozen for log consumers (constraint of `#D005`).

### 3. `src/CliInvoke.Core/Primitives/Results/BufferedProcessResult.cs`

Add two members [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#D005`, `#D006`]:

1. **`ToString()` override** — base-format prefix plus output length indicators (e.g., `StdOutLen`, `StdErrLen`) and `Truncated` only when `WasTruncated`; never embed output content (log lines stay bounded in size).
2. **`Deconstruct`** — exactly one overload:

```csharp
public void Deconstruct(out int exitCode, out string stdout, out string stderr)
```

No additional arities until demonstrated demand (frozen-surface constraint of [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#D006`]). Note the compiler-binds-by-pattern mechanics: an instance `Deconstruct` shadows any user-supplied extension `Deconstruct` without error — the recorded choice is the idiomatic one.

### 4. Declined per record (implement nothing)

- Cached lazy `StandardOutputLines`/`StandardErrorLines` instance properties — declined; line access stays with the extensions [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#D007`]. Revisit only on a demonstrated split-allocation bottleneck via a new `Supersedes: D004/D007` record.

### 5. `tests/CliInvoke.Tests/`

Unit + property coverage [docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#T003]:

- **Unit tests** (TUnit, CI's only run project): `IsExitCodeZero` truth table (zero/non-zero, canceled-zero observation); `EnsureExitCodeZero` throw path — exception type + `ProcessExceptionInfo` population vs no-throw path; `EnumerateOutputLines`/`EnumerateErrorLines` parity with `GetOutputLines()` split results (empty string, single line, trailing newline, multi-line); `ToString` format snapshots for both types incl. truncation indicator; `Deconstruct` trio assignment.
- **FsCheck property tests**: enumeration parity holds for arbitrary strings; `ToString` shape stability (single line, bracketed prefix on `[ExitCode=…`) across arbitrary field values.
- Test exec spawning caveat does not apply — these are pure result-object tests (no `pwsh`/PATH dependency).

## Docs & release touchpoints

- New public API → update `site/docs/` relevant result pages and `CHANGELOG.md` per `CONTRIBUTING.md` (user-driven; manual handoff [`docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md#T005`]).
- No `AGENTS.md`/pattern changes — no invocation-pattern code touched; `cliinvoke-pattern-validator` skill unnecessary for this footprint.

## Ledger Reference

| Record | Title |
|--------|-------|
| `#D001` | session goal |
| `#D002` | IsSuccess semantics and placement (extension-only; superseded by `#D003` naming) |
| `#D003` | throwing-success surface: renamed exit-code-zero extensions |
| `#D004` | line enumeration semantics and placement |
| `#D005` | ToString override |
| `#D006` | Deconstruct |
| `#D007` | cached line accessors dropped |
| `#T001` | foundation items |
| `#T002` | member placement within Core |
| `#T003` | test scope |
| `#T004` | output format |
| `#T005` | downstream consumer |
