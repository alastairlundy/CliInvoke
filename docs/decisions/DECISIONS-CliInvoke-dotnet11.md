# Decision Ledger — CliInvoke .NET 11 support

Session: technical-grilling over adding .NET 11 support to CliInvoke and the accompanying changes and improvements (TFM strategy, LangVersion/C# 15 adoption, CI/SDK, .NET 11 Process API adoption, pipe-variant union/closed-hierarchy fate), re-baselined against repo state `net10.0` single-TFM, v3.1.0 unreleased. Related ledger: `DECISIONS-CliInvoke-v4-improvements.md` (D001–D015, T001–T009).

### [D001] - session goal

- **Driver**: the user wants .NET 11 support added to CliInvoke together with the changes and improvements that go along with it, pressure-tested into implementable decisions.
- **Resolved Answer**: "Full end to end + update to v4 implementation blueprint on .NET 11 TFM + C# 15 decision"
- **Normalized Requirement**: The session shall resolve .NET 11 enablement (TFM matrix, LangVersion, global.json/CI, packaging, docs) and the improvement decisions (new Process/SafeProcessHandle API adoption, D012 pipe-variant union/closed-hierarchy fate) into decision records, and shall amend `IMPLEMENTATION-CliInvoke_Improvement_Report.md` with the .NET 11 TFM + C# 15 outcome.
- **Constraints**: track: concept-then-implementation. Gate A outcomes (user): D009 (stdin obsolescence→v4 removal) and D014 (4.0.0-alpha/beta train alongside stable v3) are **locked** — release-timing landscape not re-opened; T001 (foundation, net10 no-language-change) and D012 (union migration "at the first net11 TFM bump") are **open** for this session. Scope: enablement + Process API adoption; polyfill/compat audit declined. Verified library facts (Sept 2026): .NET 11 RC1, GA Nov 2026, STS (24-mo support; only .NET 10 is LTS); `net11.0` requires the .NET 11 SDK (`rollForward` cannot cross majors); C# 15 is the `net11.0` default and union types / closed hierarchies are preview-gated (`LangVersion=preview`), `UnionAttribute` shipped in .NET 11 previews and still stabilising.

### [I001] - ledger home and improvement scope

- **Prompt**: "(1) Where should this session's records get stored? (2) What counts as \"the changes and improvements that go along with it\"? (The pipe-variant union/closed-hierarchy fate is confirmed open already and gets its own branch regardless.)"
- **User Response**: "New ledger (Recommended)" and "Enablement + Process APIs (Recommended)"
- **Resolution**: records continue in a new ledger, `docs/decisions/DECISIONS-CliInvoke-dotnet11.md`; scope is enablement (TFM/LangVersion, global.json, CI, packaging, docs) plus adoption decisions for the new .NET 11 Process/SafeProcessHandle APIs including deliberate ignores; polyfill/compatibility-machinery audit excluded.
- **Notes**: the D012 union/closed-hierarchy fate branch proceeds independently per the user's confirmation; old ledger stays on disk as-is with its "pending D012 confirmation" wording intact.

### [D002] - support model

- **Driver**: the user wants real .NET 11 support for net11 consumers without cutting off the net10 audience that the just-shipped stable v3 line serves (`DECISIONS-CliInvoke-v4-improvements.md#D014`).
- **Resolved Answer**: "Option A - Dual target net10.0 and net11.0"
- **Normalized Requirement**: CliInvoke packages (Core, CliInvoke, Specializations) shall multi-target `net10.0;net11.0`; the net11.0 leg may use TFM-conditional APIs and its C# 15 default; the net10.0 leg and its consumers are unchanged.
- **Constraints**: Single-TFM `net11.0` floor bump rejected; runtime-only "compat stance" rejected. Per-TFM source drift is guarded by CI and later branches. `IsAotCompatible`/`IsTrimmable` and PolyEnsure behavior must hold on both legs.
- **Cites**: D001

### [D003] - timing gate

- **Driver**: the user wants published stable assets free of RC-era API risk — GA-validated SDK and API surface only for stable releases.
- **Resolved Answer**: "Option A - v3.x stable stays on .NET 10 until .NET 11 reaches GA."
- **Normalized Requirement**: The `net11.0` TFM work shall merge after .NET 11 GA (expected Nov 2026); the RC/preview window is used only for read-only CI validation; stable v3.x releases ship net10.0-only assets until GA. *(Amended by D004 — Supersedes: D003's "read-only RC window" scope is narrowed: the read-only barrier now applies only to stable-band pins and stable-version publishes; the `3.2.0-*` prerelease publishes during the RC window per D004, built from a work branch without touching the pinned stable band on `main`.)*
- **Constraints**: User floated shipping a pre-release v3 with .NET 11 RC support before GA — opened as its own branch (resolved in D004), not folded into this record. No publish from CI during the RC window.
- **Cites**: D001, D002

### [D004] - RC-window pre-release

- **Driver**: the user wants real-adopter validation of the net11.0 assets before GA without holding the v3.1 stabilizing release hostage to the .NET 11 calendar.
- **Resolved Answer**: "Option A but I want the exact versioning to be in v3.2 to not block v3.1's release. v3.1 ships stabilizing changes with .NET 10 only."
- **Normalized Requirement**: v3.1.0 shall ship net10.0-only with the pending stabilizing changes, on the current net10-only build; a `3.2.0` prerelease carrying `net10.0;net11.0` assets shall publish during the .NET 11 RC window, with stable `3.2.0` following after .NET 11 GA.
- **Constraints**: No post-RC API-shift republish churn into v3.1 — RC-era risk is quarantined in the v3.2 train. `3.2.0-*` prereleases are NuGet-prerelease only (stable v3 protection intact per D003). Release CI during the RC window needs the .NET 11 SDK installed.
- **Cites**: D001, D002, D003

### [T001] - SDK and CI mechanics

- **Driver**: the user wants one truthful pinned toolchain that can build both legs of the dual-target solution.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: When the `net11.0` TFM work merges (post-GA, per D003), `global.json` shall pin the `11.0.1xx` band (`rollForward: latestFeature`); CI shall install .NET 10.x + .NET 11.x SDKs; contributors build with the .NET 11 SDK alone.
- **Constraints**: The 11 SDK is the only SDK that can build both legs; the 10 SDK cannot build net11.0 (`rollForward` cannot cross majors). During the RC window, only read-only CI validation jobs may install preview/RC SDKs (D003); no stable-band pin changes pre-merge.
- **Cites**: D001, D002, D003

### [D005] - pipe-variant union fate

- **Driver**: the user wants stable, reviewable v4 pipe surfaces rather than preview-era language flexibility; the net11 TFM bump does not actually un-gate unions (`LangVersion=preview` still required at RC1, verified).
- **Resolved Answer**: "Option A - keep sealed record variants for v4 and drop the 'at the first net11 bump' clause"
- **Normalized Requirement**: The v4 `PipeSource`/`PipeTarget` variants ship as public sealed record variants exactly per `DECISIONS-CliInvoke-v4-improvements.md#D012`; D012's pending "migrate to union/closed hierarchies at the first net11 TFM bump" clause is superseded and no migration is planned by this session.
- **Constraints**: Supersedes: D012's migration clause only (the sealed-record resolution itself stands). May re-open as a fresh branch if a future C# release un-gates unions. Blueprint §2.3 must be amended to drop the migration clause (`IMPLEMENTATION-CliInvoke_Improvement_Report.md` §2.3).
- **Cites**: D001, D002

### [I002] - Branch E and G clarifications

- **Prompt**: "<user-posed> (1) Does taking advantage of .NET 11 Process APIs require C# 15? (2) I'm not sure I understand what Option B entails exactly. Also where does Process Suspend and Resuming fit in your options (if anywhere), Option A or Option B?"
- **User Response**: (Both questions — no resolutions given for E or G yet.)
- **Resolution**: (1) Answered no — the new Process/`SafeProcessHandle` APIs are runtime/BCL additions callable from the net11.0 leg at any `LangVersion`; C# 15 language features and preview-gated unions are irrelevant to API adoption. (2) Option B clarified as broad adoption inside the pipeline core (e.g., `SafeProcessHandle.Start(ProcessStartInfo)` replacing a `Process`-object launch, wait-by-handle) with per-leg maintenance; suspend/resume (`ProcessStartInfo.StartSuspended` + `SafeProcessHandle.Resume()`) fits neither A nor B — it is exposed as a new-capability branch (D008) next round.
- **Notes**: Branch F resolved as D005 this turn; D008 spawned directly from this clarification.

### [D006] - language level

- **Driver**: the user wants per-leg language levels matching each runtime's designed default, with minimal config ceremony; verified that the new Process APIs do not require C# 15 (I002).
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The repo `LangVersion 14` pins shall be removed; the net10.0 leg compiles as C# 14, the net11.0 leg as C# 15 (stable features only), via per-TFM MSBuild defaults; no preview-gated language surface ships.
- **Constraints**: No preview-gated use anywhere in src/tests. Both legs must still satisfy the XML-doc-as-error and ConfigureAwait guard gates. Any shared-source file relying on identical parsing across legs is enforced by CI, not config.
- **Cites**: D001, D002, D005, I002

### [D007] - Process API adoption posture

- **Driver**: the user wants real .NET 11 Process wins where they pay for themselves, with per-leg maintenance cost kept linear and visible.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The net11.0 leg adopts the new `Process`/`SafeProcessHandle` surface at bounded, per-pain-point call sites behind `#if NET11_0` (candidates: `Process.TryGetProcessById`, `Process.Signal(PosixSignal)`, `ProcessExitStatus`, selective handle inheritance); pipeline-core launch/wait plumbing is NOT rewritten (Option B declined); no blanket decline (Option C declined).
- **Constraints**: Each adopted site keeps identical net10.0 behavior through the existing shared-source path; per-leg parity is a CI concern rather than a source concern. Public API visibility choices (whether any adopted call site becomes public surface) are a later branch. The exact adopted-set list is finalized in a follow-up branch before the blueprint amendment.
- **Cites**: D001, D002, D003, D005, D006, I002

### [D008] - suspend/resume inside ProcessWrapper (internal capability)

- **Driver**: the user wants suspend/resume to stay a private implementation mechanism — applying `ProcessResourcePolicy` after start without exposing new process-control surface.
- **Resolved Answer**: "ProcessWrapper gets updated to use StartSuspended on .NET 11 to apply Resource Policies and then resumes afterwards. Process Suspending and Resuming code in Process Control Adapters use .NET 11's Process APIs on .NET 11 TFM and use existing code on .NET 10. No new APIs publicly exposed on ExternalProcess or IExternalProcess."
- **Normalized Requirement**: On the net11.0 leg, `ProcessWrapper` shall start processes suspended, apply `ProcessResourcePolicy` via the process control adapter, then resume always (including when `SetResourcePolicy` throws); suspend/resume adapter code shall call the new .NET 11 `Process`/`SafeProcessHandle` APIs on the net11.0 leg and the existing code on net10.0; `ExternalProcess`/`IExternalProcess` shall not expose any new public suspend/resume API.
- **Constraints**: Cross-reference verified in code: `ProcessWrapper.cs:60–106`, adapter factory per-OS adapters, `SuspendProcess`/`ResumeProcess` at `ProcessWrapper.cs:284/306`, "Always resume — even if SetResourcePolicy throws" guard comments at lines 135–152. The exact .NET 11 API mapping inside the adapters (e.g., `ProcessStartInfo.StartSuspended`, `SafeProcessHandle.Resume()`) is finalized in the adopted-set branch (T003). No net10.0 behavior change on the shared-source path.
- **Cites**: D001, D002, D003, D006, D007, I002

### [T002] - test runtime matrix

- **Driver**: the user wants per-leg evidence rather than compile-only confidence, keeping the D007-parity promise testable.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: Test projects (at minimum `tests/CliInvoke.Tests`) shall dual-target `net10.0;net11.0`; CI shall run the suite on both legs once net11.0 merges (per D003), with the RC-window validation run covering the net11.0 leg during the preview phase.
- **Constraints**: OS-dependent behaviors (spawning real executables; powershell/pwsh on PATH) follow existing skip conventions on both legs. The RC-window net11.0 test run is CI-validation only and must not publish.
- **Cites**: D001, D002, D003, T001

### [D009] - v4 train × net11 interplay

- **Driver**: the user wants one consistent multi-target shape across every published train once v3.2 has validated it.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The 4.0.0 (v4) stabilization train shall carry `net10.0;net11.0` assets from its first alpha, inheriting the TFM shape validated by the v3.2 prerelease (D004); the v4 TFM plan interaction noted in `DECISIONS-CliInvoke-v4-improvements.md#D014` is resolved.
- **Constraints**: Does not change D014's locked release-timing landscape; net11.0 assets publish on the v4 train only after .NET 11 GA (D003), i.e., the train's early alphas may be net10-only in practice until GA.
- **Cites**: D001, D002, D003, D004

### [T003] - exact adopted set

- **Driver**: the user wants the net11 Process adoption bounded and proportional, with declines as formally recorded as adoptions.
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The net11.0 leg shall adopt, inside the adapter layer/`ProcessWrapper` only: `ProcessStartInfo.StartSuspended` + `SafeProcessHandle.Resume()` (D008 flow), `Process.TryGetProcessById`, `Process.Signal(PosixSignal)`, `ProcessExitStatus` introspection, and handle-based kill where the adapters currently P/Invoke; the one-shot capture and detach family (`RunAndCaptureTextAsync`/`Run*`, `ReadAll*`, `StartAndForget`, `StartDetached`) is formally declined and recorded.
- **Constraints**: Every mapped site must stay behavior-identical on net10.0 (verified sites: `ProcessWrapper.cs:106`, `:814`, suspend/resume paths); per-leg + per-OS verification rides T002's dual-leg runs; handle-based swaps confined to interrupt/suspend/resume/kill adapter paths; InheritedHandles/Std-handle pre-spawning remains out of scope by default.
- **Cites**: D001, D006, D007, D008

### [T004] - packaging and docs

- **Driver**: the user wants the dual-target shape legible with the smallest durable artifact set to maintain.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: Packaging and docs changes shall be limited to: README/`site/docs` support-statement updates covering `net10.0 + net11.0`, and release notes noting the multi-target bump; no "what .NET 11 adds" docs section and no per-platform capability table.
- **Constraints**: The T003 adopt/decline record stays ledger-only (user declined a user-facing docs mirrored section); per-Csproj `PackageVersion` and CPM discipline unchanged; CHANGELOG updates ride the existing release process.
- **Cites**: D001, D002, D004, T003

### [T005] - output format and downstream consumer

- **Driver**: the user wants the .NET 11 decisions auditable inside the existing v4 blueprint rather than in a separate artifact.
- **Resolved Answer**: Output format pre-decided by D001 — "update to v4 implementation blueprint"; Part B: "Manual handoff (Recommended)"
- **Normalized Requirement**: The session shall amend `IMPLEMENTATION-CliInvoke_Improvement_Report.md` in place (no new standalone file), adding the .NET 11 TFM + C# 15 decision sections with inline `filename#Dxxx/Txxx` citations; no automated ticket/issue decomposition follows — the user drives downstream work.
- **Constraints**: Step 7.1 filename confirmation skipped (existing file named by D001); blueprint §2.3 drops the union-migration clause per D005 in the same edit; the Ledger Reference section extends to cite the dotnet11 ledger records.
- **Cites**: D001, D005

<!-- next-d: D010 -->
<!-- next-t: T006 -->
<!-- next-i: I003 -->
