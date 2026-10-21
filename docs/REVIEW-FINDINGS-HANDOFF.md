# CliInvoke Review Findings — Fix Handoff

Date: 2026-10-18 (updated 2026-10-19 after fix round 5: L6 Tier-2 non-breaking fix + empty-string env XML correction; fix round 4: L7(b); fix round 3: L1, L2 minimal, L7(a); fix round 2: M1, M3, M6)
Source: behavioral review of `src/` ("potentially surprising or unexpected behaviour"), severity-ranked, followed by per-finding fix proposals. Findings verified against the tree at 3.1.0.

**TL;DR: Fixed (verified): H1, M1, M2, M3, M4, M6, M7, L1, L2 (minimal), L3b, L6 (Tier-2, non-breaking), L7(a), L7(b), plus docs-only fixes M5, L3a, L4. Everything else in this document is proposed/not implemented.** Fixed items verified with the ConfigureAwait guard, `dotnet build src/CliInvoke.sln` (0 errors), and the `tests/CliInvoke.Tests` suite (682 total, 0 failed, 1 pre-existing skip) plus `tests/CliInvoke.Specializations.Tests` (14 total, 0 failed) as of fix round 5 (2026-10-19).

## Status board

| Finding | Severity | Status |
|---|---|---|
| H2 Raw-mode pipe deadlock | High | ❌ Not fixed — proposed (3.2.0) |
| M1 Raw vs Buffered cancellation divergence | Medium | ✅ **Fixed** — Buffered honors the documented cancellation contract (knob-gated absorb) |
| M2 `ProcessExitConfiguration` null-unsafe `==` | Medium | ✅ **Fixed** — null-aware operators |
| M3 retry middleware replays empty stdin on attempts ≥ 2 | Medium | ✅ **Fixed** — stdin rewound per retry attempt; non-seekable stdin throws |
| M4 "directory recursion" docs drift | Medium | ✅ **Fixed** — docs (GLOSSARY, XML, site/docs) |
| M5 Windows graceful exit degrades to kill | Medium | ✅ **Fixed** — docs (XML + site/docs troubleshooting/configuration) |
| M6 logging shows empty arguments for shell-wrapped runs | Medium | ✅ **Fixed** — logs joined ArgumentList when the argument string is empty |
| M7 `MaxBufferedOutputBytes` docs contradiction | Medium | ✅ **Fixed** — docs converge on per-stream |
| L1 `FireAndForget` disposal + redirection trap | Low | ✅ **Fixed** — fail-fast `ArgumentException` on redirected configs (ctor-default collision: the position-arg `ProcessConfiguration` ctors default `outputRedirection: true`) |
| L2 eager filesystem I/O in `ExternalProcess` ctor | Low | ✅ **Fixed (minimal)** — ctor-resolved `FileInfo` cached and reused by `Start`/`StartAsync`; lazy resolution still deferred |
| L3a static `Equals(null, null)` returns false | Low | ✅ **Fixed (docs)** — per-type XML remarks; semantic flip still deferred (4.0.0) |
| L3b case-sensitive path comparison on Windows | Low | ✅ **Fixed** — OS-aware comparison |
| L4 `WaitForExitOrTimeoutAsync` XML copy-paste bug | Low | ✅ **Fixed** — docs (XML returns) |
| L5 UTF-8 default vs OEM/ANSI child output | Low | ❌ Not fixed — proposed (3.1.x docs) |
| L6 null env-var values silently dropped | Low | ✅ **Fixed** (fix round 5) — BCL null-removes semantics in the adapter; XML + 3 tests; no signature/in-contract change; ships 3.2.0 |
| L7 "Default" divergence + ±1s swallow window | Low | ✅ **Fixed** — L7(a) `Default` re-pointed to ctor defaults; L7(b) swallow window gated on `OperationCanceledException` |

Repo gates for implementers: XML doc comments are **build errors** (CS1574/1580/1581/1584/1658/1734/1762 escalated in `Directory.Build.props`); style per `src/.editorconfig` (no `var` for built-ins, separate using groups, 100-col); GLOSSARY.md entries are contracts (PATH-first ordering is ADR-protected).

## Release batches (consolidated)

- **3.1.x — docs-only, zero behavior change:** H1, M4, M7 (**✅ done**) + L4, L5 docs, L3a doc notes + XML hazard notes for H2/L1/M5 (docs halves of those proposals). Theme: *make the contracts say what the code does.*
- **3.2.0 — behavior fixes, one release note ("Raw/FireAndForget pipe and cancellation contract alignment"):** H2 normalization, M1 Buffered cancellation (**✅ done**), L1 fail-fast (**✅ done**), M3 stdin replay (**✅ done**), M6 logging (**✅ done**), L2 minimal cache (**✅ done**), L6 null env-var semantics (**✅ done**), L7(b) OCE-gated swallow (**✅ done**). Theme: *fail fast or normalize, never silently hang.* Release note must also call out the L7(a) `Default` re-point (pulled forward from 4.0.0) — the DI `ProcessInvoker` null-fallback now allows unexpected-exit exceptions.
- **4.0.0 / deferred:** H2 hard-fail (only if normalization draws bug reports), M1 throw-by-default, M5 console-attach knob (recommend never), L3a semantic flip (all eight types in one change), L2 lazy resolution, L5 encoding-default decision.

---

## 3.2.0 proposals (code)

### H2 — Raw-mode pipe deadlock with `OutputRedirection=true`
**Fix:** In `ProcessInvocationPipeline.InvokeAsync` (`src/CliInvoke/ProcessInvocationPipeline.cs:44-100`), when `ctx.Mode == InvocationMode.Raw`, derive the config with `OutputRedirection` forced off before creating the `IExternalProcess` — same normalization the shell middleware already does via `Derive(cfg, ...)` (`src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs:84-86`). Pair with XML hazard notes on `CliRun.RunAsync(ProcessConfiguration)` (`CliRun.cs:101-115`), `ProcessInvoker.ExecuteAsync` (`ProcessInvoker.cs:92-114`), and `IExternalProcess.WaitForExitOrTimeoutAsync` (`IExternalProcess.cs:90-101` — direct `IExternalProcess` users bypass the pipeline; fixing that path means drain-and-discard readers inside `ExternalProcess`, docs-only is fine).
**Compat:** Technically observable (children see console handles instead of pipes in Raw mode) but nothing readable is lost — no public surface exposes Raw output. Ship in 3.2.0 with a release note.
**Alternative:** Fail fast — throw `InvalidOperationException` on Raw+redirection (clearest, breaks configs that "work" for quiet children today) → 4.0.0.
**Effort:** ~0.5 day incl. tests (chatty Raw child completes).

### M1 — Cancellation semantics differ between Raw and Buffered
**Status:** ✅ Fixed (2026-10-19, fix round 2) — drain-side OCE absorbed in `CaptureBufferedResultAsync` under the proposal's guards **plus one required addition**: the absorb is gated on `!ExitConfiguration.CancellationThrowsException`. Rationale: the wait fault and the drain OCE race to the `Task.WhenAll` join, and `wrapper.Canceled` is already set (reason persisted at `ProcessWrapper.cs:1044`) before `HandleCancellationExceptions` rethrows — so at catch time the proposal's three guards can hold while the wait task has not faulted yet, and the OCE would be swallowed even with the knob on. With the knob gate both contract paths are deterministic: absorb when the knob is false, propagate when true. The guarded `outputStrings.Result` read falls back to empty strings when the drain fault was absorbed (cancelled reads yield no partial strings). XML updated on `IExternalProcess.CaptureBufferedResultAsync`, `ExternalProcess.CaptureBufferedResultAsync`, and all three `CliRun.RunBufferedAsync` overloads. Tests: `tests/CliInvoke.Tests/Invokers/Cancellation/BufferedCancellationContractTests.cs` (buffered default → `Canceled` result, buffered knob=true → OCE, raw regression guard).
**Fix:** Fix **Buffered**, not Raw — the documented contract on `ProcessExitConfiguration.CancellationThrowsException=false` (`ProcessExitConfiguration.cs:90-103`) is a normal result with `Canceled=true`; Raw honors it, Buffered violates it (stream-read OCE escapes `Task.WhenAll`). Extend the catch filter at `src/CliInvoke/Processes/ExternalProcess.cs:367-377` to also absorb `outputStrings` faults when the fault is OCE **and** `cancellationToken.IsCancellationRequested` **and** `wrapper.Canceled` (same three guards as the existing stdin clause — never absorb genuine read failures). Cancelled reads yield no partial strings, so return the `BufferedProcessResult` with empty output, `Canceled=true`. Update XML on `CaptureBufferedResultAsync` (`IExternalProcess.cs:103-127`, `ExternalProcess.cs:312-338`) and `CliRun.RunBufferedAsync`; note that callers wanting OCE already have the opt-in (`CancellationThrowsException=true`).
**Compat:** Observable change, but toward the documented contract — bug fix. 3.2.0 with release note.
**Alternative:** Unify on throw-by-default (cleaner .NET citizenship, contradicts documented default) → 4.0.0.
**Effort:** ~0.5 day incl. cancellation tests on both modes.

### L1 — `FireAndForget(ProcessConfiguration)` disposal + redirection trap
**Status:** ✅ Fixed (2026-10-19, fix round 3) — `CliRun.FireAndForget(ProcessConfiguration)` throws `ArgumentException` (param `configuration`) when `OutputRedirection || RedirectStandardInput`, before any process handle is created. XML `<exception>` added and cross-references the string-args overload (which forces redirection off). **Discovered during implementation — important compat note:** the position-argument `ProcessConfiguration` constructors default `outputRedirection` to `true`, so `new ProcessConfiguration(path, args)` is itself a redirected config and now throws; callers must pass `outputRedirection: false` explicitly (the repo's own tests were updated accordingly). The string-args `FireAndForget` overload is unaffected. Tests: `tests/CliInvoke.Tests/Invokers/CliRunTests.cs` (redirected-output throw, redirected-stdin throw, non-redirected still starts, resolution failure still `FileNotFoundException`).
**Fix:** Fail fast in `CliRun.FireAndForget(ProcessConfiguration)` (`CliRun.cs:247-251`): throw `ArgumentException` when `configuration.OutputRedirection || configuration.RedirectStandardInput`. A redirected fire-and-forget is broken by construction (no one drains pipes; handle disposed immediately after `Start()`). Update the XML and cross-reference the string-args overload that forces redirection off (`CliRun.cs:261-270`).
**Compat:** Behavior change for callers passing redirected configs — they get silent breakage today. 3.2.0 with release note.
**Alternative:** Docs-only in 3.1.x, or silently force redirection off like the string overload (rejected — hides caller mistakes). Genuinely supporting redirected fire-and-forget needs background drain tasks outliving the call — complexity that earns nothing.
**Effort:** ~2 h incl. XML + tests.

### M3 — Retry middleware replays empty stdin on attempts ≥ 2
**Status:** ✅ Fixed (2026-10-19, fix round 2) — `RetryMiddleware.InvokeAsync` rewinds a seekable `StandardInput.BaseStream` (`Position = 0`) before every attempt after the first and throws `InvalidOperationException` for a non-seekable source (resource key `Exceptions.RetryMiddleware.NonSeekableStandardInput`). Spec deltas: the replay sentence is a class-level `<remarks>` covering all `UseRetryPolicy` overloads (no `UseRetry` method exists) and is also on `RetryOptions`. Tests: `tests/CliInvoke.Tests/Extensions/Middleware/Retry/RetryStdinReplayTests.cs` (MemoryStream + FileStream replay, non-seekable throw, no-throw paths, null-stdin no-op).
**Fix:** In `RetryMiddleware.InvokeAsync` (`src/CliInvoke/Extensions/Middleware/Retry/RetryMiddleware.cs:77-96`), before invoking `next(context)` on any attempt after the first, reset the stdin source: if `context.Configuration.StandardInput?.BaseStream` is `{ CanSeek: true }`, set `Position = 0`; if stdin is set but not seekable, throw `InvalidOperationException` at the moment a retry would occur ("retrying an invocation with a non-seekable standard input stream cannot replay the input"). `RetryMiddleware` is `internal sealed`, so the fix site is contained. Add one `<remarks>` sentence to `UseRetry`/`RetryOptions` docs: stdin is replayed per attempt and must be seekable (or null).
**Compat:** No API surface change; silent empty-stdin corruption becomes correct replay or a loud error. 3.2.0.
**Alternative:** Snapshot stdin into a `MemoryStream` on first attempt — copies unbounded payloads and spreads retry knowledge into the pipeline; rejected.
**Effort:** ~1-2 h incl. replay tests (FileStream + MemoryStream, non-seekable throw case).

### M6 — LoggingMiddleware logs empty arguments for ArgumentList/shell-wrapped runs
**Status:** ✅ Fixed (2026-10-19, fix round 2) — `LoggingMiddleware.InvokeAsync` computes the display string once (`Arguments` when non-whitespace, else `string.Join(" ", ArgumentList)`) and passes it through `Sanitize`; redaction still spans the joined string. Exit line untouched. Tests: `tests/CliInvoke.Tests/Middleware/Integration/LoggingArgumentListTests.cs` (joined list logged, redaction across the join, `Arguments` precedence).
**Fix:** In `LoggingMiddleware.cs:74`, compute the display string once: use `Configuration.Arguments` when non-whitespace, else `string.Join(" ", Configuration.ArgumentList)`; pass it through the existing `Sanitize` redactor (flag-then-value regex still matches across the join, so `["--password","hunter2"]` redacts correctly). Bonus: shell-wrapped invocations log the derived `ArgumentList` containing the quoted inner command — the truthful picture. Exit line untouched.
**Compat:** No public API change; log content only (current output is factually wrong). 3.2.0.
**Alternative:** Log both fields as structured properties — preserves raw fidelity but always logs an empty `Arguments` and doubles redaction calls; not worth it.
**Effort:** ~30-45 min incl. test asserting the ArgumentList case appears in logs.

### L2 — `ExternalProcess` constructor eagerly resolves the target file
**Status:** ✅ Fixed (minimal, 2026-10-19, fix round 3) — `ExternalProcess` caches the ctor-resolved `FileInfo` in a readonly field (`_resolvedFilePath`) and `Start()`/`StartAsync(CancellationToken)` reuse it instead of re-resolving; the `StartAsync(ProcessConfiguration, ct)` overload still resolves its *parameter's* target path (different config object, not a double resolution). Ctor XML now documents construct-time resolution and the `FileNotFoundException`. Construct-time-throw semantics preserved exactly; zero observable change.
**Fix (minimal, recommended):** Cache the `FileInfo` resolved in the constructor (`src/CliInvoke/Processes/ExternalProcess.cs:39`) and reuse it in `Start()` (:118) and `StartAsync()` (:167) instead of re-resolving. Three lines; closes the double-resolution/TOCTOU window and halves filesystem I/O; construct-time `FileNotFoundException` semantics preserved exactly.
**Compat:** Zero observable change. 3.2.0.
**Full fix (defer):** Lazy resolution — store only the resolver in the ctor, resolve on first `Start`/`StartAsync` so no I/O and no throw at create time. Moves an exception from ctor to Start (observable) → 4.0.0, and needs an "unstarted" `ProcessWrapper` state since `HasExited`/`HasStarted` touch `_processWrapper` pre-start. Do only if someone hits the construct-time-throw pain.
**Effort:** Minimal ~30 min; lazy ~2-3 h with lifecycle tests.

### L6 — `SetEnvironmentVariables` silently drops null-valued entries
**Status:** ✅ Fixed (2026-10-19, fix round 5, Tier-2 non-breaking behavior fix) — the `if (variable.Value is not null)` guard is gone; every entry is assigned (`process.StartInfo.Environment[variable.Key] = variable.Value`), so a null value flows to the BCL, which omits null-valued entries when the child's environment block is built: the variable is removed instead of falling back to the inherited parent value (`.NET 9` "Support for empty environment variables" semantics). **Non-breaking shape:** no signature/type change; null entries are unreachable for conformant callers (`IReadOnlyDictionary<string, string>` non-nullable values; `EnvironmentVariablesSpec`/builder still rejects nulls with `ArgumentNullException`, including via `ProcessConfigurationDerivation`), so in-contract behavior is identical — an edge-case behavior change only; 3.2.0 release note required. **Verified null-safe end to end:** `GetHashCode` (`HashCode.Add`) and `Equals` (`SequenceEqual`) tolerate null values. XML: `ProcessConfiguration.EnvironmentVariables` now states null=remove / empty=set-empty / builder-rejects; `FromProcessStartInfo` remarks document that source-side nulls (BCL removal intent) are *not* carried over — preserving them would require weakening the spec's documented `ArgumentNullException`, which was rejected as breaking-adjacent. Tests: 3 in `tests/CliInvoke.Tests/Processes/AdapterConfigurationDeliveryTests.cs` — null unit (`StartInfo.Environment` holds null over the inherited value), null e2e (child sees the variable absent: `echo-env` exit 1), empty e2e (present-but-empty: exit 0 with empty output) locking the empty≠remove contract.
**Fix:** In `BaseProcessControlAdapter.cs:239-243`, assign `process.StartInfo.Environment[variable.Key] = variable.Value!;` — BCL-documented semantics: a null value *removes* the variable from the child environment. Keep a code comment citing the Process docs semantics and update the `ProcessConfiguration.EnvironmentVariables` XML to state null-value behavior.
**Compat:** Edge-case change; previously-ignored null entries now clear inherited variables. Unobservable for conformant callers (nulls violate `IReadOnlyDictionary<string, string>` anyway). 3.2.0.
**Alternative:** Throw `ArgumentException` at the configuration boundary — surfaces violations loudly but adds validation the BCL doesn't have and breaks accidentally-working callers.
**Effort:** ~30 min incl. one test (null value → variable absent in child env).

### L7(b) — ±1s skew heuristic can swallow genuine failures
**Status:** ✅ Fixed (2026-10-19, fix round 4) — under `AllowExceptionsIfUnexpected` in the `Timeout`/`NotKnown` branch of `CancellationHelper.HandleCancellationExceptions`, the throw condition is now `exception is not OperationCanceledException || difference > TimeSpan.FromSeconds(1)`: non-cancellation exceptions always propagate regardless of timing; only OCEs are time-discriminated (swallowed when on-time). `SuppressExceptions` and `AllowExceptions` behaviour unchanged; `RequestedCancellation` branch untouched (outside the finding's scope); the `CancellationThrowsException` knob still overrides first. XML remarks rewritten to match. Tests: `tests/CliInvoke.Tests/Processes/CancellationHelperTests.cs` (7 tests — on-time OCE swallowed, off-window OCE thrown, IOException-near-timeout thrown on Timeout and NotKnown, SuppressExceptions still swallows, AllowExceptions still throws, requested-cancellation branch regression guard).
**Fix:** In `CancellationHelper.cs:98-108`, under `AllowExceptionsIfUnexpected`, gate the time-window swallow on exception type: only swallow `OperationCanceledException` raised within the window; rethrow all non-OCE exceptions regardless of timing (`if (exception is not OperationCanceledException || difference > TimeSpan.FromSeconds(1)) throw;`). Every OCE in this pipeline originates from CliInvoke's own cancellation machinery (timer or caller token), so on-time-timeout OCEs stay swallowed and intended behavior is preserved — while genuine I/O errors near the timeout can no longer vanish. The wall-clock heuristic alone is unsalvageable as a failure discriminator.
**Compat:** Internal method, behavior change only. 3.2.0.
**Alternative:** Narrow window to ±100 ms — still wall-clock gambling; rejected.
**Effort:** ~1 h incl. tests (OCE-near-timeout swallowed; IOException-near-timeout rethrown).

---

## 3.1.x proposals (docs-only)


### L5 — UTF-8 default decoding mojibakes OEM/ANSI child output
**Fix:** Docs-only now. XML remarks (`ProcessConfiguration.cs:276-303`) already state UTF-8; add a troubleshooting entry to `site/docs` (and a README note): children emitting Windows OEM/ANSI code-page text need explicit encodings — the builder API exists (see `RunnerConfigurationFactory.cs:131` for the pattern). Rationale for not flipping the default: UTF-8 is the correct modern default (pwsh 7, git, node emit it) and the BCL's own fallback (`Console.OutputEncoding`) breaks a different class of children — any default breaks someone; the fix is discoverability.
**Deferred (4.0.0):** default change (e.g., stdout/stderr UTF-8 + stdin `Console.InputEncoding`, or a `ProcessEncodings.Oem()`/`Ansi()` helper) — needs its own ADR-style note; `Console.OutputEncoding` can throw on non-console hosts and must not be captured in a static initializer.
**Effort:** docs ~1 h.


### L7(a) — two spellings of "default" `ProcessExitConfiguration` differ in exception behavior
**Status:** ✅ Fixed (code, 2026-10-19, fix round 3 — pulled forward from 4.0.0 by maintainer decision) — `ProcessExitConfiguration.Default` re-pointed from `CreateGraceful()` to `new ProcessExitConfiguration()`, so `Default` and the constructor default are now the same concept (`AllowExceptionsIfUnexpected`, `CancellationThrowsException=false`); `CreateGraceful()` keeps its suppressing semantics (`SuppressExceptions`) and is the documented contrast. XML updated on `Default`, both `CreateGraceful` overloads, the `?? CreateGraceful()` call sites (`CliRun.RunAsync`/`RunBufferedAsync(ProcessConfiguration, ...)`, `ExternalProcess` ctor), and the `?? ProcessExitConfiguration.Default` call sites (`ProcessInvoker.ExecuteAsync`/`ExecuteBufferedAsync`). **Observable change:** the DI `ProcessInvoker` null-fallback previously suppressed unexpected-exit exceptions (via the old graceful `Default`) and now allows them — callers wanting the old behavior pass `CreateGraceful()`. `CliRun`/`ExternalProcess` null-fallbacks are unchanged (still `CreateGraceful()`). Note for doc authors: crefs to the extension-block members `Default`/`CreateGraceful` do not resolve (CS1574) from outside — use `<c>` text or the containing-type cref for `CreateGraceful()`. Tests: `Default_MatchesConstructorDefaults`, `Default_DiffersFromCreateGraceful_InExceptionBehaviour` in `tests/CliInvoke.Tests/Core/Extensions/ProcessExitConfigurationCreationExtensionsTests.cs`.
**Fix (docs now):** XML remarks on `Default`, `CreateGraceful` (`src/CliInvoke.Core/Extensions/ProcessExitConfigurationCreationExtensions.cs:26-61`) and the `exitConfiguration ?? CreateGraceful()` call sites (`CliRun.cs:122/233`, `ExternalProcess.cs:41`) stating plainly: the graceful flavor suppresses unexpected-exit exceptions (`CancellationThrowsException=false`, `SuppressExceptions`); the ctor default allows them (`AllowExceptionsIfUnexpected`).
**Deferred (4.0.0):** ~~keep `CreateGraceful` semantics (the name signals intent) but obsolete `Default` or re-point it to ctor defaults~~ — the re-point happened in fix round 3 instead; nothing further deferred.

### Fix round 5 (2026-10-19): L6 (+ empty-string env XML correction)
- L6: `BaseProcessControlAdapter.SetEnvironmentVariables` drops the `is not null` guard and assigns every entry, giving null values BCL removal semantics (omitted from the child's environment block; inherited value can no longer leak). No signature change; unreachable for conformant callers. XML on `ProcessConfiguration.EnvironmentVariables` (null=remove / empty=set-empty / builder rejects nulls) and on `FromProcessStartInfo` (source nulls not carried over — lossy like the redirect-flag collapse). Three tests in `AdapterConfigurationDeliveryTests` (null unit, null e2e → exit 1, empty e2e → exit 0).
- Empty-string XML truth pass: `EnvironmentVariablesSpec.SetEnumerable`/`SetPair` param docs no longer claim empty string "clears" a variable (it sets an empty value; never removes — .NET 9 semantics). Non-XML stragglers deliberately left as-is (released text): `CHANGELOG.md` 3.1.0 entry, `CliInvoke.csproj` `PackageReleaseNotes`, one test comment.

### Fix round 4 (2026-10-19): L7(b)
- `CancellationHelper.HandleCancellationExceptions`: the `AllowExceptionsIfUnexpected` clause in the `Timeout`/`NotKnown` branch now throws when `exception is not OperationCanceledException || difference > TimeSpan.FromSeconds(1)` — non-OCE failures propagate even inside the ±1s window; only cancellation exceptions remain time-discriminated. XML remarks rewritten; new `CancellationHelperTests` (7 tests).

### Fix round 3 (2026-10-19): L1, L2 minimal, L7(a)
- L1: `FireAndForget(ProcessConfiguration)` fail-fast on `OutputRedirection || RedirectStandardInput` + `<exception>` XML warning that the position-arg `ProcessConfiguration` ctors default `outputRedirection: true` (so `outputRedirection: false` must be explicit). Repo tests updated to the non-redirected construction.
- L2: `ExternalProcess` caches the ctor-resolved `FileInfo` (`_resolvedFilePath`) and reuses it in `Start()`/`StartAsync(CancellationToken)`; ctor XML filled in (was empty stubs) documenting construct-time resolution.
- L7(a): `ProcessExitConfiguration.Default` re-pointed to `new ProcessExitConfiguration()`; XML contrast with `CreateGraceful()` added at the definitions and at every `?? CreateGraceful()` / `?? Default` call site (`CliRun`, `ExternalProcess`, `ProcessInvoker`).

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

No signature changes anywhere in the 3.1.x/3.2.0 batches (the L7(a) `Default` re-point and L1 fail-fast are behavior changes only, no signature changes); no new IVT grants, no reflection (AOT-safe throughout).

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
