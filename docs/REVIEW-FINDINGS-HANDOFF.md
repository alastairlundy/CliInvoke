# CliInvoke Review Findings — Fix Handoff

Date: 2026-10-18 (updated 2026-10-18 after fix round 1)
Source: behavioral review of `src/` ("potentially surprising or unexpected behaviour"), severity-ranked, followed by per-finding fix proposals. Findings verified against the tree at 3.1.0.

**TL;DR: Fixed (verified): H1, M2, M4, M7, L3b. Everything else in this document is proposed/not implemented.** Fixed items verified with `dotnet build src/CliInvoke.sln` (0 errors) and the `tests/CliInvoke.Tests` suite (656 total, 0 failed, 1 pre-existing skip).

## Status board

| Finding | Severity | Status |
|---|---|---|
| H1 silent 2-min default timeout | High | ✅ **Fixed** — docs (timeout + kill/cancellation semantics) |
| H2 Raw-mode pipe deadlock | High | ❌ Not fixed — proposed (3.2.0) |
| M1 Raw vs Buffered cancellation divergence | Medium | ❌ Not fixed — proposed (3.2.0) |
| M2 `ProcessExitConfiguration` null-unsafe `==` | Medium | ✅ **Fixed** — null-aware operators |
| M3 retry middleware replays empty stdin on attempts ≥ 2 | Medium | ❌ Not fixed — proposed (3.2.0) |
| M4 "directory recursion" docs drift | Medium | ✅ **Fixed** — docs (GLOSSARY, XML, site/docs) |
| M5 Windows graceful exit degrades to kill | Medium | ❌ Not fixed — proposed (3.1.x docs) |
| M6 logging shows empty arguments for shell-wrapped runs | Medium | ❌ Not fixed — proposed (3.2.0) |
| M7 `MaxBufferedOutputBytes` docs contradiction | Medium | ✅ **Fixed** — docs converge on per-stream |
| L1 `FireAndForget` disposal + redirection trap | Low | ❌ Not fixed — proposed (3.2.0) |
| L2 eager filesystem I/O in `ExternalProcess` ctor | Low | ❌ Not fixed — proposed (3.2.0 minimal) |
| L3a static `Equals(null, null)` returns false | Low | ❌ Not fixed — proposed (3.1.x docs; flip deferred) |
| L3b case-sensitive path comparison on Windows | Low | ✅ **Fixed** — OS-aware comparison |
| L4 `WaitForExitOrTimeoutAsync` XML copy-paste bug | Low | ❌ Not fixed — proposed (3.1.x docs) |
| L5 UTF-8 default vs OEM/ANSI child output | Low | ❌ Not fixed — proposed (3.1.x docs) |
| L6 null env-var values silently dropped | Low | ❌ Not fixed — proposed (3.2.0) |
| L7 "Default" divergence + ±1s swallow window | Low | ❌ Not fixed — proposed (split: 3.1.x / 3.2.0 / 4.0.0) |

Repo gates for implementers: XML doc comments are **build errors** (CS1574/1580/1581/1584/1658/1734/1762 escalated in `Directory.Build.props`); style per `src/.editorconfig` (no `var` for built-ins, separate using groups, 100-col); GLOSSARY.md entries are contracts (PATH-first ordering is ADR-protected).

## Release batches (consolidated)

- **3.1.x — docs-only, zero behavior change:** H1, M4, M7 (**✅ done**) + L4, L5 docs, L3a doc notes, L7(a) docs + XML hazard notes for H2/L1/M5 (docs halves of those proposals). Theme: *make the contracts say what the code does.*
- **3.2.0 — behavior fixes, one release note ("Raw/FireAndForget pipe and cancellation contract alignment"):** H2 normalization, M1 Buffered cancellation, L1 fail-fast, M3 stdin replay, M6 logging, L2 minimal cache, L6 null env-var semantics, L7(b) OCE-gated swallow. Theme: *fail fast or normalize, never silently hang.*
- **4.0.0 / deferred:** H2 hard-fail (only if normalization draws bug reports), M1 throw-by-default, M5 console-attach knob (recommend never), L3a semantic flip (all eight types in one change), L2 lazy resolution, L5 encoding-default decision, L7(a) `Default` alignment.

---

## 3.2.0 proposals (code)

### H2 — Raw-mode pipe deadlock with `OutputRedirection=true`
**Fix:** In `ProcessInvocationPipeline.InvokeAsync` (`src/CliInvoke/ProcessInvocationPipeline.cs:44-100`), when `ctx.Mode == InvocationMode.Raw`, derive the config with `OutputRedirection` forced off before creating the `IExternalProcess` — same normalization the shell middleware already does via `Derive(cfg, ...)` (`src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs:84-86`). Pair with XML hazard notes on `CliRun.RunAsync(ProcessConfiguration)` (`CliRun.cs:101-115`), `ProcessInvoker.ExecuteAsync` (`ProcessInvoker.cs:92-114`), and `IExternalProcess.WaitForExitOrTimeoutAsync` (`IExternalProcess.cs:90-101` — direct `IExternalProcess` users bypass the pipeline; fixing that path means drain-and-discard readers inside `ExternalProcess`, docs-only is fine).
**Compat:** Technically observable (children see console handles instead of pipes in Raw mode) but nothing readable is lost — no public surface exposes Raw output. Ship in 3.2.0 with a release note.
**Alternative:** Fail fast — throw `InvalidOperationException` on Raw+redirection (clearest, breaks configs that "work" for quiet children today) → 4.0.0.
**Effort:** ~0.5 day incl. tests (chatty Raw child completes).

### M1 — Cancellation semantics differ between Raw and Buffered
**Fix:** Fix **Buffered**, not Raw — the documented contract on `ProcessExitConfiguration.CancellationThrowsException=false` (`ProcessExitConfiguration.cs:90-103`) is a normal result with `Canceled=true`; Raw honors it, Buffered violates it (stream-read OCE escapes `Task.WhenAll`). Extend the catch filter at `src/CliInvoke/Processes/ExternalProcess.cs:367-377` to also absorb `outputStrings` faults when the fault is OCE **and** `cancellationToken.IsCancellationRequested` **and** `wrapper.Canceled` (same three guards as the existing stdin clause — never absorb genuine read failures). Cancelled reads yield no partial strings, so return the `BufferedProcessResult` with empty output, `Canceled=true`. Update XML on `CaptureBufferedResultAsync` (`IExternalProcess.cs:103-127`, `ExternalProcess.cs:312-338`) and `CliRun.RunBufferedAsync`; note that callers wanting OCE already have the opt-in (`CancellationThrowsException=true`).
**Compat:** Observable change, but toward the documented contract — bug fix. 3.2.0 with release note.
**Alternative:** Unify on throw-by-default (cleaner .NET citizenship, contradicts documented default) → 4.0.0.
**Effort:** ~0.5 day incl. cancellation tests on both modes.

### L1 — `FireAndForget(ProcessConfiguration)` disposal + redirection trap
**Fix:** Fail fast in `CliRun.FireAndForget(ProcessConfiguration)` (`CliRun.cs:247-251`): throw `ArgumentException` when `configuration.OutputRedirection || configuration.RedirectStandardInput`. A redirected fire-and-forget is broken by construction (no one drains pipes; handle disposed immediately after `Start()`). Update the XML and cross-reference the string-args overload that forces redirection off (`CliRun.cs:261-270`).
**Compat:** Behavior change for callers passing redirected configs — they get silent breakage today. 3.2.0 with release note.
**Alternative:** Docs-only in 3.1.x, or silently force redirection off like the string overload (rejected — hides caller mistakes). Genuinely supporting redirected fire-and-forget needs background drain tasks outliving the call — complexity that earns nothing.
**Effort:** ~2 h incl. XML + tests.

### M3 — Retry middleware replays empty stdin on attempts ≥ 2
**Fix:** In `RetryMiddleware.InvokeAsync` (`src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs:77-96`), before invoking `next(context)` on any attempt after the first, reset the stdin source: if `context.Configuration.StandardInput?.BaseStream` is `{ CanSeek: true }`, set `Position = 0`; if stdin is set but not seekable, throw `InvalidOperationException` at the moment a retry would occur ("retrying an invocation with a non-seekable standard input stream cannot replay the input"). `RetryMiddleware` is `internal sealed`, so the fix site is contained. Add one `<remarks>` sentence to `UseRetry`/`RetryOptions` docs: stdin is replayed per attempt and must be seekable (or null).
**Compat:** No API surface change; silent empty-stdin corruption becomes correct replay or a loud error. 3.2.0.
**Alternative:** Snapshot stdin into a `MemoryStream` on first attempt — copies unbounded payloads and spreads retry knowledge into the pipeline; rejected.
**Effort:** ~1-2 h incl. replay tests (FileStream + MemoryStream, non-seekable throw case).

### M6 — LoggingMiddleware logs empty arguments for ArgumentList/shell-wrapped runs
**Fix:** In `LoggingMiddleware.cs:74`, compute the display string once: use `Configuration.Arguments` when non-whitespace, else `string.Join(" ", Configuration.ArgumentList)`; pass it through the existing `Sanitize` redactor (flag-then-value regex still matches across the join, so `["--password","hunter2"]` redacts correctly). Bonus: shell-wrapped invocations log the derived `ArgumentList` containing the quoted inner command — the truthful picture. Exit line untouched.
**Compat:** No public API change; log content only (current output is factually wrong). 3.2.0.
**Alternative:** Log both fields as structured properties — preserves raw fidelity but always logs an empty `Arguments` and doubles redaction calls; not worth it.
**Effort:** ~30-45 min incl. test asserting the ArgumentList case appears in logs.

### L2 — `ExternalProcess` constructor eagerly resolves the target file
**Fix (minimal, recommended):** Cache the `FileInfo` resolved in the constructor (`src/CliInvoke/Processes/ExternalProcess.cs:39`) and reuse it in `Start()` (:118) and `StartAsync()` (:167) instead of re-resolving. Three lines; closes the double-resolution/TOCTOU window and halves filesystem I/O; construct-time `FileNotFoundException` semantics preserved exactly.
**Compat:** Zero observable change. 3.2.0.
**Full fix (defer):** Lazy resolution — store only the resolver in the ctor, resolve on first `Start`/`StartAsync` so no I/O and no throw at create time. Moves an exception from ctor to Start (observable) → 4.0.0, and needs an "unstarted" `ProcessWrapper` state since `HasExited`/`HasStarted` touch `_processWrapper` pre-start. Do only if someone hits the construct-time-throw pain.
**Effort:** Minimal ~30 min; lazy ~2-3 h with lifecycle tests.

### L6 — `SetEnvironmentVariables` silently drops null-valued entries
**Fix:** In `BaseProcessControlAdapter.cs:239-243`, assign `process.StartInfo.Environment[variable.Key] = variable.Value!;` — BCL-documented semantics: a null value *removes* the variable from the child environment. Keep a code comment citing the Process docs semantics and update the `ProcessConfiguration.EnvironmentVariables` XML to state null-value behavior.
**Compat:** Edge-case change; previously-ignored null entries now clear inherited variables. Unobservable for conformant callers (nulls violate `IReadOnlyDictionary<string, string>` anyway). 3.2.0.
**Alternative:** Throw `ArgumentException` at the configuration boundary — surfaces violations loudly but adds validation the BCL doesn't have and breaks accidentally-working callers.
**Effort:** ~30 min incl. one test (null value → variable absent in child env).

### L7(b) — ±1s skew heuristic can swallow genuine failures
**Fix:** In `CancellationHelper.cs:98-108`, under `AllowExceptionsIfUnexpected`, gate the time-window swallow on exception type: only swallow `OperationCanceledException` raised within the window; rethrow all non-OCE exceptions regardless of timing (`if (exception is not OperationCanceledException || difference > TimeSpan.FromSeconds(1)) throw;`). Every OCE in this pipeline originates from CliInvoke's own cancellation machinery (timer or caller token), so on-time-timeout OCEs stay swallowed and intended behavior is preserved — while genuine I/O errors near the timeout can no longer vanish. The wall-clock heuristic alone is unsalvageable as a failure discriminator.
**Compat:** Internal method, behavior change only. 3.2.0.
**Alternative:** Narrow window to ±100 ms — still wall-clock gambling; rejected.
**Effort:** ~1 h incl. tests (OCE-near-timeout swallowed; IOException-near-timeout rethrown).

---

## 3.1.x proposals (docs-only)

### M5 — Windows "GracefulExit" timeout frequently degrades to a straight kill
**Fix:** Document precisely; do **not** attempt the console dance. XML on `ProcessTimeoutPolicy.FromTimeSpan`/`ProcessTimeoutPolicy` (`src/CliInvoke.Core/Primitives/Policies/ProcessTimeoutPolicy.cs:37-47, 114-118`) and `ProcessExitConfiguration`: `GracefulExit` is best-effort on Windows — a console-host caller typically cannot `AttachConsole(child)` (`src/CliInvoke/Processes/Internal/ControlAdapters/WindowsProcessControlAdapter.cs:187-216`), no signal is delivered, and the child is force-killed (`ProcessWrapper.cs:1052-1056`). Unix sends SIGTERM and is genuinely graceful. Plus a troubleshooting entry in `site/docs`.
**Never build:** the `FreeConsole()` → `AttachConsole(child)` → signal → restore sequence — a failed restore leaves the *host* without a console. Not honestly achievable with `GenerateConsoleCtrlEvent` from an arbitrary host.
**Effort:** docs ~30 min; the opt-in attach knob ~1-2 days if ever pursued (recommend never).

### L4 — `WaitForExitOrTimeoutAsync` XML promises a buffered result
**Fix:** At `IExternalProcess.cs:97-100`, change the `<returns>` to state the result is a plain `ProcessResult` without buffered output; cross-reference `CaptureBufferedResultAsync` for buffered capture.
**Effort:** 10 min.

### L5 — UTF-8 default decoding mojibakes OEM/ANSI child output
**Fix:** Docs-only now. XML remarks (`ProcessConfiguration.cs:276-303`) already state UTF-8; add a troubleshooting entry to `site/docs` (and a README note): children emitting Windows OEM/ANSI code-page text need explicit encodings — the builder API exists (see `RunnerConfigurationFactory.cs:131` for the pattern). Rationale for not flipping the default: UTF-8 is the correct modern default (pwsh 7, git, node emit it) and the BCL's own fallback (`Console.OutputEncoding`) breaks a different class of children — any default breaks someone; the fix is discoverability.
**Deferred (4.0.0):** default change (e.g., stdout/stderr UTF-8 + stdin `Console.InputEncoding`, or a `ProcessEncodings.Oem()`/`Ansi()` helper) — needs its own ADR-style note; `Console.OutputEncoding` can throw on non-console hosts and must not be captured in a static initializer.
**Effort:** docs ~1 h.

### L3a — static `Equals(left, right)` returns false for `(null, null)`
**Fix (docs now):** Add a `<remarks>` to all **eight** static `Equals` methods — `ProcessConfiguration.cs:386`, `ProcessExitConfiguration.cs:185`, `UserCredential.cs:147`, `ShellInformation.cs:88`, `BufferedProcessResult.cs:205`, `ProcessResult.cs:197`, `ProcessResourcePolicy.cs:198`, `ProcessTimeoutPolicy.cs:161` — stating: "Returns false when either operand is null, including when both are null; unlike `==`." **Resolved flag:** the in-progress operator fix has landed — `ProcessExitConfiguration`'s `==`/`!=` operators are now implemented independently (`left is null ? right is null : left.Equals(right)`; verified at `ProcessExitConfiguration.cs:201-202`) and no longer delegate to the static method, so the eight doc notes apply as written.
**Deferred (4.0.0):** semantic flip to `left is null ? right is null : left.Equals(right)` — observable change on public API; apply uniformly across all eight types in one change, never piecemeal. Alternatively keep the "strict" semantics permanently and close L3a as docs-only — defensible now that operators follow BCL convention.
**Effort:** docs ~1 h across eight types; flip ~1 h + tests in 4.0.0.

### L7(a) — two spellings of "default" `ProcessExitConfiguration` differ in exception behavior
**Fix (docs now):** XML remarks on `Default`, `CreateGraceful` (`src/CliInvoke.Core/Extensions/ProcessExitConfigurationCreationExtensions.cs:26-61`) and the `exitConfiguration ?? CreateGraceful()` call sites (`CliRun.cs:122/233`, `ExternalProcess.cs:41`) stating plainly: the graceful flavor suppresses unexpected-exit exceptions (`CancellationThrowsException=false`, `SuppressExceptions`); the ctor default allows them (`AllowExceptionsIfUnexpected`).
**Deferred (4.0.0):** keep `CreateGraceful` semantics (the name signals intent) but obsolete `Default` or re-point it to ctor defaults, so the two "default" concepts stop disagreeing. Obsolete first — re-pointing is breaking.
**Effort:** docs ~45 min; 4.0.0 alignment ~1 h + obsolete warnings.

### Done in this round (docs halves of H2/L1 already applied with the code work, plus H1/M4/M7)
- H1: name the 2-minute `ProcessTimeoutPolicy.Default` in `CliRun.cs` XML (the "a default value is applied" remark at ~41-43 + `timeoutTimeSpan` params) and document threshold behavior: graceful interrupt attempted → force-kill after a short grace period; with the default graceful config no exception is thrown — result comes back `Canceled=true`. Also `ProcessTimeoutPolicy.Default` property XML and a `CreateGraceful` remark.
- M4: GLOSSARY.md Decision 1 + `LocateFileFromDirectory` XML (`FilePathResolver.cs:176-178`): single-level directory fallback, recursion deliberately removed (over-broad-match/DoS surface); PATH-first ordering contract unchanged (no new ADR). Grep README + site/docs for stragglers.
- M7: converge docs on per-stream caps ("applied independently to standard output and to standard error"): `CliRun.cs:144-147` + twin ~186-189, `ProcessExitConfiguration.cs:119-123`, `WithMaxBufferedOutputBytes` (`ProcessExitConfigurationCreationExtensions.cs:92-99`).
- XML hazard warnings for H2 (Raw+redirection), L1 (FireAndForget redirection), M5 (Windows graceful best-effort) — document current reality, patch-safe.

---

## Deferred to 4.0.0 (summary)

| Item | Reason |
|---|---|
| H2 hard-fail on Raw+redirection | Only if normalization generates bug reports |
| M1 throw-by-default on cancellation | `CancellationThrowsException` knob already covers demand |
| M5 console-attach opt-in | Recommend never — host console-loss failure mode |
| L3a static `Equals` semantic flip | All eight types in one uniform change |
| L2 lazy resolution (no I/O at create) | Observable exception-move; needs unstarted wrapper state |
| L5 encoding-default change | Needs ADR-style note on which children break |
| L7(a) `Default` re-point/obsolete | Breaking; obsolete-first path |

No signature changes anywhere in the 3.1.x/3.2.0 batches; no new IVT grants, no reflection (AOT-safe throughout).

## Low label reference

| Label | One-liner |
|---|---|
| L1 | `FireAndForget` handle disposed immediately; redirected configs hang/broken-pipe silently |
| L2 | `IExternalProcess` creation does eager filesystem I/O; factory throws at create; double resolution |
| L3a | Static `Equals(null, null)` → false on both config types (and six more types) |
| L3b | Path comparison case-sensitive on Windows (**fixed in this round**) |
| L4 | `WaitForExitOrTimeoutAsync` XML promises buffered result, returns plain `ProcessResult` |
| L5 | UTF-8 default decodes OEM/ANSI child output into mojibake |
| L6 | Null env-var values silently dropped instead of clearing the variable |
| L7 | `Default` (suppresses) vs `new()` (allows exceptions) divergence; ±1s swallow window |
