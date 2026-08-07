# Decision Ledger

The Decision Ledger is the durable record of every branch resolved during a
grilling session. It is a single markdown file that lives at
`docs/decisions/DECISIONS-<repo>-<feature>.md` and uses stable `Dxxx` IDs as
the cross-reference key for every downstream consumer (memos, tickets,
blueprints, specialized grilling sessions). When citing a record from outside
the ledger file, use the `filename#Dxxx` format
(e.g., `DECISIONS-repo-feature.md#D001`).

## Path derivation

- `<repo>` is the directory name of the working repository.
- `<feature>` is a short kebab-case slug of the topic being grilled
  (e.g., `tab-session-restore`, `pricing-pivot`, `retro-format`).

Examples:

- Working in `~/code/acme-store`, topic is "tab session restore" →
  `docs/decisions/DECISIONS-acme-store-tab-session-restore.md`.
- Working in `~/code/acme-store`, topic is "should we pivot to per-seat
  pricing" → `docs/decisions/DECISIONS-acme-store-pivot-per-seat.md`.

## Lazy creation

`docs/decisions/` is created only when the first `Dxxx` record is about to
be written. Do not create the directory during the initialization summary;
create it on the first real append.

## Real-time appending

Append a `Dxxx` record **immediately after the user resolves a branch**,
before opening the next branch. Do not batch the writes at session end —
real-time writes give both the user and the agent a persistent,
up-to-date record to reference in later branches, and they let the user
spot a missing or weakened entry at the next branch and correct it
before drift compounds.

### Sentinel comment for next append ID

Every ledger file ends with a single-line sentinel comment that encodes
the next available `Dxxx` ID:

```md
<!-- next-id: Dxxx -->
```

The agent reads this one line (via a targeted `read` or `grep`) to find
the next append point, instead of re-reading the entire ledger tail.
The sentinel update is **atomic with the record write** — the same
`edit` call that appends the new `Dxxx` record also bumps the sentinel
to `<!-- next-id: D<NEXT> -->`.

If the sentinel is missing or out of sync with the highest existing ID,
fall back to scanning the file for the highest existing `Dxxx` and
re-seeding the sentinel before the next append.

## Per-branch record template

```md
### [Dxxx] — <branch name>

- **Driver**: <the user's underlying principle or motivation>
- **Resolved Answer**: <verbatim user choice>
- **Normalized Requirement**: <concise, testable statement>
- **Constraints**: <negative requirements, edge cases, or defaults>
```

- `Dxxx` is a zero-padded sequence: `D001`, `D002`, `D003`, … The
  next available ID is read from the trailing `<!-- next-id: Dxxx -->`
  sentinel at the end of the ledger file (see
  [Sentinel comment for next append ID](#sentinel-comment-for-next-append-id)).
  Do not reuse IDs. If the sentinel is missing or out of sync, fall
  back to scanning the file for the highest existing `Dxxx` and
  re-seeding the sentinel before the next append.
- `Driver` captures the **why** — the user's underlying principle or
  motivation behind the decision. It is distinct from `Resolved Answer`
  (the **what**) and `Normalized Requirement` (the testable outcome).
  If the user states multiple motivations, record the primary one and
  note the rest in `Constraints`.
- `Resolved Answer` is the user's exact wording (or a close paraphrase the
  user has explicitly accepted). It is **not** the agent's summary.
- `Normalized Requirement` is a single concise, testable statement an
  implementer or verifier can act on. The "testable" bar is the same as
  a PRD acceptance criterion.
- `Constraints` are negative requirements, edge cases, or defaults the
  user named (e.g., "Do not collapse multiple tabs into one session",
  "All open tabs must survive restart"). If none, write `None.`.

## Goal record

The first record in the ledger (D001) is the **goal record**. It captures
the session's foundational goal as surfaced by the goal-discovery question.
The goal record uses the same template but with goal-specific content:

```md
### [D001] — session goal

- **Driver**: <the user's underlying motivation for the session>
- **Resolved Answer**: <the user's stated goal or goals>
- **Normalized Requirement**: <a testable statement of the session's purpose>
- **Constraints**: <any scope boundaries the user named>
```

If the user's goal changes mid-session, add a new goal record with a
fresh `Dxxx` ID and a `Supersedes: Dxxx` line in `Constraints` linking
to the prior goal record. Do not amend the prior goal record.

## Re-opens

If a branch is re-opened later in the session (because a new discovery
invalidates the earlier decision), do **not** amend the prior record.
Add a new record with a fresh `Dxxx` ID and a `Supersedes: Dxxx` line in
`Constraints`. The superseded record stays in the ledger for traceability.

Example — the original D007 picked Option 2; a later discovery forces a
revisit:

```md
### [D012] — where the branch lives

- **Driver**: the user wants precondition failures to be visible at the
  call site, not deferred to a later validation step.
- **Resolved Answer**: encode the precondition inside the constructor of
  the tab container.
- **Normalized Requirement**: `TabContainer` shall reject construction
  with `null` dependencies at the call site, raising
  `ArgumentNullException` synchronously.
- **Constraints**: `Supersedes: D007`. The check must be a hard
  precondition, not a post-condition validator. The exception type is
  fixed (no custom exception class).
```

## Soft cap

If a single Decision Ledger reaches **~ la 30 records**, consider closing it
and opening a new one for the next phase of the interview. The cap is a
trigger for reflection, not a hard limit; override with reasoning if the
interview genuinely needs more.

## Worked example — full ledger excerpt

```md
### [D001] — session goal

- **Driver**: the user wants to build a platform that correctly models
  the payment relationship between contacts and client organizations.
- **Resolved Answer**: "clarify the domain model for a freelancing
  platform where contacts message on behalf of client organizations."
- **Normalized Requirement**: The session shall produce a domain model
  that distinguishes contacts from client organizations and defines the
  payment flow.
- **Constraints**: `None.`

### [D002] — who hires whom

- **Driver**: the user wants the model to reflect real-world agency —
  the contact acts for an organization, not for themselves.
- **Resolved Answer**: "the contact is a person acting for a client
  organization; the client organization is the payer."
- **Normalized Requirement**: The platform shall distinguish between a
  `Contact` (the person messaging) and a `ClientOrganization` (the legal
  entity that invoices and pays).
- **Constraints**: Both terms must exist in the glossary
  (`docs/CONTEXT.md`) with the definitions recorded inline here.

### [D003] — how payments are routed

- **Driver**: the user wants the platform fee to be transparent and
  deducted before the freelancer receives funds.
- **Resolved Answer**: "client organization is the payer; freelancer is
  the payee; platform takes a percentage fee."
- **Normalized Requirement**: Payment flow shall route funds from
  `ClientOrganization` to `Freelancer` with a platform fee deducted
  before the freelancer payout.
- **Constraints**: `None.`
```

### [D001] — session goal

- **Driver**: the user wants to introduce middleware to CliInvoke while maintaining backward compatibility and avoiding complexity for the majority of users.
- **Resolved Answer**: "implement the Middleware system whilst preserving the recently introduced pipeline invocation system and enabling the usage of middleware without complicating CliInvoke for basic users."
- **Normalized Requirement**: The implementation shall provide a middleware pipeline for `ProcessInvoker` that wraps `ProcessInvocationPipeline` without altering the public API for non-middleware users.
- **Constraints**: Must preserve the `ProcessInvocationPipeline` logic from PR 397.

### [T001] — primary language

- **Driver**: the user wants to maintain consistency with the existing project target.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: The middleware system shall be implemented in C# targeting .NET 10.
- **Constraints**: None.
- **Cites**: D001

### [T002] — key dependencies

- **Driver**: the user wants to avoid unnecessary dependencies to ensure a lean core and prevent dependency conflicts for developers.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: The middleware core shall have zero external third-party dependencies, relying solely on standard .NET types and internal project references.
- **Constraints**: None.
- **Cites**: D001

### [T003] — project structure

- **Driver**: the user wants to ensure that the ability to define and implement middleware is available to users of the Core package, keeping abstractions separate from concrete implementation details.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: `IProcessMiddleware` and `ProcessInvocationContext` shall reside in `CliInvoke.Core`, while the pipeline orchestrator and concrete middleware implementations shall reside in `CliInvoke`.
- **Constraints**: None.
- **Cites**: D001

### [T004] — sub-project scope

- **Driver**: the user wants to maintain a lean core while providing common middleware utilities in Extensions and using middleware to handle platform-specific process orchestration in Specializations.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: `CliInvoke.Extensions` shall host common middleware implementations (e.g., Validation, Logging), and `CliInvoke.Specializations` shall utilize the middleware system to implement platform-specific process selection and orchestration.
- **Constraints**: None.
- **Cites**: D001, T003

### [T005] — basic vs. advanced user flow

- **Driver**: the user wants the `ProcessInvoker` constructor to be straightforward and not full of clutter.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: `ProcessInvoker` shall accept a `ProcessInvocationContext` via a secondary constructor (or factory method) to provide a straightforward entry point for basic users while allowing advanced users to inject a customized context with a fully configured middleware chain.
- **Constraints**: T003.
- **Cites**: D001

### [T006] — middleware ownership boundary between Core and main package

- **Driver**: the user wants Core-only consumers to be able to write and run middleware without depending on the main `CliInvoke` package.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: `CliInvoke.Core` shall define `IProcessMiddleware` and a Core-owned composition model (a `MiddlewareChain` or equivalent) so Core-only users can run middleware. `CliInvoke` shall provide a bridge that runs a Core chain and terminates with the existing `ProcessInvocationPipeline` as the leaf.
- **Constraints**: T002, T003.
- **Cites**: D001

### [T007] — middleware ordering and terminal-pipeline rule

- **Driver**: the spec says middleware composes around the pipeline as the terminal. The decision below formalizes how ordering is expressed at the call site and how the terminal is guaranteed to be last.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: middleware shall be registered by the caller via `.Use(...)` calls in execution order. The composition API shall append the existing `ProcessInvocationPipeline` as the implicit final step at build time, so the caller cannot forget to add it and cannot place middleware after the terminal.
- **Constraints**: T003, T006.
- **Cites**: D001

### [T008] — error and exception propagation through the chain

- **Driver**: middleware wraps the terminal, so the chain must define what happens when the terminal or an upstream middleware throws.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: the middleware chain shall not catch or transform exceptions raised by the terminal or upstream middleware. The chain shall rethrow as-is. Middleware that wishes to log, suppress, or transform exceptions shall do so explicitly within its own `InvokeAsync` body.
- **Constraints**: T002, T007.
- **Cites**: D001

### [T009] — async surface and cancellation

- **Driver**: `ProcessInvoker` is fully async and the spec mandates `CancellationToken` propagation. The decision below locks the middleware contract shape.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: `IProcessMiddleware` shall be defined as `Task InvokeAsync(ProcessInvocationContext context, Func<ProcessInvocationContext, CancellationToken, Task> next)` with the `CancellationToken` flowing through `ProcessInvocationContext`. The terminal is represented by the `next` delegate so middleware can short-circuit by not invoking it.
- **Constraints**: T002, T007, T008.
- **Cites**: D001

### [T010] — context object ownership and shape

- **Driver**: T006 puts the middleware contract in `CliInvoke.Core`, but the process-invocation state (`ProcessConfiguration`, `IExternalProcess`, `PipedProcessResult`) lives in `CliInvoke`. The decision below resolves the split.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: `CliInvoke.Core` shall define `MiddlewareContext` to carry middleware-only state (the `next` delegate, `CancellationToken`, scratch state). `CliInvoke` shall continue to own `ProcessInvocationContext` for process-invocation state. The chain shall expose both to middleware; middleware reads the one it needs.
- **Constraints**: T003, T006, T009.
- **Cites**: D001

### [T011] — middleware execution model

- **Driver**: middleware can be executed with nested awaits (each middleware awaits `next` before returning) or as a flat walk. The decision below picks the model.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: middleware shall be executed with nested awaits ("Russian doll" model). Each middleware shall `await next(context, token)`; the outermost middleware's `await` resumes only after the entire chain finishes. This supports `await using` for resources like `PipedProcessResult` and matches the ASP.NET-Core middleware pattern.
- **Constraints**: T008, T009, T010.
- **Cites**: D001

### [T012] — `PipedProcessResult` disposal semantics across the chain

- **Driver**: `PipedProcessResult` holds an `IExternalProcess` and must be disposed. The decision below defines who owns disposal and what the user receives.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: the terminal (`ProcessInvocationPipeline`) shall produce a `PipedProcessResult` and return it to the caller (the outermost middleware or the user) without disposing it. The user (or the outermost middleware) is responsible for disposing the result. This matches the `PipedProcessResult`/`IExternalProcess` lifecycle documented in `README.md §Resource Cleanup`.
- **Constraints**: T002, T003, T008, T011.
- **Cites**: D001

### [T013] — middleware package split

- **Driver**: T004 put common utilities in `CliInvoke.Extensions` and platform-specific orchestration in `CliInvoke.Specializations`. This decision formalizes the boundary, the dependency graph, and the default for basic users.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**:
  - `CliInvoke.Core` — `IProcessMiddleware`, `MiddlewareContext`, and the composition API. No implementations.
  - `CliInvoke` — `ProcessInvocationContext`, the default builder, and the integration with `ProcessInvoker`. A basic user who references `CliInvoke` gets middleware enabled by default.
  - `CliInvoke.Extensions` — common middleware (e.g., `ValidationMiddleware`, `LoggingMiddleware`).
  - `CliInvoke.Specializations` — middleware that wraps the terminal to select Cmd / PowerShell / etc.
- **Constraints**: T003, T004, T006, T010.
- **Cites**: D001

### [T014] — Specializations-as-middleware integration

- **Driver**: `CliInvoke.Specializations` exposes `PowershellProcessInvoker` and `CmdProcessInvoker` that today inherit from `ProcessInvoker`. T013 puts platform orchestration in `Specializations` *as middleware*. The decision below picks how that middleware interacts with the existing invokers.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: a platform middleware (e.g., `PowerShellMiddleware`, `CmdMiddleware`) shall sit near the top of the chain and rewrite `ProcessInvocationContext.Configuration` so its `FilePath` and `Arguments` target the platform binary (e.g., `pwsh` / `cmd.exe`) and wrap the original command. The terminal pipeline remains unchanged. This is a drop-in replacement for the existing `PowershellProcessInvoker`/`CmdProcessInvoker` invokers.
- **Constraints**: T003, T004, T010, T013.
- **Cites**: D001

### [T015] — `ProcessInvoker` configuration updates

- **Driver**: T005 picked the configure-and-inject flow. T010 picked the two-context model. The decision below specifies the actual constructor/build surface on `ProcessInvoker` and how the basic user (no middleware) still gets a clean entry point.
- **Resolved Answer**: "Option 2"
- **Normalized Requirement**: `ProcessInvoker` shall retain its existing primary constructor for basic users. It shall gain a second constructor that accepts an `IEnumerable<IProcessMiddleware>` so advanced users can register middleware at the call site. The basic constructor is unchanged and is the default for users who do not need middleware.
- **Constraints**: T003, T005, T010, T013.
- **Cites**: D001

### [T016] — context mutability and cross-middleware state

- **Driver**: middleware that needs to pass data to other middleware (e.g., a correlation ID, a log scope) needs somewhere to write. The decision below picks the mutability model.
- **Resolved Answer**: "Option 1-Typed"
- **Normalized Requirement**: `ProcessInvocationContext` and `MiddlewareContext` shall be read-only for middleware. Cross-middleware state shall be stored in `MiddlewareContext.Items`, which returns a `MiddlewareItems` helper backed internally by `IDictionary<string, object?>` and exposing typed `Get<T>(string key)` / `Set<T>(string key, T value)` extension methods. Call sites are type-safe on the value; the key is a string.
- **Constraints**: T010, T011.
- **Cites**: D001

### [T017] — packaging and discoverability of built-in middleware

- **Driver**: T013 put common middleware in `CliInvoke.Extensions` and platform middleware in `CliInvoke.Specializations`. The decision below picks how a user *finds* that middleware.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: built-in middleware shall be exposed as extension methods on `IProcessMiddlewareBuilder` (in `CliInvoke`) and on `ProcessInvoker` (in `CliInvoke.Specializations`), e.g., `UseValidation(builder)`, `UseLogging(builder)`, `UsePowerShell(invoker)`, `UseCmd(invoker)`. The middleware classes themselves shall be internal/friend; the public API surface is the extension method. This matches the ASP.NET-Core convention and keeps middleware implementations as implementation details.
- **Constraints**: T013, T014, T015.
- **Cites**: D001

### [T018] — testing strategy

- **Driver**: the middleware chain is a new public surface. Tests must cover ordering, short-circuit, exception propagation, `CancellationToken` flow, `PipedProcessResult` disposal, the `Items` dictionary, and the `IProcessMiddlewareBuilder.Use(...)` order rule.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: tests shall live under `tests/CliInvoke.Tests` using TUnit (per the repo's testing convention). The chain shall be covered by in-memory unit tests with fake middleware that assert ordering, short-circuit, exception propagation, cancellation, and `MiddlewareItems` behavior. End-to-end behavior (real process invocation, `PipedProcessResult` lifecycle) shall be covered by integration tests against real processes.
- **Constraints**: T008, T009, T011, T012, T016.
- **Cites**: D001

### [T019] — middleware registration in `ProcessInvoker` constructor

- **Driver**: T015 picked the second-constructor pattern. The decision below pins down the constructor signature, the order the middleware is composed in, and the validation rules on the parameter.
- **Resolved Answer**: "Option 1"
- **Normalized Requirement**: `ProcessInvoker` shall expose a second constructor `ProcessInvoker(ProcessConfiguration configuration, IEnumerable<IProcessMiddleware> middleware)`. Middleware shall be composed in the order they appear in the enumeration; the existing `ProcessInvocationPipeline` shall be appended as the implicit terminal at construction time. A null entry in the enumeration shall throw `ArgumentNullException` at construction time. The enumeration shall be materialized once into a list.
- **Constraints**: T007, T009, T010, T011, T012, T015, T016.
- **Cites**: D001

<!-- next-id: T020 -->
