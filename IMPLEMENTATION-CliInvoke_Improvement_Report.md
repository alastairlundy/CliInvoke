# Implementation Blueprint — CliInvoke v4 improvements

## Scope Binding

- **Linked Spec:** `CliInvoke_Improvement_Report.md`
- **Decision Ledger:** `docs/decisions/DECISIONS-CliInvoke-v4-improvements.md`
- **Notice:** This blueprint is a context pointer valid ONLY for the linked spec and ledger. It must not be applied to other specifications without explicit authorization.

## Release scoping

The ledger resolves the plan into two release trains:

- **v3 GA (current `3.0.0` line)** — the construction story: init conversion, factory removal, builder positioning. Records: `D005`, `D006`, `D007`, `T005`, `T006`. **Completed.**
- **v4 (post-GA)** — the capability surfaces: event stream and pipes. Records: `D002`, `D003`, `T002`, `T003`, `T004`.
- **Explicitly not in v3:** `ListenAsync`, `ProcessEvent`, `PipeSource`, `PipeTarget` (user clarification, `DECISIONS-CliInvoke-v4-improvements.md#I009`).
- **Dropped from the plan:** R10 (`UserCredential`/`SecureString` rework) — `DECISIONS-CliInvoke-v4-improvements.md#D004`.

---

## Part 1 — v3 GA: construction story ✅ Completed

### 1.1 `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Convert get-only properties to `init`: `WorkingDirectoryPath`, `Arguments`, `ArgumentList`, `WindowCreation`, `EnvironmentVariables`, `Credential`, `UseShellExecution`, `StandardInput`, `RedirectStandardInput`, `OutputRedirection`, `ResourcePolicy`, and the three encodings, plus `RequiresAdministrator` [`DECISIONS-CliInvoke-v4-improvements.md#T005`].
- Mark `TargetFilePath` `required` (already `init`) [`DECISIONS-CliInvoke-v4-improvements.md#T005`]; annotate the convenience constructor with `[SetsRequiredMembers]` [`DECISIONS-CliInvoke-v4-improvements.md#T005`].
- Remove the `protected internal` 16-parameter constructor; its defaults move to property initializers (`UserCredential.Null`, `ProcessResourcePolicy.Default`, empty `ImmutableSortedDictionary`, `Encoding.Default`, `StreamWriter.Null`) [`DECISIONS-CliInvoke-v4-improvements.md#T005`].
- The `EnvironmentVariables` init accessor coerces any supplied dictionary into the ordinal-sorted `ImmutableSortedDictionary`, preserving the documented snapshot/equality semantics [`DECISIONS-CliInvoke-v4-improvements.md#T005`].
- Init-accessor validation: `TargetFilePath` null/empty throws (parity with the current constructor); `WorkingDirectoryPath` runs the `Directory.Exists` check and throws `DirectoryNotFoundException` on miss — factory-parity semantics [`DECISIONS-CliInvoke-v4-improvements.md#D006`].
- Merge `ArgumentsList` into `ArgumentList` as init-only; delete the mutable `ArgumentsList` property; equality/hash keep `ArgumentList` sequence semantics [`DECISIONS-CliInvoke-v4-improvements.md#T005`].

### 1.2 `src/CliInvoke/Extensions/ProcessConfigurationFactory.cs`

- ~~Delete the file (both `Create` overloads)~~ [`DECISIONS-CliInvoke-v4-improvements.md#D006`, `DECISIONS-CliInvoke-v4-improvements.md#D007`]. **Done** — file removed.

### 1.3 `src/CliInvoke/CliRun.cs`

- ~~`BuildStringArgsConfig` (line 190) and `FireAndForget(string)` (line 175) construct `ProcessConfiguration` directly via init instead of the factory~~ [`DECISIONS-CliInvoke-v4-improvements.md#D006`]. **Done** — both paths use init construction.

### 1.4 `src/CliInvoke/ShellDetector.cs`

- ~~The four construction sites (lines 69, 78, 119, 141) construct directly~~ [`DECISIONS-CliInvoke-v4-improvements.md#D006`]. **Done**.

### 1.5 `src/CliInvoke/Extensibility/RunnerConfigurationFactory.cs`

- ~~Adapt `ArgumentsList` writes to `ArgumentList`~~ [`DECISIONS-CliInvoke-v4-improvements.md#T005`]. **Done**.

### 1.6 `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` + `src/CliInvoke.Core/Builders/IProcessConfigurationBuilder.cs`

- ~~Keep the builder for argument escaping and `UserCredentialSpec`/resource-policy callback flows; init construction becomes the documented default~~ [`DECISIONS-CliInvoke-v4-improvements.md#T006`]. **Done** — builder remains for advanced scenarios.
- ~~`Set*` methods delegate to the init properties where applicable; `Build()` semantics unchanged; DI registration in Specializations unchanged~~ [`DECISIONS-CliInvoke-v4-improvements.md#T006`]. **Done**.

### 1.7 Docs

- ~~`DESIGN_PATTERNS.md` / README: position init construction as the default and the builder as the advanced path (escaping, credential specs)~~ [`DECISIONS-CliInvoke-v4-improvements.md#T006`]. **Done**.

### 1.8 Tests — v3 pass

- ~~Migrate the ~20 `ProcessConfigurationFactory.Create` call sites (CliRun paths, invoker tests, middleware integration tests, Specializations tests, AOT/trimming programs)~~ [`DECISIONS-CliInvoke-v4-improvements.md#T007`, `DECISIONS-CliInvoke-v4-improvements.md#D006`]. **Done**.
- ~~New coverage: init validation (target, working directory), `required` enforcement, `ArgumentsList` merge~~ [`DECISIONS-CliInvoke-v4-improvements.md#T007`]. **Done**.
- ~~FsCheck property tests: equality/hash stability across environment-variable insertion order; snapshot isolation from caller dictionaries~~ [`DECISIONS-CliInvoke-v4-improvements.md#T007`]. **Done**.

---

## Part 2 — v4: capability surfaces (not yet implemented; semantics resolved — `DECISIONS-CliInvoke-v4-improvements.md#D008`–`#D015`)

### 2.1 `src/CliInvoke.Core/IProcessInvoker.cs`

- Add `IAsyncEnumerable<ProcessEvent> ListenAsync(ProcessConfiguration, ProcessExitConfiguration?, CancellationToken)` as a member [`DECISIONS-CliInvoke-v4-improvements.md#T002`].

### 2.2 `src/CliInvoke.Core/Events/` (new)

- `ProcessEvent` abstract record carrying `ProcessId`; `StartedProcessEvent`, `StandardOutputProcessEvent`, `StandardErrorProcessEvent`, `ExitedProcessEvent` (`ExitCode`, `PosixSignal?`, `Canceled`) — reusing the existing result vocabulary [`DECISIONS-CliInvoke-v4-improvements.md#T002`, `DECISIONS-CliInvoke-v4-improvements.md#D002`].

### 2.3 `src/CliInvoke.Core/Pipes/` (new)

- `PipeSource` factories: `Null`, `FromStream`, `FromFile`, `FromBytes`, `FromString`, `FromDelegate` [`DECISIONS-CliInvoke-v4-improvements.md#T004`].
- `PipeTarget` factories: `Null`, `ToStream`, `ToFile`, `ToStringBuilder`, `ToDelegate`, `Merge` [`DECISIONS-CliInvoke-v4-improvements.md#T004`].
- Each variant is a **public `sealed record`** with static factory conveniences on the abstract base; constructors are inaccessible so the variant set stays closed and external derivation is impossible. Variant payloads (e.g., `FileInfo`) are frozen API commitments — document each in `site/docs` [`DECISIONS-CliInvoke-v4-improvements.md#D012`].
- Using `sealed record` variants (net10.0 / C# 14) rather than C# 15 `union` types; migrate to `union`/`closed` hierarchies at the first net11 TFM bump — a follow-up decision, not part of v4 [`DECISIONS-CliInvoke-v4-improvements.md#D012`, `DECISIONS-CliInvoke-v4-improvements.md#T001`].
- `PipeTarget.Merge` requires **≥ 2 targets**; construction throws for fewer — no degenerate wrappers exist. Duplicate handling and multi-target Merge equality/hash semantics are blueprint details for the record variants [`DECISIONS-CliInvoke-v4-improvements.md#D013`].
- No pipe type starts or owns a process [`DECISIONS-CliInvoke-v4-improvements.md#D003`].

### 2.4 `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs` (v4 additive)

- **stdin is superseded**: the new `PipeSource` stdin init property replaces `StandardInput: StreamWriter?` and `ProcessConfigurationBuilder.SetStandardInputPipe(StreamWriter)`. Those two members become `[Obsolete]` in the v3 line **post-GA** (a future 3.x release — the v3 GA construction story itself is untouched) and are **removed in v4**. Migrators use `PipeSource.FromStream` (e.g., `StreamWriter.BaseStream`). See `docs/adr/0004-standard-input-supersede.md` [`DECISIONS-CliInvoke-v4-improvements.md#D009`].
- Init properties for the stdin `PipeSource` and the standard output/error `PipeTarget`s; equality gains pipe-reference fields anchored on the base instance (reference equality, matching the `StandardInput` precedent — variant records carry value equality but the config anchor is the base reference) [`DECISIONS-CliInvoke-v4-improvements.md#T004`, `DECISIONS-CliInvoke-v4-improvements.md#D012`].
- **Contradiction throws at init**: attaching a non-Null `PipeTarget` while `OutputRedirection` is false throws. The flag retains meaning only for the buffered/no-redirection cases when no target is attached [`DECISIONS-CliInvoke-v4-improvements.md#D010`].

### 2.5 `src/CliInvoke/ProcessInvocationPipeline.cs`

- `ListenAsync` implementation: a streaming invocation mode; events produced as they happen; `ValidationRules` evaluate after the `Exited` event; cancellation/timeout reuses `ProcessExitConfiguration` machinery [`DECISIONS-CliInvoke-v4-improvements.md#T003`].
- **Always tee**: every pipeline event flows to both the attached `PipeTarget`s and the caller's enumeration; user targets behave as one merge branch in the same event set (internal machinery, distinct from the public `Merge` contract). Pull pacing is preserved — event production paces to consumption, and slow event consumption must not corrupt target delivery (make this a testable tee back-pressure assertion) [`DECISIONS-CliInvoke-v4-improvements.md#D011`].
- **Lifecycle source**: `StartedProcessEvent`/`ExitedProcessEvent` are synthesized by subscribing to the `ExternalProcess` `Started`/`Exited` event handlers — the one source of truth; no independent pipeline wiring. `ProcessWrapper`'s `HasStarted`/`HasExited` guard semantics are unchanged, but the pipeline is a second subscriber [`DECISIONS-CliInvoke-v4-improvements.md#D015`].

### 2.6 `src/CliInvoke/CliRun.cs`

- `CliRun.ListenAsync` static facade — execution only; no configuration surface on `CliRun` [`DECISIONS-CliInvoke-v4-improvements.md#T003`, `DECISIONS-CliInvoke-v4-improvements.md#I004`].

### 2.7 Middleware contract

- Streaming mode is uniform: middleware wraps the tee'd stream the same way regardless of whether pipe targets are attached — no conditional event semantics [`DECISIONS-CliInvoke-v4-improvements.md#D011`].
- Middleware may pattern-match attached pipe kinds (e.g., branch on `PipeTarget ToFile` vs `ToStream`) — the reason variants are public sealed records rather than hidden impls [`DECISIONS-CliInvoke-v4-improvements.md#D012`].
- Bypass stays orthogonal: `IExternalProcess` gains no streaming member; the event-authority question was explored and declined (streaming was considered as a property of the process handle and rejected — it would widen the bypass pattern and re-open `D003`-style ownership questions) [`DECISIONS-CliInvoke-v4-improvements.md#D015`, `DECISIONS-CliInvoke-v4-improvements.md#D003`].

### 2.8 Tests — v4 pass

- `ListenAsync` pipeline flow (teed event/target delivery with slow-consumer back-pressure safety, post-`Exited` validation), pipe attachment, init validation (`D010` contradiction throws), `Merge` ≥ 2 construction, and property tests over the record-variant equality surface [`DECISIONS-CliInvoke-v4-improvements.md#T007`, `DECISIONS-CliInvoke-v4-improvements.md#D011`, `DECISIONS-CliInvoke-v4-improvements.md#D013`].
- Obsolescence assertion: the superseded `StandardInput`/`SetStandardInputPipe` members carry `[Obsolete]` with a migration message pointing at `PipeSource.FromStream` (compile-checked, CI-enforceable) [`DECISIONS-CliInvoke-v4-improvements.md#D009`].

---

## Ledger Reference

`DECISIONS-CliInvoke-v4-improvements.md`: D001 (goal), D002 (pull event stream), D003 (data redirection + Merge), D004 (keep SecureString; R10 dropped), D005 (config as middle tier), D006 (factory removed), D007 (ride v3 GA), D008 (v4 semantics session goal), D009 (stdin superseded by PipeSource; obsolete in v3.x post-GA, removed v4 — `docs/adr/0004`), D010 (redirection flag contradiction throws), D011 (always-tee event/target flow), D012 (public sealed record variants; unions at net11 bump), D013 (Merge requires ≥ 2), D014 (v4 stabilising pre-release train), D015 (lifecycle events sourced from ExternalProcess handlers), T001 (foundation locked), T002 (Core invoker member), T003 (full pipeline flow), T004 (Core pipe types, config attachment), T005 (init + required target), T006 (builder kept), T007 (migrate + property tests), T008 (blueprint output), T009 (manual handoff).
