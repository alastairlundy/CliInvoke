# Decision Ledger — CliInvoke Process-flaw mitigations

Session: technical-grilling over the five minor code changes identified in the "Everything wrong with the Process class" doc-update handoff (`C:\Users\alast\AppData\Local\Temp\opencode\handoff-process-flaws-doc.md`).

### [D001] - session goal

- **Driver**: the user wants each identified Process-flaw mitigation pressure-tested and resolved into an implement (or don't) decision.
- **Resolved Answer**: "Decide which to implement + how" — resolve each of the five handoff items into implementable decisions (behavior, API surface, defaults).
- **Normalized Requirement**: The session shall resolve the five handoff code-change candidates — start-failure exception taxonomy, encoding default, working-directory semantics, argument-length validation, kill-tree completion — into decision records an implementer can act on without re-asking the user.
- **Constraints**: track: concept-then-implementation. All five items confirmed open (item 5 not locked by v4-ledger D006/T005: validation ≠ normalization). Doc follow-ups (fact-check pass, more examples) out of scope unless a branch surfaces one. Code-reading corrections recorded: handoff item 1 already mostly implemented in `ProcessWrapper.Start()`; handoff item 2 partially moot on net10.0 (`Encoding.Default` is UTF-8). Ledger path confirmed by proposal with no objection: `docs/decisions/DECISIONS-CliInvoke-process-flaw-mitigations.md`.

### [T001] - start-failure exception taxonomy

- **Driver**: the user wants start failures to report the real cause without adding API surface mid-beta.
- **Resolved Answer**: "Option A — Map known codes"
- **Normalized Requirement**: `ProcessWrapper.Start()` shall map additional known native error codes to their natural .NET exception types (e.g. 5 → `UnauthorizedAccessException`, 193 → bad-image-format) and keep a descriptive fallback carrying the file path for unknown codes.
- **Constraints**: No new public exception type. Existing FileNotFoundException mapping for codes 2/3 unchanged. Cancellation-path exception handling out of scope.
- **Cites**: D001

### [T002] - encoding default

- **Driver**: the user wants the default to stay consistent with what the .NET team itself would set; `Encoding.Default` is the conventional choice and its value must simply be documented.
- **Resolved Answer**: "Option B — Keep `Encoding.Default`"
- **Normalized Requirement**: The three encoding properties on `ProcessConfiguration` shall keep `Encoding.Default` as their default; the default's value (UTF-8 on net10.0) shall be documented rather than changed.
- **Constraints**: Zero code change to defaults; documentation must state the net10.0 value of `Encoding.Default`. Nullable "OS decides" encoding rejected.
- **Cites**: D001

### [T003] - working-directory semantics

- **Driver**: the user wants no behavior change where .NET's inherit behavior is already consistent cross-runtime.
- **Resolved Answer**: "Option A — Document inherit semantics"
- **Normalized Requirement**: An empty `WorkingDirectoryPath` shall keep meaning "child inherits the caller's current directory"; the semantics shall be documented and the doc's normalization claim corrected rather than shipping normalization code.
- **Constraints**: No normalization at adapter or construction time; D006/T005 (v4 ledger) `Directory.Exists` validation on non-empty values unchanged.
- **Cites**: D001

### [T004] - argument-length validation

- **Driver**: the user wants mitigations that give users actionable errors rather than cryptic OS failures.
- **Resolved Answer**: "Option A — Windows-gated pre-start check"
- **Normalized Requirement**: On Windows, the total command-line length shall be checked before process start and an `ArgumentException` with the measured length thrown past the ~32,767-character `CreateProcess` limit.
- **Constraints**: Windows-only code path; the limit is approximate. T001's code mapping remains as a backstop for over-limit cases that slip through. Per-argument escaping stays in the builder (v4 ledger T006).
- **Cites**: D001, T001

### [T005] - kill-tree completion

- **Driver**: the user wants no behavior change where a delay cannot actually guarantee closure.
- **Resolved Answer**: "Option A — Document best-effort"
- **Normalized Requirement**: Tree-kill shall remain best-effort with no post-kill delay or descendant re-kill; the residual race (descendants spawned mid-kill may survive) shall be documented, matching .NET's own `Kill(entireProcessTree: true)` semantics.
- **Constraints**: No added latency on forceful-exit and timeout paths; graceful-interrupt path out of scope.
- **Cites**: D001

### [T006] - output format

- **Driver**: the user wants decisions and implementation detail in separate, scannable artifacts.
- **Resolved Answer**: "Option A — Implementation Blueprint"
- **Normalized Requirement**: The session shall produce a standalone blueprint at the repo root with a Scope Binding section linking the spec and the ledger, inline `filename#Dxxx/Txxx` citations for every technical statement, and a Ledger Reference section.
- **Constraints**: Filename confirmed in Step 7.1. Downstream consumer recorded in T007.
- **Cites**: D001, T001, T002, T003, T004, T005

### [T007] - downstream consumer

- **Driver**: the user wants to take the artifacts and drive downstream work themselves.
- **Resolved Answer**: "Manual handoff" — blueprint filename `IMPLEMENTATION-handoff-process-flaws-doc.md` at the repo root confirmed by no objection
- **Normalized Requirement**: The blueprint shall be written to `IMPLEMENTATION-handoff-process-flaws-doc.md` at the repo root; no automated ticket or issue decomposition follows; the user drives downstream work.
- **Constraints**: The agent does not launch downstream workflows.
- **Cites**: T006

<!-- next-t: T008 -->

### [I001] - ticket output target

- **Prompt**: Where should the tickets be published - GitHub Issues (the repo's issue tracker per AGENTS.md), or local markdown files under `tickets/`?
- **User Response**: "Local markdown (tickets/)" - local markdown files under `tickets/`, not GitHub Issues.
- **Resolution**: drove the local-markdown publish branch of Step 9; `blocked_by` fields will use file basenames at publish time; the conversation-context parent rule and the Step 9 writing rules from `references/publishing-rules.md` apply.
- **Notes**: The user also confirmed implicitly that spec-driven work should not be pushed to the issue tracker for this decomposition - downstream use stays local-driven per T007.

<!-- next-t: T008 -->

### [I002] - T006/T007 ticket coverage gap

- **Prompt**: The coverage matrix cannot cite T006 (blueprint output format) or T007 (downstream consumer) to any ticket - they are session-output meta-decisions, not implementation work. T006 is satisfied by the blueprint artifact already existing; T007's "no automated ticket or issue decomposition follows" has been superseded by you explicitly requesting this decomposition. Do you accept marking T006/T007 as out of implementation scope in the coverage matrix, or do you want tickets covering them?
- **User Response**: "Agree with decomposition" - a clear pass on the closing questions, implicitly accepting the proposed matrix including the out-of-implementation-scope marking for T006/T007.
- **Resolution**: closed the two coverage gaps without new tickets; T006 and T007 are recorded as out of implementation scope in the coverage matrix (T006 satisfied by the existing blueprint artifact; T007 superseded by the user's explicit request for this decomposition).
- **Notes**: The user also passed on the combine/split/rescope question, keeping the five-ticket split; the two `blocked by` edges (TK002←TK001, TK004←TK003) were left as proposed.

<!-- next-i: I003 -->
