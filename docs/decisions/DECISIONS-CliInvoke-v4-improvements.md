# Decision Ledger — CliInvoke v4 improvements

Session: technical-grilling over the v4 improvements plan (R7–R10 of `CliInvoke_Improvement_Report.md`), re-baselined against repo state v3 `3.0.0-beta.2`.

### [D001] - session goal

- **Driver**: the user wants the v4 capability-catch-up plan pressure-tested and resolved into implementable decisions.
- **Resolved Answer**: "Grill me on v4 improvements plan" — "Scope is just R7 through R10"
- **Normalized Requirement**: The session shall resolve the four open v4 plan items — R7 (event stream), R8 (PipeSource/PipeTarget), R9 (fluent handle), R10 (UserCredential/SecureString) — into decision records that an implementer can act on without re-asking the user.
- **Constraints**: track: concept-then-implementation (default; user may override). Scope limited to R7–R10. Out of scope: R1/R2/R3/R5/R6 (locked — already implemented in v3 beta.2), R4 remainder (beta→GA), R11, R12, AddCliInvoke package location, unreported disposables (`ProcessExceptionInfo<T>`, `ProcessConfigurationBuilder`) except where R9 directly involves the builder. Prior ledger `DECISIONS-CliInvoke-middleware-deepening.md` decisions constrain where cited. Ledger path confirmed by proposal with no objection: `docs/decisions/DECISIONS-CliInvoke-v4-improvements.md`.

### [I001] - streaming concept

- **Prompt**: "For D002 – streaming concept: pick an option, hybridize, or provide your own answer."
- **User Response**: "D002: Option A"
- **Resolution**: resolved streaming as a pull event stream; recorded as D002.
- **Notes**: recommendation accepted without pushback.

### [I002] - pipe composition concept

- **Prompt**: "For D003 – pipe composition concept: pick an option, hybridize, or provide your own answer."
- **User Response**: "D003: Option A"
- **Resolution**: resolved pipes as data redirection plus Merge, with no process-owning pipes; recorded as D003.
- **Notes**: recommendation accepted without pushback.

### [I003] - credential concept

- **Prompt**: "For D004 – credential concept: pick an option, hybridize, or provide your own answer."
- **User Response**: "D004: Option A"
- **Resolution**: resolved to keep SecureString and drop R10 from the v4 plan; recorded as D004.
- **Notes**: recommendation accepted without pushback.

### [D002] - streaming concept

- **Driver**: the user wants the streaming capability idiomatic and dependency-free, with back-pressure handled by construction.
- **Resolved Answer**: "Option A — Pull event stream"
- **Normalized Requirement**: The v4 streaming capability shall be a pull-based `IAsyncEnumerable<ProcessEvent>` surface; the library produces events as they happen and production paces to consumption; no push/subscription surface ships in v4.
- **Constraints**: Fan-out to multiple consumers is the caller's responsibility (e.g., System.Threading.Channels); no Rx/IObservable dependency. API placement, signatures, and event names deferred to Phase 2.

### [D003] - pipe composition concept

- **Driver**: the user wants composition capabilities without reintroducing process-ownership disposal traps.
- **Resolved Answer**: "Option A — Data redirection + Merge"
- **Normalized Requirement**: The v4 pipe vocabulary shall cover data redirection (streams, files, bytes, strings, delegates) plus Merge; no pipe source or target shall start or own a process.
- **Constraints**: Process-to-process chaining stays manual (caller composes invocations); `FromCommand`-style process-owning pipes are out of v4 scope. Names and attachment points deferred to Phase 2.

### [D004] - credential concept

- **Driver**: the user wants v4 effort spent on the competitive gaps (R7–R9); the Process API boundary requires SecureString regardless of backing choice.
- **Resolved Answer**: "Option A — Keep SecureString"
- **Normalized Requirement**: R10 shall be dropped from the v4 plan; `UserCredential`/`UserCredentialSpec` remain SecureString-backed caller-owned disposables in Core with no v4 rework.
- **Constraints**: Revisit backing only if the .NET Process API boundary for credentials changes in a future major.

### [I004] - middle-tier concept

- **Prompt**: "For D005 – middle-tier concept: pick an option, hybridize, or provide your own answer."
- **User Response**: "D005: I dislike all the options. Give me better options that don't put a static configuration surface on CliRun"
- **Resolution**: D005 re-opened with new options that keep configuration surfaces off the static `CliRun` facade; the constraint is recorded as binding for the branch.
- **Notes**: constraint protects ADR-0003's philosophy — `CliRun` remains a pure per-call execution facade.

### [I005] - middle-tier resolution

- **Prompt**: "For D005 – middle-tier concept: pick an option, hybridize, or provide your own answer."
- **User Response**: "Option A. Let's add a round for what to do with ProcessConfigurationFactory"
- **Resolution**: D005 resolved as config-as-the-middle-tier; a follow-up concept branch (D006) opened for the factory's fate.
- **Notes**: user accepted the recommendation after two clarification turns (option mechanics; factory's current role). The factory question was user-initiated, not agent-proposed.

### [D005] - middle-tier concept

- **Driver**: the user wants the middle tier to be the configuration object itself — no static configuration surface on `CliRun`, no new types.
- **Resolved Answer**: "Option A — Config as the middle tier"
- **Normalized Requirement**: `ProcessConfiguration` shall become directly constructible with all knobs (init setters, public construction path); it shall serve as the middle tier executed through the existing `CliRun` config overloads; no new spec type shall be added for this purpose.
- **Constraints**: No static configuration surface on `CliRun` (I004). Builder and factory fates decided in follow-up branches. Argument-escaping and working-directory validation behaviors must be preserved somewhere. `record` conversion and `required`-modifier sub-decisions deferred to Phase 2.

### [I006] - factory fate

- **Prompt**: "For D006 – factory fate: pick an option, hybridize, or provide your own answer."
- **User Response**: "Option C - Rehome validation to ProcessConfiguration itself"
- **Resolution**: factory removed; working-directory validation rehomed to the config type; recorded as D006.
- **Notes**: user overrode the recommended Option A; the validation rehoming is their refinement.

### [D006] - factory fate

- **Driver**: the user wants one construction path (direct init construction) with the smallest surface, and validation belonging on the type itself.
- **Resolved Answer**: "Option C — Rehome validation to ProcessConfiguration itself"
- **Normalized Requirement**: `ProcessConfigurationFactory` shall be removed; `CliRun` and `ShellDetector` shall construct configurations directly; working-directory validation shall move into `ProcessConfiguration` construction; argument escaping shall be rehomed (destination decided in Phase 2).
- **Constraints**: Construction-time `Directory.Exists` check accepted for factory-parity semantics (throws on non-existent directory). Escaping destination interacts with the builder-fate decision (Phase 2). ~20 test call sites migrate.

### [I007] - D005/D006 timeline

- **Prompt**: "For D007 – D005/D006 timeline: pick an option, hybridize, or provide your own answer."
- **User Response**: "Option A"
- **Resolution**: D005+D006 moved into v3 pre-GA scope; recorded as D007.
- **Notes**: branch was user-initiated — they asked the two timeline questions (D005 breaking? deprecate-vs-remove-now?) that surfaced it; both answered before the branch opened.

### [D007] - D005/D006 timeline

- **Driver**: the user wants the coherent one-construction-path story shipped at GA rather than scheduling a v4 breaking change.
- **Resolved Answer**: "Option A — Ride v3 GA"
- **Normalized Requirement**: The init-construction successor (D005) and the factory removal (D006) shall land in v3 before GA; v3 GA shall ship the final construction shape with no deprecation ceremony.
- **Constraints**: `ArgumentsList` cleanup included (free pre-GA). Expands v3 GA scope; GA timing risk accepted. R7 (event stream) and R8 (pipes) remain the v4 plan's capability items.

### [I008] - foundation items

- **Prompt**: "For T001 – foundation items: pick an option, hybridize, or provide your own answer."
- **User Response**: "Option A"
- **Resolution**: foundation locked by repo state; recorded as T001.
- **Notes**: mirrors the middleware session's T001 outcome with the project set updated (Extensions dissolved).

### [T001] - foundation items

- **Driver**: the user wants capability catch-up without structural churn.
- **Resolved Answer**: "Option A — Locked in place"
- **Normalized Requirement**: All foundation items are locked by repo state — C#/.NET 10, existing NuGet dependencies, the three shipping projects (Core, CliInvoke, Specializations), existing test projects, and library project type; the v4 surfaces and the v3-GA construction story modify existing projects only.
- **Constraints**: No new projects; no new packages; no language or framework changes.
- **Cites**: D001

### [I009] - event-stream surface placement

- **Prompt**: "For T002 – event-stream surface placement: pick an option, hybridize, or provide your own answer."
- **User Response**: "T002: Option A" — with: "For the avoidance of doubt: PipeSource/PipeTarget are not shipping in v3. Neither is ListenAsync."
- **Resolution**: `ListenAsync` + `ProcessEvent` placed on `IProcessInvoker`/Core; recorded as T002; the v4-only shipping constraint recorded in T002 and T004.
- **Notes**: the shipping clarification was stated once and applies to both streaming and pipes.

### [I010] - pipe types and attachment

- **Prompt**: "For T004 – pipe types and attachment: pick an option, hybridize, or provide your own answer."
- **User Response**: "T004: Option A"
- **Resolution**: pipe types in Core attached via config init properties; recorded as T004.
- **Notes**: v4-only shipping confirmed in the same turn (I009).

### [I011] - init conversion mechanics

- **Prompt**: "For T005 – init conversion mechanics: pick an option, hybridize, or provide your own answer."
- **User Response**: "T005: Option A"
- **Resolution**: init setters + required `TargetFilePath`; recorded as T005.
- **Notes**: rides v3 GA per D007.

### [T002] - event-stream surface placement

- **Driver**: the user wants streaming fakeable through the one invoker abstraction, following the Core-contracts precedent.
- **Resolved Answer**: "Option A — Core invoker member"
- **Normalized Requirement**: `ListenAsync` shall be a member of `IProcessInvoker` in CliInvoke.Core; the `ProcessEvent` record hierarchy (Started/StandardOutput/StandardError/Exited using the existing PosixSignal/Canceled vocabulary) shall live in Core.
- **Constraints**: Ships in v4, not v3 (user clarification, I009). Exact record shapes and signature details deferred to the blueprint. No Rx dependency (D002).

### [T004] - pipe types and attachment

- **Driver**: the user wants pipes stated as Invocation Capabilities on the configuration, readable by the pipeline without handoffs.
- **Resolved Answer**: "Option A — Core types, config attachment"
- **Normalized Requirement**: `PipeSource`/`PipeTarget` abstract types and their factories (data redirection + Merge taxonomy) shall live in CliInvoke.Core; `ProcessConfiguration` shall gain init properties for the stdin source and standard output/error targets; the invoker shall read pipes from the configuration.
- **Constraints**: Ships in v4, not v3 (user clarification, I009). No pipe type starts or owns a process (D003). Factory names follow the CliWrap taxonomy; exact property names deferred to the blueprint.

### [T005] - init conversion mechanics

- **Driver**: the user wants compile-time enforcement of the one essential member and validation on the type itself.
- **Resolved Answer**: "Option A — init + required target"
- **Normalized Requirement**: `ProcessConfiguration` knobs shall become init-only properties with `required` `TargetFilePath` (`[SetsRequiredMembers]` on the convenience ctor); init accessors shall validate (target null/empty; working-directory existence per D006); `ArgumentsList` shall merge into `ArgumentList` as init-only.
- **Constraints**: Rides v3 GA (D007). `RunnerConfigurationFactory` adapts to the merge. record conversion rejected for now. Exact validation exception types deferred to the blueprint.

### [I012] - ListenAsync semantics

- **Prompt**: "For T003 – ListenAsync semantics: pick an option, hybridize, or provide your own answer."
- **User Response**: "T003: Option A"
- **Resolution**: full pipeline flow with post-Exited validation and a CliRun facade; recorded as T003.
- **Notes**: recommendation accepted.

### [I013] - builder fate and escaping home

- **Prompt**: "For T006 – builder fate and escaping home: pick an option, hybridize, or provide your own answer."
- **User Response**: "T006: Option A"
- **Resolution**: builder kept as-is; escaping home settled; recorded as T006.
- **Notes**: recommendation accepted.

### [I014] - test scope

- **Prompt**: "For T007 – test scope: pick an option, hybridize, or provide your own answer."
- **User Response**: "T007: Option B"
- **Resolution**: migrate plus property tests; recorded as T007.
- **Notes**: matches the middleware session's T006 precedent.

### [T003] - ListenAsync semantics

- **Driver**: the user wants streaming to flow through the middleware pipeline so cross-cutting behavior covers streamed invocations.
- **Resolved Answer**: "Option A — Full pipeline flow"
- **Normalized Requirement**: `ListenAsync` shall flow through the middleware pipeline; `ValidationRules` shall evaluate after the Exited event (single validation path); a `CliRun.ListenAsync` static facade shall be added; cancellation/timeout machinery reuses `ProcessExitConfiguration`.
- **Constraints**: Ships in v4 (I009). The middleware contract must define streaming-mode behavior (blueprint detail). No Rx dependency.

### [T006] - builder fate and escaping home

- **Driver**: the user wants escaping and credential-spec flows preserved where no init equivalent exists.
- **Resolved Answer**: "Option A — Keep builder as-is"
- **Normalized Requirement**: `ProcessConfigurationBuilder` shall remain for argument escaping and `UserCredentialSpec`/resource-policy callback flows; init construction (T005) shall be the documented default; the builder's Set* methods shall delegate to the init properties where applicable.
- **Constraints**: Builder stays in the main package; DI registration unchanged. Docs must position init as the default and the builder as advanced.

### [T007] - test scope

- **Driver**: the user wants the strongest practical test evidence, matching the middleware session's precedent.
- **Resolved Answer**: "Option B — Migrate + property tests"
- **Normalized Requirement**: Factory/builder call sites shall migrate; new coverage shall include init validation, required enforcement, the ArgumentsList merge, ListenAsync pipeline flow, and pipe attachment; FsCheck property tests shall cover converted equality/snapshot semantics and pipe normalization.
- **Constraints**: v3-GA items (T005/T006) and v4 items (T002/T003/T004) may land in separate test passes matching their release timing. Property tests target new surfaces, not parsers.

### [I015] - output format

- **Prompt**: "For T008 – output format: pick an option, hybridize, or provide your own answer."
- **User Response**: "Option A"
- **Resolution**: Implementation Blueprint chosen; recorded as T008.
- **Notes**: mirrors the middleware session's T007 choice.

### [T008] - output format

- **Driver**: the user wants decisions and implementation detail in separate, scannable artifacts.
- **Resolved Answer**: "Option A — Implementation Blueprint"
- **Normalized Requirement**: The session shall produce a standalone blueprint at the repo root with a Scope Binding section linking the spec and the ledger, inline `filename#Dxxx/Txxx` citations for every technical statement, and a Ledger Reference section.
- **Constraints**: Filename confirmed in Step 7.1. Downstream consumer recorded in T009.
- **Cites**: D001, T001, T002, T003, T004, T005, T006, T007

### [I016] - filename + downstream consumer

- **Prompt**: "Filename — derived from the spec basename: `IMPLEMENTATION-CliInvoke_Improvement_Report.md` at the repo root. OK, or rename? / Part B — downstream consumer (T009): ticket consumer, issue tracker, or manual handoff?"
- **User Response**: "manual handoff"
- **Resolution**: manual handoff; filename confirmed by no objection; recorded as T009.
- **Notes**: mirrors the middleware session's manual handoff choice.

### [T009] - downstream consumer

- **Driver**: the user wants to take the artifacts and drive downstream work themselves.
- **Resolved Answer**: "Manual handoff" — blueprint filename `IMPLEMENTATION-CliInvoke_Improvement_Report.md` at the repo root confirmed by no objection
- **Normalized Requirement**: The blueprint shall be written to `IMPLEMENTATION-CliInvoke_Improvement_Report.md` at the repo root; no automated ticket or issue decomposition follows; the user drives downstream work.
- **Constraints**: The agent does not launch downstream workflows.
- **Cites**: T008

### [I017] - ticket decomposition scope and output

- **Prompt**: "Decomposing blueprint Part 1 only (v3 GA construction story) into tickets - (a) output target - GitHub Issues or local markdown files - (b) pull request grouping - one pull request or multiple - "
- **User Response**: "Local markdown files" - "One PR (Recommended)"
- **Resolution**: tickets publish as local markdown files; the set groups under a single v3 GA pull request; decomposition proceeds on blueprint Part 1 only.
- **Notes**: user instruction preceding the session - "Only decompose v3 changes. Ignore v4 changes." - excludes the v4 train (D002, D003, T002, T003, T004) from ticket coverage; those records surface as intentionally uncovered in the coverage matrix.

### [I018] - decomposition pattern and ticket set validation

- **Prompt**: "The proposal above is the full v3 GA decomposition - five tickets, one pull request, published as local markdown under tickets/ after your approval.

A few things to check:
Which tickets, if any, would you combine, split, or rescope?
Are there any spec requirements not yet covered by a ticket, or any ticket that doesn't trace back to a requirement?
Are there any tickets where the `Blocked by` chain or Independent/Collaborative classification feels off?"
- **User Response**: "agree with decomposition"
- **Resolution**: clear pass - the five-ticket Domain-pattern decomposition, dependency chain, and all-Independent classification approved as proposed; tickets proceed to generation and publishing as local markdown under tickets/.
- **Notes**: no combine, split, rescope, or coverage adjustments requested; one pull request grouping and local markdown target confirmed via I017.
