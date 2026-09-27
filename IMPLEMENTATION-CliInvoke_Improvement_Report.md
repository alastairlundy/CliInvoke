# Implementation Blueprint — CliInvoke v4 improvements

## Scope Binding

- **Linked Spec:** `CliInvoke_Improvement_Report.md`
- **Decision Ledger:** `docs/decisions/DECISIONS-CliInvoke-v4-improvements.md` (v4 surfaces) and `docs/decisions/DECISIONS-CliInvoke-dotnet11.md` (.NET 11 TFM/C# 15 enablement)
- **Notice:** This blueprint is a context pointer valid ONLY for the linked spec and ledgers. It must not be applied to other specifications without explicit authorization.

## Release scoping

The ledger resolves the plan into two release trains:

- **v3 GA (current `3.0.0` line)** — the construction story: init conversion, factory removal, builder positioning. Records: `D005`, `D006`, `D007`, `T005`, `T006`. **Completed.**
- **v4 (post-GA)** — the capability surfaces: event stream and pipes. Records: `D002`, `D003`, `T002`, `T003`, `T004`.
- **v3.2 prerelease (RC window, pre-GA)** — the only pre-GA vehicle carrying `net10.0;net11.0` assets, published from a work branch; v3.1 stays net10-only and unblocked. Records: `DECISIONS-CliInvoke-dotnet11.md#D003`, `#D004`, `#T001`.
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
- Uses public `sealed record` variants (C# 14) rather than C# 15 `union` types [`DECISIONS-CliInvoke-v4-improvements.md#D012`]. The former "migrate at the first net11 TFM bump" clause is **removed**: union types and closed hierarchies remain preview-gated (`LangVersion=preview`) even on the net11.0 TFM, so no migration is planned [`DECISIONS-CliInvoke-dotnet11.md#D005`]. Re-open only if a future C# release un-gates them.
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

## Part 3 — .NET 11 enablement (RC-window prerelease, then post-GA merge)

.NET 11 facts this section builds on: RC 1 as of Sept 2026, GA expected Nov 2026 (STS — 24-month support; only .NET 10 is LTS); the `net11.0` TFM requires the .NET 11 SDK (`global.json` `rollForward` cannot cross the 10→11 major boundary); C# 15 is the `net11.0` default; union types / closed hierarchies remain preview-gated.

### 3.1 Target frameworks

- `src/CliInvoke.Core`, `src/CliInvoke`, `src/CliInvoke.Specializations` csprojs move from `net10.0` to `net10.0;net11.0` [`DECISIONS-CliInvoke-dotnet11.md#D002`].
- Test projects dual-target `net10.0;net11.0` too [`DECISIONS-CliInvoke-dotnet11.md#T002`].
- `IsAotCompatible`, `IsTrimmable`, and PolyEnsure behavior must hold on both legs; per-leg parity failures block the merge.

### 3.2 Language

- Remove the explicit `LangVersion 14` pins; the net10.0 leg compiles as C# 14, the net11.0 leg as C# 15 (stable features only) via per-TFM MSBuild defaults [`DECISIONS-CliInvoke-dotnet11.md#D006`].
- No preview-gated language surface (`LangVersion=preview`) anywhere in `src/` or `tests/` — CI-checkable [`DECISIONS-CliInvoke-dotnet11.md#D006`].
- New Process APIs do NOT require C# 15 — adoption and language level are independent axes (verified; `DECISIONS-CliInvoke-dotnet11.md#I002`).

### 3.3 Toolchain and CI

- At the merge (post-GA), `global.json` pins the `11.0.1xx` band, `rollForward: latestFeature`; CI installs .NET 10.x + .NET 11.x SDKs; contributors build with the .NET 11 SDK alone — the 10 SDK cannot build net11.0 [`DECISIONS-CliInvoke-dotnet11.md#T001`].
- During the RC window: `main`'s stable-band pin is untouched; a work branch builds both legs with the .NET 11 RC SDK, CI-validates both legs [`DECISIONS-CliInvoke-dotnet11.md#T001`], and publishes `3.2.0-*` prereleases for early adopters — stable train version numbers are not touched (v3.1 remains net10-only and unblocked) [`DECISIONS-CliInvoke-dotnet11.md#D004`, `DECISIONS-CliInvoke-dotnet11.md#D003`].
- ConfigureAwait guard and XML-doc-as-error gates run on both legs [`DECISIONS-CliInvoke-dotnet11.md#D006`].

### 3.4 Release trains

- Stable v3.x publishes stay net10.0-only until .NET 11 GA [`DECISIONS-CliInvoke-dotnet11.md#D003`]; the `3.2.0-*` prerelease is the only pre-GA vehicle carrying `net10.0;net11.0` assets, published from the work branch during the RC window (D004 supersedes the read-only scope of D003 for prerelease publishes) [`DECISIONS-CliInvoke-dotnet11.md#D004`].
- The 4.0.0 (v4) train dual-targets from alpha.1; net11.0 assets publish on the v4 train only post-GA [`DECISIONS-CliInvoke-dotnet11.md#D009`, `DECISIONS-CliInvoke-v4-improvements.md#D014`].

### 3.5 Process API adoption (net11.0 leg only, `#if NET11_0`)

- Posture: bounded per-pain-point adoption; the pipeline-core launch/wait plumbing is NOT rewritten; no blanket decline [`DECISIONS-CliInvoke-dotnet11.md#D007`].
- **Adopted** — all inside the adapter layer / `ProcessWrapper` (the per-OS/per-leg seam [`DECISIONS-CliInvoke-dotnet11.md#T003`]):
  - Suspend/resume flow: start suspended, apply `ProcessResourcePolicy` via the control adapter, then resume always — including when `SetResourcePolicy` throws — using `ProcessStartInfo.StartSuspended` + `SafeProcessHandle.Resume()` [`DECISIONS-CliInvoke-dotnet11.md#D008`]. Verified call sites: `ProcessWrapper.cs:60–106` (application and always-resume guard), `SuspendProcess`/`ResumeProcess` at `:284`/`:306` per-OS adapters.
  - `Process.TryGetProcessById`, `Process.Signal(PosixSignal)`, `ProcessExitStatus` introspection, and handle-based kill/signal/waits where adapters currently P/Invoke (`GetTerminatingSignal` at `:106`, `SendInterruptSignalAsync` at `:814`).
- **Formally declined** (ledger-recorded, not user-facing docs — T004 scope): `Process.RunAndCaptureTextAsync`/`Run*`, `ReadAll*`, `Process.StartAndForget`, `ProcessStartInfo.StartDetached` (compete-with-self — CliRun/Buffered own that surface), `ProcessStartInfo.InheritedHandles` and Std-handle pre-spawning (default out of scope) [`DECISIONS-CliInvoke-dotnet11.md#T003`].
- **No new public APIs** on `ExternalProcess` or `IExternalProcess` — suspend/resume stays private [`DECISIONS-CliInvoke-dotnet11.md#D008`]. Each adopted site's behavior is identical on net10.0 (shared-source path unchanged).

### 3.6 Tests

- Test projects target `net10.0;net11.0`; CI runs the suite on both legs post-merge, and the RC-window run covers the net11.0 leg from the work branch (CI-validation only) [`DECISIONS-CliInvoke-dotnet11.md#T002`].
- OS-dependent behaviors (real executables, `pwsh`-on-PATH skips) follow existing skip conventions on both legs.

### 3.7 Packaging and docs

- Scope limited to: README/`site/docs` support statements covering `net10.0 + net11.0`, and release notes noting the multi-target bump [`DECISIONS-CliInvoke-dotnet11.md#T004`]. No "what .NET 11 adds" docs section; the adopt/decline record stays ledger-only.
- Per-package `PackageVersion` + CPM discipline and CHANGELOG flow unchanged.

---

## Ledger Reference

`DECISIONS-CliInvoke-v4-improvements.md`: D001 (goal), D002 (pull event stream), D003 (data redirection + Merge), D004 (keep SecureString; R10 dropped), D005 (config as middle tier), D006 (factory removed), D007 (ride v3 GA), D008 (v4 semantics session goal), D009 (stdin superseded by PipeSource; obsolete in v3.x post-GA, removed v4 — `docs/adr/0004`), D010 (redirection flag contradiction throws), D011 (always-tee event/target flow), D012 (public sealed record variants; union migration clause superseded by `DECISIONS-CliInvoke-dotnet11.md#D005`), D013 (Merge requires ≥ 2), D014 (v4 stabilising pre-release train; TFM plan resolved by `DECISIONS-CliInvoke-dotnet11.md#D009`), D015 (lifecycle events sourced from ExternalProcess handlers), T001 (foundation locked; language/TFM superseded by `DECISIONS-CliInvoke-dotnet11.md#D002`), T002 (Core invoker member), T003 (full pipeline flow), T004 (Core pipe types, config attachment), T005 (init + required target), T006 (builder kept), T007 (migrate + property tests), T008 (blueprint output), T009 (manual handoff).

`DECISIONS-CliInvoke-dotnet11.md`: D001 (goal), D002 (dual-target `net10.0;net11.0`), D003 (TFM work merges post-GA; stable band untouched during RC window), D004 (`3.2.0-*` prerelease with net11.0 assets during RC window; v3.1 stays net10-only), D005 (sealed record pipe variants; union migration clause dropped), D006 (LangVersion per-TFM defaults, pins removed), D007 (bounded per-pain-point API adoption posture), D008 (suspend/resume internal start-suspended flow in ProcessWrapper; no new public API), D009 (v4 train dual-targets from alpha.1), T001 (global.json 11.0.1xx band at merge; CI dual SDKs), T002 (dual-target test projects, CI runs both legs), T003 (adopted set: suspend/resume, TryGetProcessById, Signal, ProcessExitStatus, handle-based kill; one-shot/detach declined), T004 (docs: support matrix + release notes only), T005 (output: in-place blueprint amendment; manual handoff).
