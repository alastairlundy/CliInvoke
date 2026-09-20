# Decision Ledger — Result ergonomics (v3 additive)

Session: technical-grilling over item #1 ("Result ergonomics") of `C:\Users\alast\Desktop\cliinvoke_nonbreaking_suggestiosn.md`, re-baselined against repo state v3 `3.0.0`.

### [D001] - session goal

- **Driver**: the user wants the `ProcessResult`/`BufferedProcessResult` ergonomic conveniences pressure-tested and resolved into implementable decisions without breaking existing API.
- **Resolved Answer**: "Grill me on implementing item #1 in `cliinvoke_nonbreaking_suggestiosn.md`" — "All prior-art determinations hold; track concept-then-implementation confirmed."
- **Normalized Requirement**: The session shall resolve the six §1 sub-items — `IsSuccess`, `EnsureSuccess()`, `ProcessResult.ToString()`, `EnumerateOutputLines()`/`EnumerateErrorLines()`, `Deconstruct`, and cached lazy `StandardOutputLines`/`StandardErrorLines` — into decision records an implementer can act on without re-asking the user.
- **Constraints**: track: concept-then-implementation (user-confirmed). Locked by user confirmation: GLOSSARY Get-/Enumerate- naming convention binds line-accessor naming; `decisions/DECISIONS-CliInvoke-v4-improvements.md#D010` "ToString unsurprising" constraint covers `ProcessConfiguration.ToString`, not result types (not binding here); v4 ledger `#T007` is precedent only. All six sub-items confirmed open. Catalog additions only — no existing member may change (spec's binary-compatibility rule). Ledger path confirmed: `docs/decisions/DECISIONS-cliinvoke-result-ergonomics.md`.

### [D004] - line enumeration semantics and placement

- **Driver**: the user values placement symmetry with the existing extension helpers over CliWrap-style instance ergonomics, and holds that `Environment.NewLine` splitting is the correct existing contract.
- **Resolved Answer**: "Option C - extension-only, symmetric with the existing helpers" ("Your Option D recommendation is just wrong... We're going with Option C").
- **Normalized Requirement**: `EnumerateOutputLines()` and `EnumerateErrorLines()` shall ship as extension methods in `ProcessResultHelperExtensions` alongside `GetOutputLines()`, using the same `Environment.NewLine` splitting contract; no new public instance line members on `BufferedProcessResult`.
- **Constraints**: Names locked lazy per the GLOSSARY Get/Enumerate convention (D001); no newline-agnostic re-splitting semantics introduced (span-based splitting rejected as less correct for this contract).
- **Cites**: D001

### [D002] - IsSuccess semantics and placement

- **Driver**: the user wants the success concept to serve the non-DI/bypass audience without installing a second success ontology on the result type itself, after isolating who `IsSuccess` actually helps.
- **Resolved Answer**: "Option C — IsSuccess as an extension in Core's `ProcessResultHelperExtensions`".
- **Normalized Requirement**: `IsSuccess` shall ship as an extension method in `ProcessResultHelperExtensions` with semantics `ExitCode == 0 && !Canceled`; `ProcessResult` shall gain no intrinsic success members.
- **Constraints**: Serves the `IExternalProcess`-bypass and non-DI `CliRun` audiences only — it is documented as a default heuristic, not the validator's ruling (validators remain the authoritative caller-stated success vocabulary). Placement symmetry with D004. No instance success property.
- **Cites**: D001, D004

### [D003] - throwing-success surface: renamed exit-code-zero extensions

- **Driver**: the user recognized nothing structurally stops DI/main-package users from abusing a generic `EnsureSuccess`, so names themselves must carry the narrow intent rather than relying on docs.
- **Resolved Answer**: "Reframe as `IsExitCodeZero()` and `EnsureExitCodeZero()`" — extension methods in Core's `ProcessResultHelperExtensions`; `EnsureExitCodeZero()` throws `ProcessNotSuccessfulException<ProcessResult>` built from `ProcessExceptionInfo` when the semantics fail; `IsSuccess`/`EnsureSuccess` names dropped.
- **Normalized Requirement**: Success-flavored conveniences shall ship as name-honest exit-code-zero extensions — `IsExitCodeZero()` mirroring the main package's `ExitCodeIsZero()` validator semantics (literal `ExitCode == 0`), and `EnsureExitCodeZero()` throwing through the shared `ProcessNotSuccessfulException`/`ProcessExceptionInfo` family; XML docs cross-reference `ThrowIfUnsuccessful` and validators as the authoritative caller-stated path.
- **Constraints**: Supersedes D002's member names ("IsSuccess") and narrows its semantics to literal `ExitCode == 0` (the `!Canceled` clause is dropped — cancellation of a zero-exit process is a caller-policy question, not an exit-code fact). Instance success members remain rejected (D002). Core re-implements (not delegates to) the main package's validator factory; the two throw paths share one exception family.
- **Cites**: D001, D002, D004

### [D005] - ToString override

- **Driver**: the user wants real diagnostic value from the spec's stated debugging goal, with output length shown rather than content dumped into log lines.
- **Resolved Answer**: "Option A" — spec-format `[ExitCode=0, Path=dotnet, Runtime=00:00:01.234]` overridden on `ProcessResult`; `BufferedProcessResult` appends stdout/stderr length indicators (plus `WasTruncated` when set), never output content.
- **Normalized Requirement**: Both result types shall override `ToString()` with the bracketed compact format; buffered results show output lengths, not lines; no other members change.
- **Constraints**: Format becomes de facto frozen for parsing/readability; no newline or multi-line output; equality members untouched.
- **Cites**: D001

### [D006] - Deconstruct

- **Driver**: the user accepts speculative surface only where the compiler idiom itself grants discoverability (instance `Deconstruct` is compiler-recognized; extension `Deconstruct` is a quirk).
- **Resolved Answer**: "Option A" — single instance `void Deconstruct(out int exitCode, out string stdout, out string stderr)` on `BufferedProcessResult` per the spec trio.
- **Normalized Requirement**: `BufferedProcessResult` shall gain exactly one `Deconstruct` overload exposing the spec's `(exitCode, stdout, stderr)` trio; base `ProcessResult` gains no deconstruction.
- **Constraints**: No additional arities until real demand; arity ambiguity is the frozen surface.
- **Cites**: D001

### [D007] - cached line accessors dropped

- **Driver**: the user's additivity-minimalism instinct wins — the cached lazy accessor proposal is optimization without demonstrated need, contradicting the D004 no-instance-line-members rule.
- **Resolved Answer**: "Option A" — no `StandardOutputLines`/`StandardErrorLines` instance properties; line access stays D004's extension enumeration.
- **Normalized Requirement**: `BufferedProcessResult` shall gain no cached line-access members; repeated access uses `EnumerateOutputLines()`/`EnumerateErrorLines()` extensions or local materialization.
- **Constraints**: Declined optimization is recorded for the record; revisit only if measured split-allocation cost is a demonstrated bottleneck (new record would Supersede D004/D007).
- **Cites**: D001, D004

### [T001] - foundation items

- **Driver**: the user wants capability additions with zero structural churn, consistent with the prior session's locked-in-place foundation.
- **Resolved Answer**: "Option A — Locked in place" (v4 precedents mirrored).
- **Normalized Requirement**: All members land in existing `CliInvoke.Core` result/extension source files; C#/.NET 10, CPM, the three shipping projects, `tests/CliInvoke.Tests/`, and library project type remain unchanged.
- **Constraints**: No new projects; no new packages; Core public API grows exactly per D002–D007.
- **Cites**: D001

### [T002] - member placement within Core

- **Driver**: the user wants new members, zero new types — the tightest expression of D001's additivity.
- **Resolved Answer**: "Option A — Extend the existing `ProcessResultHelperExtensions` and result files".
- **Normalized Requirement**: `IsExitCodeZero`/`EnsureExitCodeZero`/`EnumerateOutputLines`/`EnumerateErrorLines` join `ProcessResultHelperExtensions`; `ToString`/`Deconstruct` are added in the existing result class files.
- **Constraints**: No new public extension types; helper class stays cohesive (existing ~4 members).
- **Cites**: D001, D003, D004, D005, D006

### [T003] - test scope

- **Driver**: the user wants the strongest practical test evidence per the v4 session precedent (T007 there: migrate + property tests).
- **Resolved Answer**: "Option B — Unit + property tests".
- **Normalized Requirement**: TUnit unit tests for each new member's happy/degenerate path plus FsCheck properties (e.g., `ToString` shape stability across field values; line enumeration over arbitrary strings), in `tests/CliInvoke.Tests/`.
- **Constraints**: CI runs only `tests/CliInvoke.Tests/` (repo constraint); no cross-package test projects; nothing to migrate.
- **Cites**: D001, T001

### [T004] - output format

- **Driver**: the user wants decisions and implementation detail in separate, scannable artifacts, per their prior-session precedent.
- **Resolved Answer**: "Option A — Implementation Blueprint".
- **Normalized Requirement**: The session shall produce a standalone blueprint at the repo root with a Scope Binding section linking the spec and the ledger, inline `filename#Dxxx/Txxx` citations for every technical statement, and a Ledger Reference section.
- **Constraints**: Filename confirmed in the follow-up turn. Downstream consumer recorded in T005.
- **Cites**: D001, T001, T002, T003

### [T005] - downstream consumer

- **Driver**: the user takes the artifacts and drives downstream work themselves.
- **Resolved Answer**: "Manual handoff".
- **Normalized Requirement**: No automated ticket or issue decomposition follows the blueprint; the user drives downstream work from the artifacts.
- **Constraints**: The agent does not launch downstream workflows.
- **Cites**: T004

<!-- next-t: T006 -->
