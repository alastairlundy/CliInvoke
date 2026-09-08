# Decision Ledger — CliInvoke bug audit fixes

Session: technical-grilling over the 2026-09-07 bug audit (handoff `cliinvoke-bug-audit-handoff-2026-09-07.md`). Triage order High → Medium → Low; per-finding, case-by-case inclusion and approach.

### [D001] - session goal

- **Driver**: the user wants to decide on a case-by-case basis which audit findings become fixes and how each one is addressed.
- **Resolved Answer**: "New ledger path." / "Where the audit overlaps, we can re-open those branches with the audit's context" / "Let's triage the audit's findings in High -> Medium - Low." / "The audit's results are not binding and I want to decide on a case by case basis which finding to address and how to address it if I decide to address it." / confirmed the orchestrator's goal reading (decide which findings become fixes and resolve the behavior questions they force).
- **Normalized Requirement**: The session shall resolve, per audit finding, whether to fix it and what behavior the fix implements, producing Txxx records an implementer can act on without re-asking the user.
- **Constraints**: track: concept-then-implementation. Audit findings are input data, not binding decisions. Traversal order High → Medium → Low; inclusion is case-by-case per finding. v4-ledger branches (T005, T007, D004, D006) may be re-opened with the audit's context where they overlap. No @oracle this session. New ledger, separate from `DECISIONS-CliInvoke-v4-improvements.md`.

### [I001] - Gate A intake

- **Prompt**: "Confirm or amend: (1) Are the v4 ledger records (T005, T007, D004, D006) binding constraints where they overlap, or open? (2) Is the handoff's High → Medium → Low priority order locked, or open for re-triage? (3) Is 'no @oracle' still binding? Ledger path: fresh `docs/decisions/DECISIONS-CliInvoke-bug-audit-fixes.md` — OK? Goal: is it deciding which findings become fixes and resolving the behavior questions they force — and is the track concept-then-implementation?"
- **User Response**: "New ledger path. Where the audit overlaps, we can re-open those branches with the audit's context. Let's triage the audit's findings in High -> Medium - Low. The audit's results are not binding and I want to decide on a case by case basis which finding to address and how to address it if I decide to address it. Your reading of the handoff is correct."
- **Resolution**: ledger path confirmed as a new file; v4-ledger overlap items treated as re-openable rather than binding; triage traversal fixed High → Medium → Low; per-finding inclusion and approach explicitly user-owned; goal confirmed as stated; track defaults to concept-then-implementation.
- **Notes**: the Gate A settled-item table (T005, T007, D004, D006) was presented and superseded by the re-open ruling; audit findings are data only.

### [I002] - Gate B and foundation

- **Prompt**: "Gate B — concept readiness: the domain concepts are aligned via GLOSSARY.md and the audit's technical facts — confirm, or redirect to concept alignment first? Foundation: locked by repo state and v4-ledger T001 (C#/.NET 10, three shipping projects plus existing test projects, no new projects or packages) — confirm as locked, or open it?"
- **User Response**: "Foundation is locked. Proceed"
- **Resolution**: Gate B passed; foundation locked and recorded as T001; Phase 2 opened with the High tier.
- **Notes**: ShellDetector.cs, UnixProcessControlAdapter.cs, FilePathResolver.cs, BaseProcessControlAdapter.cs, and WindowsProcessControlAdapter.cs were read to ground the High-tier branches.

### [T001] - foundation

- **Driver**: the user wants bounded audit fixes without structural churn; no finding implies new projects, packages, or framework changes.
- **Resolved Answer**: "Foundation is locked." — C#/.NET 10, the three shipping projects (Core, CliInvoke, Specializations) plus existing test projects, no new projects or packages; fixes modify existing files only.
- **Normalized Requirement**: All fix work from this session shall land in existing projects/files; no new projects, packages, or target-framework changes.
- **Constraints**: Cross-ledger: mirrors `DECISIONS-CliInvoke-v4-improvements.md#T001`.
- **Cites**: D001

### [I003] - Round 1 picks

- **Prompt**: "For T002 – ShellDetector cancellation registration / T003 – Unix admin/credential silent no-ops / T004 – FilePathResolver Unix filename match: pick an option, hybridize, or provide your own answer."
- **User Response**: "T002: Option A / T003: Option A / T004: Option A"
- **Resolution**: all three High-tier branches resolved as their recommended options; recorded as T002, T003, T004.
- **Notes**: no pushback on any recommendation.

### [T002] - ShellDetector cancellation registration

- **Driver**: the user wants standard, prompt cancellation semantics on a public API rather than the minimal diff.
- **Resolved Answer**: "Option A — Remove Register + checkpoints"
- **Normalized Requirement**: `ShellDetector`'s Unix flow shall not throw from a `Register` callback; the throwing `Register` shall be deleted and `ThrowIfCancellationRequested` checkpoints shall guard the flow so cancellation surfaces promptly.
- **Constraints**: Checkpoints go before/after the awaits in the Unix flow; sync-stage cancellation still waits for the next checkpoint (accepted). No registration remains in the flow. Windows flow untouched.
- **Cites**: D001, T001

### [T003] - Unix admin/credential silent no-ops

- **Driver**: the user wants an honest cross-platform contract; pre-GA is the cheap moment to break.
- **Resolved Answer**: "Option A — Throw on Unix"
- **Normalized Requirement**: `UnixProcessControlAdapter.RequireRunningAsAdmin` shall throw `PlatformNotSupportedException`, and `UnixProcessControlAdapter.SetUserCredential` shall throw `PlatformNotSupportedException` for a non-null credential; Unix admin/credential requests fail loudly at invocation setup.
- **Constraints**: The `SetUserCredential` throw must be guarded on non-null credential — `ApplyConfiguration` calls it unconditionally (`BaseProcessControlAdapter.cs:76`). Windows behavior unchanged. Docs and release notes required for the breaking change.
- **Cites**: D001, T001

### [T004] - FilePathResolver Unix filename match

- **Driver**: the user wants the bug fixed and the resolver's contract pinned in docs, matching the repo's documented-contract culture.
- **Resolved Answer**: "Option A — Fix + document"
- **Normalized Requirement**: The directory-enumeration match predicate shall compare `f.Name` against `fileName` (extracted via `Path.GetFileName`) on both platforms — `Ordinal` on Unix, `OrdinalIgnoreCase` on Windows — with XML remarks documenting the inferred-directory semantics for relative subpaths.
- **Constraints**: The Windows extension-lowercasing quirk (`:207-225`) and the PATH-first strategy are excluded (glossary-locked). Regression tests required for relative-subdir resolution.
- **Cites**: D001, T001

### [I004] - Round 2 picks and T005 restructure

- **Prompt**: "For T005 – Equality contracts (Core primitives) / T006 – RetryMiddleware delay overflow / T007 – RetryMiddleware exception classification: pick an option, hybridize, or provide your own answer."
- **User Response**: "T005 bundles too many decisions together. I want these split up. / T006: Option A / T007: Option B"
- **Resolution**: T006 and T007 resolved as recorded; T005 restructured — the equality bundle is split into per-decision branches (recorded as T005), with the split branches opening in Round 3.
- **Notes**: no pushback on the T006/T007 recommendations.

### [T005] - equality branch restructure

- **Driver**: the user wants each equality defect resolved as its own decision rather than one bundled verdict.
- **Resolved Answer**: "T005 bundles too many decisions together. I want these split up."
- **Normalized Requirement**: The six equality defects shall be resolved in separate branches grouped by distinct decision — types sharing an identical decision may share a branch — not as a single bundle.
- **Constraints**: The bundle's code-check facts remain valid input to the split branches; the user may split shared-decision branches further per type on request.
- **Cites**: D001

### [T006] - RetryMiddleware delay overflow

- **Driver**: the user wants the clamp the code already documents to actually hold for any attempt count.
- **Resolved Answer**: "Option A — Overflow-checked arithmetic"
- **Normalized Requirement**: `ComputeDelay` shall compute exponential and linear delays with overflow-checked arithmetic that clamps to `MaxTaskDelay` on would-be overflow; any (BaseDelay, MaxAttempts) pair yields a valid non-negative delay.
- **Constraints**: Construction-time bound rejection (Option B) not adopted. Overflow-boundary tests required.
- **Cites**: D001, T001

### [T007] - RetryMiddleware exception classification

- **Driver**: the user wants the verifiable defect (docs/code mismatch) fixed without feature-grade scope creep.
- **Resolved Answer**: "Option B — Honest docs"
- **Normalized Requirement**: `RetryMiddleware` XML docs shall state that retries apply to classifier-approved results and that exceptions from the pipeline propagate without retry; no exception-classification code ships.
- **Constraints**: The cancellation half of the audit item needs no change (`Task.Delay` already observes the token). Exception retry remains open for a future feature decision; the prior ledger cited in the type's remarks is not on disk.
- **Cites**: D001, T001

### [I005] - Round 3 picks

- **Prompt**: "For T008 – Result operator null-safety / T009 – BufferedProcessResult ProcessId consistency / T010 – Configuration/exception-info operator null pattern: pick an option, hybridize, or provide your own answer."
- **User Response**: "T008: Option A / T009: OptioN A / T010: Option A"
- **Resolution**: all three resolved as recommended; recorded as T008, T009, T010.
- **Notes**: no pushback on any recommendation.

### [T008] - Result operator null-safety

- **Driver**: the user wants conventional operator behavior on public types rather than crash-prone comparisons.
- **Resolved Answer**: "Option A — Standard null-safe semantics"
- **Normalized Requirement**: `ProcessResult` and `BufferedProcessResult` `==`/`!=` shall implement standard null-safe equality — `null == null` true, `null == x` false, never throwing.
- **Constraints**: `null == null` changes from NRE to true — accepted behavior change. Pattern applies to both result types identically.
- **Cites**: D001, T005

### [T009] - BufferedProcessResult ProcessId consistency

- **Driver**: the user wants equality answers independent of compile-time type.
- **Resolved Answer**: "Option A — Include ProcessId"
- **Normalized Requirement**: `BufferedProcessResult.Equals`/`GetHashCode` shall include `ProcessId`, matching the base `ProcessResult` contract.
- **Constraints**: Stricter equality accepted (the same command re-run with a different PID compares unequal). The exclude-with-documentation alternative was not adopted.
- **Cites**: D001, T005, T008

### [T010] - Configuration/exception-info operator null pattern

- **Driver**: the user wants one equality convention across the library.
- **Resolved Answer**: "Option A — Match T008 semantics"
- **Normalized Requirement**: `ProcessConfiguration` and `ProcessExceptionInfo<TProcessResult>` `==`/`!=` shall implement standard null-safe equality — `null == null` true, `null == x` false.
- **Constraints**: `null == null` changes from false to true — accepted. Same pattern as T008.
- **Cites**: D001, T005, T008

### [I006] - Round 4 picks

- **Prompt**: "For T011 – UserCredential hash source / T012 – ShellInformation Version symmetry / T013 – ConfigureAwait policy: pick an option, hybridize, or provide your own answer."
- **User Response**: "T011: Option A / T012: Option A / T013: Option A"
- **Resolution**: all three resolved as recommended; recorded as T011, T012, T013.
- **Notes**: no pushback on any recommendation.

### [T011] - UserCredential hash source

- **Driver**: the user wants the equality contract to hold without unwrapping secret material.
- **Resolved Answer**: "Option A — Hash non-secret fields"
- **Normalized Requirement**: `UserCredential.GetHashCode` shall hash Domain, UserName, and LoadUserProfile only; `Password` participates in `Equals` content comparison but never in `GetHashCode`.
- **Constraints**: Collisions possible for distinct passwords with the same user/domain — contract-legal, documented in XML docs. No SecureString unwrapping in hash paths.
- **Cites**: D001, T005, T008

### [T012] - ShellInformation Version symmetry

- **Driver**: the user wants shell identity, including version, to be part of value equality.
- **Resolved Answer**: "Option A — Include Version in Equals"
- **Normalized Requirement**: `ShellInformation.Equals` shall consider `Version`, matching `GetHashCode`.
- **Constraints**: Instances differing only by version compare unequal — accepted. No API shape change.
- **Cites**: D001, T005, T008

### [T013] - ConfigureAwait policy

- **Driver**: the user wants library-wide correct continuation semantics that stay enforced without new packages.
- **Resolved Answer**: "Option A — Sweep + CI guard"
- **Normalized Requirement**: All await sites in `src` library code shall use `ConfigureAwait(false)`, enforced by a package-free CI guard that fails on bare awaits in `src`.
- **Constraints**: Guard must whitelist non-guardable awaits (e.g., `await foreach`, `await using`); test projects excluded; no analyzer package (T001 intact).
- **Cites**: D001, T001

### [I007] - Round 5 picks

- **Prompt**: "For T014 – PowershellProcessConfiguration TargetFilePath / T015 – Cancellation-reason race / T016 – ExternalProcess lifecycle synchronization: pick an option, hybridize, or provide your own answer."
- **User Response**: "T014: Option A / T015: Option A / T016: Option A"
- **Resolution**: all three resolved as recommended; recorded as T014, T015, T016.
- **Notes**: no pushback on any recommendation.

### [T014] - PowershellProcessConfiguration TargetFilePath

- **Driver**: the user wants the public property truthful with minimal surface change.
- **Resolved Answer**: "Option A — Delegate to base"
- **Normalized Requirement**: `PowershellProcessConfiguration.TargetFilePath` shall be an expression-bodied `new` property delegating to `base.TargetFilePath`, which the base ctor already resolves per OS.
- **Constraints**: Cmd's ctor re-assign quirk (`:71`) untouched — separate cleanup if wanted. Must not preclude v4-ledger T005's required-init conversion of both specializations.
- **Cites**: D001, T001

### [T015] - Cancellation-reason race

- **Driver**: the user wants deterministic Canceled-state classification and resource discipline.
- **Resolved Answer**: "Option A — Compute at catch point"
- **Normalized Requirement**: Both `WaitForExitOrForcefulTimeoutAsync` and `CancelWithInterrupt` shall delete their Register callbacks and compute the cancellation reason via `CancellationHelper.GetCancellationReason` inside the catch, where the token is known-canceled.
- **Constraints**: Reason computed at catch rather than cancel-fire — same information, accepted. Both registrations removed (leak gone). Semaphore discipline and ForcefulExit placement out of scope (Low tier).
- **Cites**: D001, T002

### [T016] - ExternalProcess lifecycle synchronization

- **Driver**: the user wants code-level lifecycle safety on a public type whose surface invites cross-thread calls.
- **Resolved Answer**: "Option A — Lock + snapshot reads"
- **Normalized Requirement**: `ExternalProcess` shall serialize the start gate and `_processWrapper` swap under a private lock; readers shall capture the wrapper reference under the lock and operate on the snapshot, with long awaits outside the lock.
- **Constraints**: Interlocked state machine and documented-contract alternatives not adopted. Awaits must not hold the lock.
- **Cites**: D001, T001

### [I008] - Round 6 picks

- **Prompt**: "For T017 – Bare catch policy / T018 – Dead-code removal / T019 – Started event null-guard: pick an option, hybridize, or provide your own answer."
- **User Response**: "T017: Option A / T018: Option A / T019: Option A"
- **Resolution**: all three resolved as recommended; recorded as T017, T018, T019.
- **Notes**: no pushback on any recommendation.

### [T017] - Bare catch policy

- **Driver**: the user wants failure semantics that surface genuine failures, per the glossary's catch-discipline culture.
- **Resolved Answer**: "Option A — Narrow + justify"
- **Normalized Requirement**: Each of the five bare-catch sites shall catch only its documented expected exception(s) with a justification comment; unexpected exceptions, including `OperationCanceledException` in ShellDetector's pwsh→cmd fallback, shall propagate.
- **Constraints**: Behavior change accepted — previously swallowed failures now throw. Exception lists per site are implementation detail for the implementer.
- **Cites**: D001, T001

### [T018] - Dead-code removal

- **Driver**: the user wants no false platform signals for future readers.
- **Resolved Answer**: "Option A — Remove all three"
- **Normalized Requirement**: The dead OS terms in `UnixProcessControlAdapter` (`:71`) and the Windows adapter, and the unreachable re-check at `ProcessWrapper.cs:177`, shall be removed.
- **Constraints**: The Windows-adapter site is audit-reported — verify by direct read before deleting.
- **Cites**: D001, T001

### [T019] - Started event null-guard

- **Driver**: the user wants standard, trap-free event raising.
- **Resolved Answer**: "Option A — Null-conditional invoke"
- **Normalized Requirement**: `ProcessWrapper.cs:213` shall raise the Started event via `Started?.Invoke(...)`.
- **Constraints**: None.
- **Cites**: D001, T001

### [I009] - Round 7 picks

- **Prompt**: "For T020 – ForcefulExit on normal exit / T021 – Truncation sentinel semantics / T022 – UTF-8 boundary split: pick an option, hybridize, or provide your own answer."
- **User Response**: "T020: Option A / T021: Change <= 0 to < 0 for 'no cap'. / T022: Option A"
- **Resolution**: T020 and T022 resolved as recommended; T021 resolved with the user's own semantics — negative joins null as no-cap, zero becomes a valid zero-byte cap; recorded as T020–T022.
- **Notes**: the T021 answer defines a three-way spelling (null/negative = no cap; 0 = zero-byte cap); XML docs must state all three.

### [T020] - ForcefulExit on normal exit

- **Driver**: the user wants honest exit behavior with the smallest conceptual cost.
- **Resolved Answer**: "Option A — Self-guarding ForcefulExit"
- **Normalized Requirement**: `ForcefulExit` shall check `HasExited` and no-op on an exited process, making the unguarded finally call at `ProcessWrapper.cs:675` safe on normal exits.
- **Constraints**: Existing call-site guards become redundant (harmless); benign HasExited snapshot races accepted. ForcefulExit's internal bare catch (`:434-436`) joins the T017 sweep.
- **Cites**: D001, T015, T017

### [T021] - Truncation sentinel semantics

- **Driver**: the user wants distinct, explicit meanings for the cap boundary rather than a silent unbounded sentinel.
- **Resolved Answer**: "Change <= 0 to < 0 for 'no cap'."
- **Normalized Requirement**: `ReadStreamCappedAsync` shall treat `null` and negative `maxBytes` as no cap; `0` shall be a valid zero-byte cap producing empty text with the truncated flag set.
- **Constraints**: XML docs shall spell out all three spellings (null = no cap, negative = no cap, 0 = zero-byte cap). No exception-based validation (Option A's throw) — the boundary stays permissive.
- **Cites**: D001

### [T022] - UTF-8 boundary split

- **Driver**: the user wants clean truncation output for multibyte content.
- **Resolved Answer**: "Option A — Decoder-based decode"
- **Normalized Requirement**: The capped read shall decode incrementally with `encoding.GetDecoder()` so a split trailing sequence is held back and dropped cleanly instead of becoming U+FFFD.
- **Constraints**: Boundary tests with multibyte content required; the drain loop and cap semantics (T021) unchanged.
- **Cites**: D001, T021

### [I010] - Round 8 picks

- **Prompt**: "For T023 – Redirect granularity collapse / T024 – $HOME stale index / T025 – CachingFilePathResolver hardening: pick an option, hybridize, or provide your own answer."
- **User Response**: "T023: Option A - There will be no surface change though. / T024: Option A / T025: Option A"
- **Resolution**: all three resolved as recommended; T023's no-surface-change emphasis recorded as a binding constraint; recorded as T023–T025.
- **Notes**: no pushback on T024/T025 recommendations.

### [T023] - Redirect granularity collapse

- **Driver**: the user wants the lossy conversion documented honestly without any surface change.
- **Resolved Answer**: "Option A — Document the collapse" — with the explicit constraint: "There will be no surface change though."
- **Normalized Requirement**: `FromProcessStartInfo`'s XML docs shall state that per-stream redirect flags collapse via OR into the configuration's single `OutputRedirection` flag; no public surface changes.
- **Constraints**: No per-stream redirect properties are added in this session; any future per-stream surface is a separate v4 decision (interacts with v4-ledger T005).
- **Cites**: D001, T001

### [T024] - $HOME stale index

- **Driver**: the user wants correct expansion with cleaner code at the same effort.
- **Resolved Answer**: "Option A — Restructure the pass"
- **Normalized Requirement**: `EnumerateDirectories` shall expand `~` first and then locate `$HOME` on the expanded string in a single sequential pass, so the token index is never stale.
- **Constraints**: Mixed `~`/`$HOME` entries now expand correctly — accepted behavior change. One `GetFolderPath` fetch per entry.
- **Cites**: D001, T001

### [T025] - CachingFilePathResolver hardening

- **Driver**: the user wants the real benefit (no redundant Windows entries) without lock overhead for a value-idempotent race.
- **Resolved Answer**: "Option A — Normalize + document"
- **Normalized Requirement**: Cache keys shall be normalized per-OS casing rules (case-insensitive on Windows, as-is elsewhere); the benign concurrent-compute race shall be documented in XML remarks.
- **Constraints**: No single-flight lock; the per-hit `File.Exists` re-verification (TOCTOU mitigation) stays as-is.
- **Cites**: D001, T001

### [I011] - Round 9 picks

- **Prompt**: "For T026 – Validator registration semantics / T027 – Test strategy / T028 – Output format: pick an option, hybridize, or provide your own answer."
- **User Response**: "T026: Option A / T027: Option A / T028: Option A"
- **Resolution**: all three resolved as recommended; recorded as T026, T027, T028.
- **Notes**: no pushback on any recommendation.

### [T026] - Validator registration semantics

- **Driver**: the user wants honest registration semantics at zero practical cost.
- **Resolved Answer**: "Option A — Add + document"
- **Normalized Requirement**: Both methods shall use `Add{Lifetime}` instead of `TryAdd{Lifetime}` after `RemoveAll`, with XML docs stating the single-threaded registration convention.
- **Constraints**: `IServiceCollection` remains non-thread-safe by convention; no locking added.
- **Cites**: D001, T001

### [T027] - Test strategy

- **Driver**: the user wants the strongest practical test evidence, matching the repo's Fuzzing suite and v4-ledger T007 precedent.
- **Resolved Answer**: "Option A — Regression + property tests"
- **Normalized Requirement**: Each behavior-changing fix shall get regression tests in the existing suites; the equality contracts (T008–T012) shall additionally get FsCheck property tests, extending v4-ledger T007's property-test scope to these fixes.
- **Constraints**: Unix-path fixes (T003, T004) verify on Ubuntu CI — tests must be OS-conditional or CI-verified; docs-only fixes (T007, T023) have no runtime tests; T013's CI guard lands in the workflow.
- **Cites**: D001, T008, T009, T010, T011, T012, T013

### [T028] - Output format

- **Driver**: the user wants decisions and implementation detail in separate, scannable artifacts, per both prior sessions.
- **Resolved Answer**: "Option A — Implementation Blueprint"
- **Normalized Requirement**: The session shall produce a standalone blueprint at the repo root with a Scope Binding section linking the audit handoff and this ledger, inline `filename#Txxx` citations for every technical statement, and a Ledger Reference section.
- **Constraints**: Filename confirmed in the next branch. Downstream consumer recorded in T029.
- **Cites**: D001, T026, T027

### [I012] - Round 10 pick

- **Prompt**: "For T029 – Downstream consumer: pick an option, hybridize, or provide your own answer."
- **User Response**: "manual handoff"
- **Resolution**: manual handoff; recorded as T029.
- **Notes**: mirrors the v4 session's T009 choice.

### [T029] - Downstream consumer

- **Driver**: the user wants to drive downstream work themselves, per both prior sessions.
- **Resolved Answer**: "Manual handoff"
- **Normalized Requirement**: The blueprint and ledger shall be written; no automated ticket or issue decomposition follows; the user drives implementation.
- **Constraints**: The agent does not launch downstream workflows.
- **Cites**: T028

### [I013] - ticket decomposition intake

- **Prompt**: "You asked to decompose IMPLEMENTATION-bug-audit-fixes.md into tickets, which re-opens T029's manual-handoff constraint for ticket creation only — the ledger's Dxxx/Txxx records stay binding as written. Confirm or amend: (1) the re-open of T029 for this purpose; (2) output target — GitHub Issues (repo convention per docs/agents/issue-tracker.md) or local markdown files; (3) PR grouping — one PR for the whole batch, or split (e.g., by domain area)?"
- **User Response**: "Local markdown files" / "One PR (Recommended)"
- **Resolution**: tickets publish to local markdown under `tickets/` at the repo root; PR count resolved to 1 — all tickets grouped under a single pull request; T029's manual-handoff constraint re-opened for ticket creation only, with the ledger's Dxxx/Txxx records staying binding as written.
- **Notes**: T029 stays on the ledger unamended; this interaction is the re-open record. Local-markdown target means `Blocked by` fields use file basenames at publish time.

<!-- next-d: D002 -->
<!-- next-t: T030 -->
<!-- next-i: I014 -->
