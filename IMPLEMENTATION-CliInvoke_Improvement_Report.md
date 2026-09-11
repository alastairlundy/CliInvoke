# Implementation Blueprint — CliInvoke v4 improvements

## Scope Binding

- **Linked Spec:** `CliInvoke_Improvement_Report.md`
- **Decision Ledger:** `docs/decisions/DECISIONS-CliInvoke-v4-improvements.md`
- **Notice:** This blueprint is a context pointer valid ONLY for the linked spec and ledger. It must not be applied to other specifications without explicit authorization.

## Release scoping

The ledger resolves the plan into two release trains:

- **v3 GA (pre-GA, current `3.0.0-beta.2` line)** — the construction story: init conversion, factory removal, builder positioning. Records: `D005`, `D006`, `D007`, `T005`, `T006`.
- **v4 (post-GA)** — the capability surfaces: event stream and pipes. Records: `D002`, `D003`, `T002`, `T003`, `T004`.
- **Explicitly not in v3:** `ListenAsync`, `ProcessEvent`, `PipeSource`, `PipeTarget` (user clarification, `DECISIONS-CliInvoke-v4-improvements.md#I009`).
- **Dropped from the plan:** R10 (`UserCredential`/`SecureString` rework) — `DECISIONS-CliInvoke-v4-improvements.md#D004`.

---

## Part 1 — v3 GA: construction story

### 1.1 `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`

- Convert get-only properties to `init`: `WorkingDirectoryPath`, `Arguments`, `ArgumentList`, `WindowCreation`, `EnvironmentVariables`, `Credential`, `UseShellExecution`, `StandardInput`, `RedirectStandardInput`, `OutputRedirection`, `ResourcePolicy`, and the three encodings, plus `RequiresAdministrator` [`DECISIONS-CliInvoke-v4-improvements.md#T005`].
- Mark `TargetFilePath` `required` (already `init`) [`DECISIONS-CliInvoke-v4-improvements.md#T005`]; annotate the convenience constructor with `[SetsRequiredMembers]` [`DECISIONS-CliInvoke-v4-improvements.md#T005`].
- Remove the `protected internal` 16-parameter constructor; its defaults move to property initializers (`UserCredential.Null`, `ProcessResourcePolicy.Default`, empty `ImmutableSortedDictionary`, `Encoding.Default`, `StreamWriter.Null`) [`DECISIONS-CliInvoke-v4-improvements.md#T005`].
- The `EnvironmentVariables` init accessor coerces any supplied dictionary into the ordinal-sorted `ImmutableSortedDictionary`, preserving the documented snapshot/equality semantics [`DECISIONS-CliInvoke-v4-improvements.md#T005`].
- Init-accessor validation: `TargetFilePath` null/empty throws (parity with the current constructor); `WorkingDirectoryPath` runs the `Directory.Exists` check and throws `DirectoryNotFoundException` on miss — factory-parity semantics [`DECISIONS-CliInvoke-v4-improvements.md#D006`].
- Merge `ArgumentsList` into `ArgumentList` as init-only; delete the mutable `ArgumentsList` property; equality/hash keep `ArgumentList` sequence semantics [`DECISIONS-CliInvoke-v4-improvements.md#T005`].

### 1.2 `src/CliInvoke/Extensions/ProcessConfigurationFactory.cs`

- Delete the file (both `Create` overloads) [`DECISIONS-CliInvoke-v4-improvements.md#D006`, `DECISIONS-CliInvoke-v4-improvements.md#D007`].

### 1.3 `src/CliInvoke/CliRun.cs`

- `BuildStringArgsConfig` (line 190) and `FireAndForget(string)` (line 175) construct `ProcessConfiguration` directly via init instead of the factory [`DECISIONS-CliInvoke-v4-improvements.md#D006`].

### 1.4 `src/CliInvoke/ShellDetector.cs`

- The four construction sites (lines 69, 78, 119, 141) construct directly [`DECISIONS-CliInvoke-v4-improvements.md#D006`].

### 1.5 `src/CliInvoke/Extensibility/RunnerConfigurationFactory.cs`

- Adapt `ArgumentsList` writes to `ArgumentList` [`DECISIONS-CliInvoke-v4-improvements.md#T005`].

### 1.6 `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` + `src/CliInvoke.Core/Builders/IProcessConfigurationBuilder.cs`

- Keep the builder for argument escaping and `UserCredentialSpec`/resource-policy callback flows; init construction becomes the documented default [`DECISIONS-CliInvoke-v4-improvements.md#T006`].
- `Set*` methods delegate to the init properties where applicable; `Build()` semantics unchanged; DI registration in Specializations unchanged [`DECISIONS-CliInvoke-v4-improvements.md#T006`].

### 1.7 Docs

- `DESIGN_PATTERNS.md` / README: position init construction as the default and the builder as the advanced path (escaping, credential specs) [`DECISIONS-CliInvoke-v4-improvements.md#T006`].

### 1.8 Tests — v3 pass

- Migrate the ~20 `ProcessConfigurationFactory.Create` call sites (CliRun paths, invoker tests, middleware integration tests, Specializations tests, AOT/trimming programs) [`DECISIONS-CliInvoke-v4-improvements.md#T007`, `DECISIONS-CliInvoke-v4-improvements.md#D006`].
- New coverage: init validation (target, working directory), `required` enforcement, `ArgumentsList` merge [`DECISIONS-CliInvoke-v4-improvements.md#T007`].
- FsCheck property tests: equality/hash stability across environment-variable insertion order; snapshot isolation from caller dictionaries [`DECISIONS-CliInvoke-v4-improvements.md#T007`].

---

## Part 2 — v4: capability surfaces

### 2.1 `src/CliInvoke.Core/IProcessInvoker.cs`

- Add `IAsyncEnumerable<ProcessEvent> ListenAsync(ProcessConfiguration, ProcessExitConfiguration?, CancellationToken)` as a member [`DECISIONS-CliInvoke-v4-improvements.md#T002`].

### 2.2 `src/CliInvoke.Core/Events/` (new)

- `ProcessEvent` abstract record carrying `ProcessId`; `StartedProcessEvent`, `StandardOutputProcessEvent`, `StandardErrorProcessEvent`, `ExitedProcessEvent` (`ExitCode`, `PosixSignal?`, `Canceled`) — reusing the existing result vocabulary [`DECISIONS-CliInvoke-v4-improvements.md#T002`, `DECISIONS-CliInvoke-v4-improvements.md#D002`].

### 2.3 `src/CliInvoke.Core/Pipes/` (new)

- `PipeSource` factories: `Null`, `FromStream`, `FromFile`, `FromBytes`, `FromString`, `FromDelegate` [`DECISIONS-CliInvoke-v4-improvements.md#T004`].
- `PipeTarget` factories: `Null`, `ToStream`, `ToFile`, `ToStringBuilder`, `ToDelegate`, `Merge` [`DECISIONS-CliInvoke-v4-improvements.md#T004`].
- No pipe type starts or owns a process [`DECISIONS-CliInvoke-v4-improvements.md#D003`].

### 2.4 `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs` (v4 additive)

- Init properties for the stdin pipe source and the standard output/error targets; equality gains pipe-reference fields (reference equality, matching the `StandardInput` precedent) [`DECISIONS-CliInvoke-v4-improvements.md#T004`].

### 2.5 `src/CliInvoke/ProcessInvocationPipeline.cs`

- `ListenAsync` implementation: a streaming invocation mode; events produced as they happen; the middleware chain flows around the stream; `ValidationRules` evaluate after the `Exited` event; cancellation/timeout reuses `ProcessExitConfiguration` machinery [`DECISIONS-CliInvoke-v4-improvements.md#T003`].

### 2.6 `src/CliInvoke/CliRun.cs`

- `CliRun.ListenAsync` static facade — execution only; no configuration surface on `CliRun` [`DECISIONS-CliInvoke-v4-improvements.md#T003`, `DECISIONS-CliInvoke-v4-improvements.md#I004`].

### 2.7 Middleware contract

- Define streaming-mode behavior for `IProcessMiddleware`/`InvocationContext` (how pre-next/post-next wrap a stream) [`DECISIONS-CliInvoke-v4-improvements.md#T003`].

### 2.8 Tests — v4 pass

- `ListenAsync` pipeline flow (middleware tee, post-`Exited` validation), pipe attachment, and property tests for pipe normalization [`DECISIONS-CliInvoke-v4-improvements.md#T007`].

---

## Ledger Reference

`DECISIONS-CliInvoke-v4-improvements.md`: D001 (goal), D002 (pull event stream), D003 (data redirection + Merge), D004 (keep SecureString; R10 dropped), D005 (config as middle tier), D006 (factory removed), D007 (ride v3 GA), T001 (foundation locked), T002 (Core invoker member), T003 (full pipeline flow), T004 (Core pipe types, config attachment), T005 (init + required target), T006 (builder kept), T007 (migrate + property tests), T008 (blueprint output), T009 (manual handoff).
