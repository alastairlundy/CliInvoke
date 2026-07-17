# CliInvoke — Process Invocation Pipeline Decisions

This ledger records the design decisions for the F1 deepening: collapsing the
"execute a process" skeleton currently duplicated 12 times across four
invoker modules. Each `Dxxx` entry is stable and cross-cited as
`DECISIONS-CliInvoke-process-invocation-pipeline.md#Dxxx`.

Context: the F1 candidate is documented in
`docs/agents/architecture-review-20260711-213732.html` (or the most recent
review in `%TEMP%/architecture-review-*.html`).

Domain vocabulary: see `CONTEXT.md` at the repo root — *Resource-Owning
Type*, *Process Invocation Pipeline*, *Process Invocation Context*.

## Records

### [D001] — where the pipeline module sits

- **Resolved Answer**: "Option 2 — Pipeline is internal; IProcessInvoker stays public."
- **Normalized Requirement**: A `ProcessInvocationPipeline` module shall own the 5-line execution skeleton (factory → start → wait/capture → dispose); the four invoker modules (`ProcessInvoker`, `CliRun`, `CmdProcessInvoker`, `PowershellProcessInvoker`) shall each become a 2-line wrapper that delegates to the pipeline; `IProcessInvoker` shall remain the public DI-friendly entry point with its current 3-method shape unchanged.
- **Constraints**: No breaking change to the public `IProcessInvoker` contract. The duplication of 12 near-identical bodies across the four invoker modules shall collapse into one pipeline module. If a future capture mode is added (e.g. `FireAndForget`, streaming), the public `IProcessInvoker` shape may need to grow — this is the documented forward risk of keeping the public shape.

### [D002] — what shape the pipeline's input takes

- **Resolved Answer**: "Option 1 — Single `ProcessInvocationContext` input."
- **Normalized Requirement**: The pipeline shall accept a single `ProcessInvocationContext` parameter that holds the effective `ProcessConfiguration`, `ProcessExitConfiguration`, and capture mode; the pipeline mutates the context as state flows through, and the populated result is read from the same context after the call.
- **Constraints**: A `ProcessInvocationContext` type shall be introduced in `CliInvoke.Core`; until the pipeline gains a mutating interceptor stage, the context is a thin wrapper around three existing parameters, which is acceptable because the type matches the glossary term.
