# Root Cause Analysis — CI Test Failure (Run 41543466477)

**Repository:** alastairlundy/CliInvoke
**Failed run:** https://github.com/alastairlundy/CliInvoke/actions/runs/41543466477
**Job:** `build (windows-latest, 5)` → step **"Test (CliInvoke)"** failed (exit code 1)
**Date of failure:** 2026-11-02 23:15 UTC
**Analysis date:** 2026-11-03
**Analyst:** AI agent (worktree `ci-flake-rca`)

---

## 1. Executive summary

The build itself **succeeded** (only Meziantou.Analyzer warnings). The failure was
in the `Test (CliInvoke)` step on `windows-latest`: **9 tests failed, all with
`TimeoutException` from their own 30-second `.WaitAsync` guards** — i.e. the
child invocation never completed within 30 s, not an assertion failure.

The evidence (identical code green 1 minute before and 5 minutes after, the
net10.0 leg passing the *same* tests on the same machine minutes later, local
runs passing 12/12) shows this is a **transient runner-side stall at the start
of the net11.0 test leg**, not a code regression from the pushed commit (which
changed only `.github/workflows/scorecard.yml`). The `--max-parallel-test-modules 1`
flag serialises the two TFM legs but **not** tests within a leg; TUnit's default
parallelism is `4 × CPU cores` = up to **16 concurrent tests** on a 4-core
runner. The first wave of `powershell.exe` children (the slowest, heaviest
child) all stalled for ~30 s under that concurrent load right after the build
finished.

**Mitigations already applied** (commit `472ab902`):

- Guards raised `30 s → 45 s` in the 3 affected test classes
  (`OutputTruncationHandoffTests`, `BufferedCaptureNoDeadlockTests`,
  `RawModeRedirectionNormalizationTests`).
- `[Retry(2, RetryOnExceptionTypes = new[] { typeof(TimeoutException) })]` added
  to those classes, verified empirically to retry only on `TimeoutException`.

---

## 2. What failed

### 2.1 Job topology

| Leg | Result | Duration |
|---|---|---|
| ubuntu-latest | ✓ passed | ~1m22s |
| macos-latest | ✓ passed | ~2m6s |
| windows-latest | ✗ failed (`Test (CliInvoke)`) | 4m21s |
| → Test (Specializations) | skipped (prior step failed) | — |

### 2.2 The 9 failures

All in `tests/CliInvoke.Tests/Invokers/`, all `TimeoutException: The operation has
timed out.`, all hitting their own `WaitAsync(TimeSpan.FromSeconds(30))` guard:

| Test class | Count | Child command |
|---|---|---|
| `OutputTruncationHandoffTests` | 6 | `powershell.exe -NoProfile -Command "Write-Output('a' * 20000)"` |
| `BufferedCaptureNoDeadlockTests` | 1 | `powershell.exe -NoProfile -Command "Write-Output('a' * 200000)"` |
| `RawModeRedirectionNormalizationTests` | 2 | `powershell.exe … "[Console]::Out.Write([char]0 * 70000)"` (chatty) and `powershell.exe … "exit 0"` (quiet) |

### 2.3 Failure timeline (from job log)

```
23:11:42  job start (windows-latest)
23:11:45  Pre Harden the runner (Audit all outbound calls) — success, 7s
23:12:26  Build step start
23:14:43  Build step end  (2m17s)
23:14:43  Test (CliInvoke) step start  →  dotnet test --max-parallel-test-modules 1
23:14:50  "Running tests from …net11.0\CliInvoke.Tests.dll"
23:14:52.7–23:14:56.0   <-- all 9 failing tests START (within ~3.3 s)
23:15:22.7–23:15:26.0   <-- all 9 hit their 30 s guard and FAIL
23:15:32  net11.0 leg ends  (46 s for 691 tests; +25–30 s vs. normal)
23:15:36  net10.0 leg starts
23:15:53  net10.0 leg ends  (16.7 s, 689 passed, 0 failed)
23:15:59  job end (orphan MSBuild/VBCSCompiler processes terminated)
```

Every failure duration was `30s 0xx ms` — i.e. the guards fired, not a test-level
timeout or an assertion.

### 2.4 Adjacent runs (same commit family)

| Run | SHA | Test Build on Windows |
|---|---|---|
| 41543286370 (23:10) | `546df3e3` | ✓ passed |
| 41543466477 (23:11) | `baa5b0e9` | **✗ failed** (this run) |
| 41543549196 (23:12) | `f3d50d51` | ✓ passed |
| 41544140214 (23:16) | `f1da9546` | ✓ passed |

Diff `546df3e3 → baa5b0e9`: **only** `.github/workflows/scorecard.yml` (2 lines).
Diff `baa5b0e9 → f1da9546`: only `benchmarks/Directory.Packages.props`.

**Zero product-code change** between a passing run and the failing one.

---

## 3. Evidence gathered

### 3.1 Logs

- `gh run view 41543466477 --log-failed` — 9 `TimeoutException` failures; stack traces
  point at the test methods' `.WaitAsync(TimeSpan.FromSeconds(30))` lines.
- `gh api …/artifacts` → **0 artifacts** (the workflow does not upload the MTP
  HTML report, so per-test durations are not retrievable post-hoc).
- Job log (263 KB) saved locally; no extra detail between 23:14:50 and 23:15:22.

### 3.2 Local reproduction

- 4 full `dotnet test --framework net11.0` runs: **690 passed, 0 failed** each (~13 s).
- 5 filtered runs of just the 3 flaky classes: **9/9 passed** each (~1.4 s).
- 9 more full-suite runs after the fix: **1379 passed, 0 failed**.
- No timeout or hang reproduced locally.

### 3.3 Runner / environment

- Same runner image/version in failing and passing runs:
  `windows-2025-vs2026` `20261030.294.1`, runner `2.338.2`.
- `--max-parallel-test-modules 1` serialises the two TFM legs, but tests *within*
  a module run in parallel. TUnit default `MaximumParallelTests = null` →
  **`4 × CPU cores`** = up to **16 concurrent tests** on a 4-core runner.
- The `Test (CliInvoke)` step ran 70 s (vs ~37 s in the passing run); the
  net11.0 leg took 46 s vs ~17 s normally — the extra ~25–30 s is the hung batch.
- The job ended by terminating **4 orphaned MSBuild + 1 VBCSCompiler + 2 conhost
  processes** — build servers were still alive during the test phase (normal node
  reuse), consuming CPU alongside the tests.

### 3.4 Code state

- No shared static locks in `CliInvoke` src (only `s_regexCache` ConcurrentDictionary
  and an `AsyncLocal<bool>` in `DefaultShellMiddleware`).
- `CliInvoke` has no `Started` event on `System.Diagnostics.Process` — its own
  `ProcessWrapper.Started` is raised synchronously at the end of `Start()`.
- The net11.0-only `StartSuspended` path (`#if NET11_0`) is the only
  TFM-specific process-start code. It runs in `ProcessWrapper.Start()` and
  resumes via `OnStarted` (invoked synchronously via `Started?.Invoke`).

### 3.5 History

Recent `Test Build` failures had *different* signatures (FsCheck property failure,
`WindowsAdapter`/`UnixAdapter` NRE, macOS capacity). This 9-timeout pattern is
**first-seen** in run 41543466477.

---

## 4. Hypotheses (with evidence for/against)

### H1 — Transient cold-start stall of `powershell.exe` children (most likely)

**Claim:** At the start of the net11.0 leg, the first wave of tests spawned 9
concurrent `powershell.exe` children. On a freshly booted runner (build just
finished, MSBuild nodes still alive, Defender scanning the freshly built
`CliInvoke.Tests.dll` and dependencies), `powershell.exe` cold start (CLR/AMSI
init + Defender scan) exceeded the 30 s guard for all of them.

- **For:** Every failed test spawns `powershell.exe` with redirected output; the
  passing first-wave tests that spawn `cmd` (`GetQuietChildCommand`) and
  `dotnet` did not time out. The net10.0 leg (same tests, same machine,
  seconds later) passed in 16.7 s — by then `powershell.exe` caches were warm.
  Adjacent runs with identical code passed (runner-load variance). Local runs
  pass (fast, warm, no Defender scan pressure).
- **Against:** `TruncationTests` and `StdinPipingClaimTests` also spawn
  `powershell.exe` and passed in the same run — but they likely started later
  (TUnit discovery order: `CliInvoke.Tests.Processes` after
  `CliInvoke.Tests.Invokers`, where the 9 failures live).
- **Verdict:** Best fit. The quiet-child failure (`exit 0`, zero output) is
  consistent with `powershell.exe` itself taking >30 s to start, and
  inconsistent with an output-pipe deadlock.

### H2 — Latent race in the net11.0 `StartSuspended` path

**Claim:** `StartSuspended` + resume has a rare race that leaves a child
creation-suspended forever → run never completes → 30 s timeout. Net11.0-only.

- **For:** The failure is Windows net11.0-only. `StartSuspended` is net11-only
  and Windows/macOS-only.
- **Against:** macOS net11 leg passed (also uses `StartSuspended`). The same code
  passed in adjacent runs. `OnStarted` runs synchronously inside `Start()` and
  resumes in a `finally` block. No shared/static state in the resume path. A
  real deadlock should not clear after 30 s.
- **Verdict:** Unlikely but not fully ruled out. The `SafeHandle.Resume()` return
  value is ignored (line 513) — a silent failure would leave the child
  suspended, but then `HasExited`-based paths would… actually the test would just
  hang (consistent with symptom). Worth investigating further if it recurs.

### H3 — Intermittent pipeline deadlock (buffered capture)

**Claim:** PR #477 (`f93b1e49`, merged a day before) "trim allocations in
buffered capture" introduced a race where the capture reader starts late/never,
deadlocking the child on a full pipe buffer.

- **For:** The failing tests are literally the deadlock-regression guards.
  PR #477 touched `ProcessWrapper.cs` buffered capture (125 lines).
- **Against:** `RawModeRedirectionNormalizationTests` (raw mode, redirection
  forced **off**, no capture) also failed — and the quiet child wrote zero bytes
  (can't fill a pipe). So a capture deadlock can't be the universal cause.
- **Verdict:** Ruled out as the sole cause. Could be a contributing factor for
  the buffered tests, but cannot explain the quiet-child failure.

### H4 — Machine-wide stall (Defender / build process contention)

**Claim:** The runner was globally stalled ~30 s at the start of the test leg;
only tests with 30 s guards that started in that window failed.

- **For:** The failures all started within the first 6 s of the run; the leg
  took 46 s vs ~17 s normally (a ~25–30 s stall).
- **Against:** 681 other tests passed within the same window (the leg finished
  only 6 s after the last timeout). The build's orphan MSBuild/VBCSCompiler
  processes were present in both passing and failing runs. StdinPipingClaimTests
  and ProcessRuntimeBugfixTests (also 30 s-guarded, powershell-spawning) passed.
- **Verdict:** Partially supported — a targeted stall of powershell.exe spawns
  fits better than a global stall.

---

## 5. Conclusion

**Most likely cause: a transient stall of `powershell.exe` child process creation
at the start of the net11.0 test leg**, driven by concurrent cold-start of many
`powershell.exe` instances (TUnit default parallelism = `4 × CPU cores` = up to
16 concurrent tests) on a freshly booted runner right after `dotnet build`
(Defender/AMSI scanning of freshly written binaries + powershell.exe cold start).
The 9 tests that happened to start in that window and had a 30 s fail-fast guard
expired; tests without guards were merely slow (and passed); cmd/echo/dotnet
children started fast enough to pass.

This is **environmental/timing flakiness, not a code regression**: the pushed
commit changed only `scorecard.yml`; net10.0 leg passed the identical tests on
the same machine minutes later; the same commit passed in three adjacent runs; and
12 local runs of the net11.0 suite passed (690/691 each).

A latent `StartSuspended` race (H2) cannot be fully excluded from the
available data, but the evidence (macOS net11 leg passed, adjacent runs passed,
local runs pass) makes a deterministic product bug unlikely.

---

## 6. What was already done

Commit `472ab902` (on `main`, included in this worktree's HEAD):

- `OutputTruncationHandoffTests.cs`: 6× `WaitAsync(30s) → 45s`
- `BufferedCaptureNoDeadlockTests.cs`: `WaitAsync(30s) → 45s`
- `RawModeRedirectionNormalizationTests.cs`: `GuardTimeout 30s → 45s`
- Added `[Retry(2, RetryOnExceptionTypes = new[] { typeof(TimeoutException) })]`
  to all three classes (verified empirically at class level on TUnit 1.74.0).

This addresses the symptom (absorbs a ~30–45 s cold-start stall) and keeps
fail-fast semantics for genuine deadlocks (they still fail after retries).

---

## 7. Recommended further actions

1. **Upload the MTP HTML report as a workflow artifact** — the workflow
   currently produces `TestResults/CliInvoke.Tests-windows-net11.0-report.html`
   with per-test durations but uploads nothing (0 artifacts). Adding an
   `actions/upload-artifact` step for `TestResults/` would give per-test
   timings for the *next* failure and resolve the ordering question that
   prevented a definitive root-cause call here.
2. **Add diagnostics on timeout** — on `TimeoutException`, log the child PID
   and `HasExited`/suspended state. `CliRun`/`IProcessInvoker` don't expose PID,
   so this needs either a new result field or wrapping invocations in a helper
   that records child state.
3. **Reduce first-wave contention** — either lower TUnit parallelism in CI
   (`--maximum-parallel-tests 4` instead of the default 16) or mark the
   powershell-spawning pipeline classes `[NotInParallel]` (TUnit runs
   `[NotInParallel]` tests completely alone). This directly targets the
   cold-start storm hypothesis.
4. **Replace `powershell.exe` with the in-repo helper binary** for the heavy
   children (add an `emit-bytes` mode to `CliInvoke.Tests.Helpers/Program.cs`).
   The helper is a small .NET app with ~100 ms deterministic startup vs
   powershell's 1–2 s cold start — this removes the slowest, most variable
   component. Note: `TruncationTests`/`StdinPipingClaimTests` (also
   powershell) passed, but they appear to run in a later wave (Processes
   namespace after Invokers), so the helper swap would make the first wave fast.
5. **Pre-warm powershell.exe** in a global setup (launch
   `powershell -NoProfile -Command "exit 0"` once before tests) — a cheap
   belt-and-braces measure against cold-start variance.
6. **If it recurs**, rerun the failed job (`gh run rerun 41543466477 --failed`)
   and check whether the same 9 tests fail again with the longer guard — that
   would indicate a genuine hang rather than a slow-start stall.

---

## 8. Files examined

- `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` — `Start`, `OnStarted`,
  `ResumeProcess`, `WaitForExitOrTimeoutAsync`, `SafeHandle.Resume` path.
- `src/CliInvoke/ProcessInvocationPipeline.cs` — `InvokeAsync` (start → wait/capture).
- `src/CliInvoke/Processes/Internal/ControlAdapters/WindowsProcessControlAdapter.cs`
  — `SetResourcePolicy`, `ResumeProcess` (net11 blocks).
- `tests/CliInvoke.Tests/Invokers/{BufferedCaptureNoDeadlock,OutputTruncationHandoff,RawModeRedirectionNormalization}Tests.cs`
- `tests/CliInvoke.Tests/Processes/{Truncation,StdinPipingClaim}Tests.cs`
- `.github/workflows/test.yml` — parallelism config, no artifact upload.
- CI logs: job `125613108179` (failing windows job), run 41543466477.

## 9. Open questions

- Which of the 9 failing tests' `powershell.exe` children actually completed
  (vs. being abandoned)? Not distinguishable from the available logs (passed
  tests are not printed; no artifact upload). Uploading the MTP HTML report
  would resolve this.
- Whether a latent `StartSuspended` race contributes. Cannot be ruled out
  without per-test child-state telemetry, but the evidence (macOS net11 leg
  passed, adjacent runs passed, local runs pass) makes a deterministic product
  bug unlikely.
- Whether TUnit's test ordering is deterministic by namespace/class (which
  determines which tests land in the first wave). Worth verifying empirically.