# Implementation Blueprint — CliInvoke v3.2: .NET 11 RC-window prerelease train

## Scope Binding

- **Decision Ledger:** `docs/decisions/DECISIONS-CliInvoke-dotnet11.md` (all records locked)
- **Output format decision:** `docs/decisions/DECISIONS-CliInvoke-dotnet11.md#T005`
- **Sibling blueprint:** `IMPLEMENTATION-CliInvoke_Improvement_Report.md` (Parts 1–2: v3 GA construction story ✅ and v4 capability surfaces — **not** covered by this file)
- **Notice:** This blueprint is valid ONLY for the v3.2 release train scoped by `docs/decisions/DECISIONS-CliInvoke-dotnet11.md#D003` and `#D004`. It must not be applied to the v4 train (`DECISIONS-CliInvoke-v4-improvements.md#D014` / `DECISIONS-CliInvoke-dotnet11.md#D009`) or to `3.1.x`.

## What v3.2 is (and is not)

Per `docs/decisions-CliInvoke-dotnet11.md#D004`:

- **IS:** a `3.2.0` train whose **prereleases** (`3.2.0-*`, NuGet-prerelease only) carry `net10.0;net11.0` assets, published during the .NET 11 RC window from a **work branch**; stable `3.2.0` follows after .NET 11 GA (expected Nov 2026).
- **IS NOT:** v3.1 — v3.1 ships the pending stabilizing changes net10.0-only on the current `3.1.0-beta.1` build, and no post-RC API-shift republish churn enters v3.1 (`#D004`). RC-era API risk is quarantined in the v3.2 train.
- **IS NOT:** the v4 train — pipes/events/streaming live in the sibling blueprint's Part 2; the v4 train inherits this TFM shape from its first alpha but publishes net11.0 assets only post-GA (`#D009`).

---

## 1. Work branch setup (RC window, pre-GA)

### 1.1 `src/CliInvoke.Core/CliInvoke.Core.csproj`, `src/CliInvoke/CliInvoke.csproj`, `src/CliInvoke.Specializations/CliInvoke.Specializations.csproj`

- Change `TargetFrameworks` from `net10.0` to `net10.0;net11.0` [`docs/decisions/DECISIONS-CliInvoke-dotnet11.md#D002`].
- Remove the explicit `<LangVersion>14</LangVersion>` pins: the net10.0 leg compiles as C# 14, the net11.0 leg as C# 15 (stable features only) via per-TFM MSBuild defaults; **no preview-gated language surface anywhere** (`LangVersion=preview` prohibited in `src/` and `tests/`) [`#D006`].
- `IsAotCompatible` / `IsTrimmable` / PolyEnsure behavior must hold on **both** legs; per-leg parity failures block the prerelease publish [`#D002`].
- Race the csproj `PackageVersion` on the work branch to `3.2.0-alpha.1` (then beta/RC as needed); per-package `PackageVersion` + CPM discipline unchanged [`#T004`].

### 1.2 `global.json` (work branch only)

- Pin the `.NET 11 RC` band locally on the work branch so both legs build; **`main`'s stable-band pin is untouched during the whole RC window** [`#D003`, `#T001`]. The `11.0.1xx` stable-band pin happens only at the post-GA merge (sibling blueprint §3.3).
- Note: `rollForward` cannot cross the 10→11 major boundary — only the .NET 11 SDK can build the net11.0 leg [`#T001`].

### 1.3 Test projects — `tests/CliInvoke.Tests`

- Dual-target `net10.0;net11.0` on the work branch; the RC-window run covers the net11.0 leg and is **CI-validation only — it must not publish** beyond the permitted `3.2.0-*` prerelease flow [`#T002`, `#D003`].
- OS-dependent behaviors (real executables; `powershell.exe`/`pwsh`-on-PATH skips) follow existing skip conventions on both legs.

### 1.4 CI behavior during RC window

- No publish from CI on `main`; only read-only CI validation jobs may install preview/RC SDKs, plus the work-branch `3.2.0-*` prerelease publish flow [`#D003`, `#D004`]. Release CI during the RC window needs the .NET 11 SDK installed on the work branch [`#D004`].
- ConfigureAwait guard (`scripts/guard-configureawait.sh src`) and XML-doc-as-error gates run on both legs [`#D006`].

---

## 2. Process API adoption shipped in the v3.2 net11.0 leg (all `#if NET11_0`)

Confirmed `net11.0`-leg work items must be finalized before the first `3.2.0-*` publish.

### 2.1 Suspend/resume flow — `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`

- On the net11.0 leg, `ProcessWrapper` starts processes **suspended** via `ProcessStartInfo.StartSuspended`, applies `ProcessResourcePolicy` through the process control adapter, then resumes — **always, including when `SetResourcePolicy` throws** — via `SafeProcessHandle.Resume()` [`#D008`].
- **OS scoping (`#if NET11_0`):** the `StartSuspended` + resume replacement applies on **Windows and macOS** (see the in-code TODO at `ProcessWrapper.cs:131–132`); Linux/macOS/FreeBSD adapters keep the `kill(SIGSTOP)`/`kill(SIGCONT)` P/Invoke path (`UnixProcessControlAdapter.cs:224`) on both legs, with .NET 11 signal adoption arriving via `Process.Signal(PosixSignal)` rather than a raw `kill` swap.
- Call sites being replaced (current line numbers): the suspend → apply → always-resume guard cycle in `OnStarted` (`ProcessWrapper.cs:120–165`); `SuspendProcess`/`ResumeProcess` and routing through per-OS control adapters at `:355`/`:377`.
- **No new public API** on `ExternalProcess` or `IExternalProcess`; suspend/resume stays a private mechanism and `IExternalProcess` surface is unchanged [`#D008`].

### 2.2 Adapter-level API swaps — `src/CliInvoke/Processes/Internal/ControlAdapters/`

- Adopt: `Process.TryGetProcessById`, `Process.Signal(PosixSignal)`, `ProcessExitStatus` introspection, and handle-based kill/signal/waits **only where the adapters currently P/Invoke**: `GetTerminatingSignal` via the `Signal` property (`ProcessWrapper.cs:117`), `SendInterruptSignalAsync` (`ProcessWrapper.cs:989`), Windows `GenerateConsoleCtrlEvent`/`AttachConsole` P/Invoke region (`WindowsProcessControlAdapter.cs:222–246`), Unix `kill` (`UnixProcessControlAdapter.cs:224`) [`#T003`].
- Every mapped site stays **behavior-identical on net10.0** through the shared-source path [`#T003`].
- Pipeline-core launch/wait plumbing is **NOT rewritten** [`#D007`].

### 2.3 Formally declined (ledger-recorded; not mirrored in user-facing docs per `#T004`)

- `Process.RunAndCaptureTextAsync`/`Run*`, `ReadAll*`, `Process.StartAndForget`, `ProcessStartInfo.StartDetached` (compete-with-self — CliRun/Buffered own that surface) [`#T003`].
- `ProcessStartInfo.InheritedHandles` and Std-handle pre-spawning default out of scope [`#T003`].

---

## 3. Packaging & docs (v3.2 scope)

- README / `site/docs` support statements updated to cover `net10.0 + net11.0`; release notes note the multi-target bump [`#T004`].
- **No** "what .NET 11 adds" docs section, **no** per-platform capability table, **no** user-facing adopt/decline list (that record stays ledger-only) [`#T004`].
- CHANGELOG flow rides the existing release process; the `D008`/`T003` adopt/decline details remain documented only in `docs/decisions/DECISIONS-CliInvoke-dotnet11.md` [`#T004`].

---

## 4. Stable `3.2.0` (post-GA)

- Merge the work branch to `main` after .NET 11 GA; flip `global.json` to the stable `11.0.1xx` band (`rollForward: latestFeature`) and set CI to install .NET 10.x + .NET 11.x SDKs at that point [`#T001`, `#D003`].
- Stable `3.2.0` publishes with `net10.0;net11.0` assets; v3.x stables before GA remain net10.0-only [`#D003`].
- Contributors build with the .NET 11 SDK alone from then on — the .NET 10 SDK cannot build the net11.0 leg [`#T001`].

## Explicitly out of scope for v3.2

- v4 capability surfaces (`ListenAsync`, `ProcessEvent`, `PipeSource`, `PipeTarget`) — sibling blueprint Part 2, v4 train (`DECISIONS-CliInvoke-v4-improvements.md#I009`).
- Polyfill / compatibility-machinery audit — declined at gate A of the ledger session (`DECISIONS-CliInvoke-dotnet11.md#D001`).
- v4-train versioning interaction — resolved separately for the 4.0.0 train (`#D009`); nothing here touches `4.0.0-*`.

---

## Ledger Record Map

| Record | What it decides | Section above |
|---|---|---|
| `#D002` | Dual-target `net10.0;net11.0`, both legs AOT/trimmable | 1.1 |
| `#D003` | Post-GA merge gate; RC window validation rules; stable band untouched | 1.2, 1.4, 4 |
| `#D004` | `3.2.0-*` prerelease vehicle; v3.1 isolation | What v3.2 is, 1.4 |
| `#D006` | LangVersion pins removed; per-TFM defaults; no preview surface | 1.1, 1.4 |
| `#D007` | Bounded adoption posture; no pipeline-core rewrite | 2.2 |
| `#D008` | Suspend/resume internal flow; no new public API | 2.1 |
| `#D009` | v4 train inherits TFM shape (pointer only) | What v3.2 is |
| `#T001` | global.json/CI SDK mechanics; stable band moves at merge | 1.2, 1.4, 4 |
| `#T002` | Dual-leg test projects and CI runs | 1.3 |
| `#T003` | Adopted/declined API set, bounded to adapter layer | 2.2, 2.3 |
| `#T004` | Docs/support matrix scope; ledger-only adoption record | 3, 2.3 |
