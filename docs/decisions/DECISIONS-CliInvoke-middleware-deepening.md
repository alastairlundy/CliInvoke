# Decision Ledger — CliInvoke middleware deepening

Session: technical-grilling over architecture-review candidates 1 (truncation seam), 3 (unify validation), 4 (retry/validation separation).

### [D001] - session goal

- **Driver**: the user wants CliInvoke middleware deepened so the library is easier to reason about mentally.
- **Resolved Answer**: "Deepen CliInvoke Middleware in a way that makes CliInvoke easier to reason about mentally. Preserve existing functionality where warranted, and avoiding excessive breaking changes whilst deepening."
- **Normalized Requirement**: The session shall produce deepening decisions for the middleware architecture (truncation seam, validation unification, retry/validation separation) that reduce cognitive load, preserve existing functionality where warranted, and minimize breaking changes.
- **Constraints**: track: concept-then-implementation (session flows into an implementation plan). Preserve existing functionality where warranted. Avoid excessive breaking changes.

### [I001] - truncation concept

- **Prompt**: "For D002 – truncation concept: pick an option, hybridize, or provide your own answer."
- **User Response**: "D002: Option A"
- **Resolution**: resolved truncation as an invocation capability; recorded as D002.
- **Notes**: user probed whether Core rather than middleware is the right owner; agent reasoned parameter-vs-decoration and seam shape; user accepted Option A.

### [I002] - validation concept

- **Prompt**: "For D003 – validation concept: pick an option, hybridize, or provide your own answer."
- **User Response**: "D003: Option A"
- **Resolution**: resolved validation as part of the invocation contract; recorded as D003.
- **Notes**: same ownership probe as I001; user accepted Option A.

### [I003] - retry/validation separation

- **Prompt**: "For D004 – retry/validation separation: pick an option, hybridize, or provide your own answer."
- **User Response**: "D004: Option B - There are no stable v3 releases. A deprecation window would be pointless."
- **Resolution**: resolved retryability as a separate concept with a hard cut; recorded as D004.
- **Notes**: user corrected the agent's policy/predicate wording inconsistency (same destination, different transition strategy); the user's rationale — no stable v3 releases exist — drove the hard cut over the deprecation window.

### [D002] - truncation concept

- **Driver**: the user wants invocation meaning to be entrypoint-independent and the pipeline to depend only on configuration.
- **Resolved Answer**: "Option A — Invocation capability"
- **Normalized Requirement**: The truncation cap shall be stated on the invocation configuration and read by the pipeline from configuration; no middleware-to-pipeline string-key handoff shall exist for the cap; CliRun invocations honor the cap.
- **Constraints**: Additive Core surface only (ADR-0001 relocation path). Cap type shape and API placement deferred to Phase 2. Middleware may set the cap only as sugar over the configuration surface.

### [D003] - validation concept

- **Driver**: the user wants one validation path so double validation is impossible and CliRun behavior is preserved.
- **Resolved Answer**: "Option A — Invocation contract"
- **Normalized Requirement**: The pipeline shall be the single validation path for ProcessExitConfiguration.ValidationRules; PostExitValidation shall surface as sugar translating an IProcessResultValidator into ValidationRules; one invocation shall not evaluate both a middleware validator and pipeline rules on the same result.
- **Constraints**: CliRun validation behavior preserved. No new Core types required. Exception types and rule APIs out of scope for this branch.

### [D004] - retry/validation separation

- **Driver**: the user wants retryability owned by the retry middleware; with no stable v3 release in existence, a deprecation window would protect nothing.
- **Resolved Answer**: "Option B — Separate policy, hard cut" ("There are no stable v3 releases. A deprecation window would be pointless.")
- **Normalized Requirement**: RetryMiddleware shall own the retry decision through its own dedicated surface; ShouldRetry shall be removed from IProcessResultValidator in the same release; the validator interface shall express validation outcome only (Validate, GetValidationFailures).
- **Constraints**: Surface shape (delegate vs named type) deferred to Phase 2. Backoff strategy and options shape out of scope. The breaking change lands in the unreleased v3 line; no stable release line is broken.

### [D005] - glossary term: Invocation Capability

- **Driver**: the user wants the parameter-vs-decoration distinction crystallized by D002 and D003 named in the domain language.
- **Resolved Answer**: "Confirm Invocation capability term"
- **Normalized Requirement**: GLOSSARY.md shall define Invocation Capability as: a parameter of the invocation contract that the caller states for the invocation to mean what they intend (e.g., validation rules, truncation cap); distinct from a middleware concern (cross-cutting behavior composed around the invocation that the caller could omit without changing the invocation's meaning, e.g., logging, retry).
- **Constraints**: Definition must match the GLOSSARY.md entry exactly. Middleware concern is recorded as the contrast inside the definition, not as a separate term.

### [I005] - foundation items

- **Prompt**: "For T001 – foundation items: pick an option, hybridize, or provide your own answer."
- **User Response**: "T001: Option A"
- **Resolution**: foundation locked by repo state; recorded as T001.
- **Notes**: in-place deepening confirmed; no structural or dependency changes.

### [T001] - foundation items

- **Driver**: the user wants the deepening to be in-place refactoring with no structural churn.
- **Resolved Answer**: "Option A — Locked in place"
- **Normalized Requirement**: All foundation items are locked by repo state — C#, .NET 10, existing NuGet dependencies, the four shipping projects (Core, CliInvoke, Extensions, Specializations), existing test projects, and library project type; the deepening modifies existing projects only.
- **Constraints**: No new projects; no new packages; no language or framework changes.
- **Cites**: D001

### [I006] - truncation cap placement

- **Prompt**: "For T002 – truncation cap placement: pick an option, hybridize, or provide your own answer."
- **User Response**: "T002: Option A"
- **Resolution**: cap placed on ProcessExitConfiguration; recorded as T002.
- **Notes**: recon fact drove the cost cell — WithConfiguration only swaps ProcessConfiguration, so the sugar needs an additive WithExitConfiguration.

### [I007] - retry surface shape

- **Prompt**: "For T003 – retry surface shape: pick an option, hybridize, or provide your own answer."
- **User Response**: "T003: Option A"
- **Resolution**: IRetryPolicy placed in Core; recorded as T003.
- **Notes**: validator precedent (IProcessResultValidator in Core, middleware consumers in the main package) drove the placement.

### [I008] - validation sugar shape

- **Prompt**: "For T004 – validation sugar shape: pick an option, hybridize, or provide your own answer."
- **User Response**: "T004: Option A"
- **Resolution**: thin pre-next middleware chosen; recorded as T004.
- **Notes**: first-failure semantics chosen to preserve the default path's messages; the middleware path's composite messages change.

### [T002] - truncation cap placement

- **Driver**: the user wants the cap to live where completion policy already lives, so invocation bounds are reasoned about in one place.
- **Resolved Answer**: "Option A — Cap on exit configuration"
- **Normalized Requirement**: The truncation cap shall be a nullable long property on ProcessExitConfiguration; ProcessInvocationPipeline shall read it from ctx.ExitConfiguration; CliRun shall set it via its config construction; the MiddlewareItems string-key handoff shall be deleted.
- **Constraints**: Additive Core surface only. A small additive WithExitConfiguration on InvocationContext is authorized for the sugar. Per-stream caps stay out of scope until needed.
- **Cites**: D002, T001

### [T003] - retry surface shape

- **Driver**: the user wants implementable contracts to live in Core, following the validator precedent.
- **Resolved Answer**: "Option A — Named interface in Core"
- **Normalized Requirement**: A named IRetryPolicy interface shall be added to CliInvoke.Core; RetryMiddleware shall consume it for the retry decision; ShouldRetry shall be removed from IProcessResultValidator in the same release.
- **Constraints**: Exact method signature (incl. attempt context) deferred to the blueprint. Backoff strategy and RetryOptions shape unchanged.
- **Cites**: D004, T001

### [T004] - validation sugar shape

- **Driver**: the user wants one validation path with the Use* entrypoint preserved and the default path's error messages unchanged.
- **Resolved Answer**: "Option A — Thin pre-next middleware"
- **Normalized Requirement**: UsePostExitValidation shall remain the public entrypoint; its middleware shall merge the validator's ValidationRules into the invocation's exit configuration before next; the pipeline shall be the only evaluator; PostExitValidationMiddleware's post-next evaluation shall be deleted.
- **Constraints**: Error semantics unify on first-failure (middleware path's composite messages change). Rule APIs and exception types unchanged.
- **Cites**: D003, D004, T001

### [I009] - truncation sugar fate

- **Prompt**: "For T005 – truncation sugar fate: pick an option, hybridize, or provide your own answer."
- **User Response**: "T005: Option A"
- **Resolution**: UseOutputTruncation kept as thin sugar; recorded as T005.
- **Notes**: configuration path serves all callers; the sugar serves the DI global-cap-policy case.

### [I010] - test migration scope

- **Prompt**: "For T006 – test migration scope: pick an option, hybridize, or provide your own answer."
- **User Response**: "T006: Option C"
- **Resolution**: migrate plus property tests; recorded as T006.
- **Notes**: user chose stronger guarantees over the recommended A — FsCheck property tests added on top of the migrate-and-add-coverage scope.

### [T005] - truncation sugar fate

- **Driver**: the user wants the existing public API preserved where it still serves a real use case.
- **Resolved Answer**: "Option A — Keep as thin sugar"
- **Normalized Requirement**: UseOutputTruncation shall remain the public entrypoint; its middleware shall set the cap on the invocation's exit configuration before next (via WithExitConfiguration); the MiddlewareItems write shall be deleted.
- **Constraints**: TruncationOptions shape is a blueprint detail. The configuration path (cap on ProcessExitConfiguration) serves all callers without middleware.
- **Cites**: T002, D002, T001

### [T006] - test migration scope

- **Driver**: the user wants the deepening's claims enforced by the strongest practical test evidence.
- **Resolved Answer**: "Option C — Migrate plus property tests"
- **Normalized Requirement**: The retry tests that relied on the removed ShouldRetry default are migrated to IRetryClassifier — the shipped name for the ledger's IRetryPolicy placeholder — evidenced by the four classifier-driven decision tests in RetryMiddlewareTests.cs (InvokeAsync_RetriesUntilMaxAttempts_WhenAlwaysRetryable, InvokeAsync_DoesNotRetry_WhenNotRetryable, InvokeAsync_DoesNotRetry_WhenExitCodeZero, UseRetryPolicy_RegistersConfiguredInvoker) and the dispatch properties in RetryDispatchFuzzTests.cs. New coverage: the truncation handoff on all three paths (CliRun capped/uncapped/exit-configuration-cap, DI UseOutputTruncation middleware, and configuration-only IProcessInvoker) in OutputTruncationHandoffTests.cs; double-validation impossibility and single-path validation via UsePostExitValidation_EvaluatesEachRuleExactlyOnce_NoDoubleValidation and UsePostExitValidation_WithExitConfigRules_ThrowsOnceNamingFirstRule; and FsCheck property tests for the new surfaces in TruncationCapFuzzTests.cs, ValidationRuleMergeFuzzTests.cs, and RetryDispatchFuzzTests.cs.
- **Constraints**: Property tests target the new surfaces (cap plumbing, retry policy dispatch, rule merging), not parsers. Individual assertions are blueprint detail.
- **Cites**: T002, T003, T004, D001

### [I011] - output selection

- **Prompt**: "For T007 – output selection (Part A: format): pick an option, hybridize, or provide your own answer."
- **User Response**: "Option A" (format); "File name confirmed. Manual handoff" (filename + consumer)
- **Resolution**: Implementation Blueprint at IMPLEMENTATION-middleware-deepening.md; manual handoff; recorded as T007.
- **Notes**: user declined the recommended ticket-consumer path; the fresh-session suggestion was offered but manual handoff chosen.

### [T007] - output selection

- **Driver**: the user wants decisions and implementation detail in separate, scannable artifacts with no automated decomposition.
- **Resolved Answer**: "Option A — Implementation Blueprint; filename IMPLEMENTATION-middleware-deepening.md at repo root; downstream consumer: manual handoff"
- **Normalized Requirement**: The session shall produce a standalone blueprint at IMPLEMENTATION-middleware-deepening.md (repo root) with a Scope Binding section linking the Decision Ledger, inline filename#Dxxx/Txxx citations for every technical statement, and a Ledger Reference section; no automated ticket or issue decomposition follows.
- **Constraints**: The blueprint is a context pointer valid only for the linked ledger. Manual handoff — the user takes the artifacts; the agent does not launch downstream workflows.
- **Cites**: D001, T001, T002, T003, T004, T005, T006

### [I012] - ticket decomposition output target

- **Prompt**: "Where should the middleware-deepening tickets be published? Options - GitHub Issues (Recommended - the repo's tracker per AGENTS.md), local markdown files, GitLab Issues, Gitea Issues, Codeberg Issues, or a hosted Forgejo instance. Pull request count defaults to 1 PR for the whole ticket set unless stated otherwise."
- **User Response**: "Local markdown files"
- **Resolution**: tickets will be published as local markdown files in a tickets/ directory at the repo root; the 1 PR default was stated and not objected to, so the set is planned as a single pull request.
- **Notes**: no tickets/ directory exists yet (verified); it will be created at publish time. Blocked-by fields use file basenames for this target.

### [I013] - decomposition proposal validation

- **Prompt**: "Decomposition proposal validation - (1) Which tickets, if any, would you combine, split, or rescope? (2) Are there any spec requirements not yet covered by a ticket, or any ticket that doesn't trace back to a requirement? (3) Are there any tickets where the Blocked by chain or Independent/Collaborative classification feels off?"
- **User Response**: TBD
- **Resolution**: TBD
- **Notes**: TBD

<!-- next-d: D006 -->
<!-- next-t: T008 -->
<!-- next-i: I014 -->
