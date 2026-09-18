---
name: process-to-cliinvoke-migration
description: >-
  Migrates System.Diagnostics.Process and ProcessStartInfo call sites to
  CliInvoke v3, mapping each call site to the right invocation pattern
  (CliRun, IProcessInvoker, or IExternalProcess). USE FOR replacing raw
  Process API usage in projects that reference CliInvoke v3 or when a
  CliInvoke migration is explicitly requested, including pre-install
  migrations. DO NOT USE FOR CliInvoke v2-to-v3 migrations, new CliInvoke
  code with no Process usage, or projects not adopting CliInvoke.
compatibility: Requires System.Diagnostics.Process usage in the source
  project; targets CliInvoke v3 packages (CliInvoke.Core, CliInvoke,
  CliInvoke.Specializations)
license: MIT
---

# Process to CliInvoke Migration

## When to Use

- When the project references a CliInvoke v3 package (CliInvoke.Core, CliInvoke, or CliInvoke.Specializations) and contains `System.Diagnostics.Process`/`ProcessStartInfo` usage.
- When the user explicitly asks to migrate `Process`/`ProcessStartInfo` code to CliInvoke, even before any CliInvoke package is installed.
- When a call site mixes legacy `Process` control flow (redirects, events, wait loops) with desired CliInvoke patterns.

## When Not to Use

- When migrating CliInvoke v2 code to v3, load `v2-to-v3-migration` instead.
- When writing new CliInvoke v3 code with no legacy `Process` usage, load `select-execution-pattern` to pick a pattern, then `generate-process-configuration` for builder flows.
- When the project is not adopting CliInvoke and the task is general `Process` hardening (timeout, kill logic) with no migration intent.

## Workflow

### Phase A: Inventory, Install, and Plan

1. **Check** CliInvoke package installation: search the project files (`*.csproj`) for `CliInvoke.Core`, `CliInvoke`, or `CliInvoke.Specializations` references. If none is referenced, load the `package-installation-choice` skill to select the right packages, then install them with `dotnet add package <PackageId>` (if the repo uses Central Package Management, add the version to `Directory.Packages.props` instead of setting `Version=` on the reference). Completion criterion: all projects being migrated carry the required CliInvoke package references, or existing references are confirmed sufficient.
2. **Scan** for `Process` and `ProcessStartInfo` usage: search target files for `Process.Start(`, `new ProcessStartInfo`, `new Process`, `: Process`, `ProcessStartInfo>` parameters, and `using System.Diagnostics;` (note: file may use `Process` without `using` if fully qualified). Completion criterion: a per-call-site inventory with `file:line` anchors exists.
3. **Classify** each call site into exactly one branch using the Classification table below. Apply the override rules first (DI container, middleware needed, credentials/policy), then the signal rules; ambiguous sites resolve to the highest-capability branch (interactive → buffered → RunAsync → FireAndForget). Completion criterion: every call site in the inventory carries a branch label.
4. **Map** each call site to the proposed CliInvoke v3 target from the Classification table, noting required API translation per site (argument list → `ArgumentList`, `WorkingDirectory` → `WorkingDirectory`, redirect flags → redirection spec, `UseShellExecute` → shell-exec handling, credentials → `UserCredentialSpec` via builder, event/loop constructs → pattern-native equivalents). For any API whose exact signature or behavior is uncertain, look it up per the Documentation Lookup section before mapping. Completion criterion: every call site carries a proposed target and a note listing each removed/renamed construct.
5. **Present** the migration plan to the user: per-call-site table (file:line, branch, current constructs, proposed CliInvoke API, risk notes). Completion criterion: the user replies with explicit confirmation or modification requests; iterate until confirmed.

### Phase B: Apply and Verify

6. **Apply** the confirmed plan file-by-file: construct `ProcessConfiguration` (init style; builder only for argument escaping, `UserCredentialSpec`, or resource-policy flows), replace each call site with its mapped target, and carry over disposal per the Disposal Mapping section. Completion criterion: all confirmed call sites are rewritten and the diff contains no remaining mapped `Process`/`ProcessStartInfo` constructs at those sites.
7. **Verify** with build: run `dotnet build <solution or project>`. If build fails, fix errors arising from the migration only (missing usings, wrong namespaces) and rebuild until clean. Completion criterion: build succeeds and re-running the Step 2 scan finds no unmigrated call sites (remaining `Process` usage is either intentionally kept or out of scope).

## Classification

| Signals at the call site | Branch | CliInvoke v3 target |
|---|---|---|
| DI container in use, middleware needed, `UserCredentialSpec`/resource-policy flows, or user asks for testability | Override | `IProcessInvoker` via DI (or `ProcessInvoker` + fluent `Use*` extensions) |
| Only `ProcessStartInfo` configuration, no start | Config-only | Init-style `ProcessConfiguration` construction |
| `Process.Start` for background execution; no output reading; result not required | Fire-and-forget | `CliRun.FireAndForget` |
| `Process.Start`; no output reading; exit code and process exit info required | No-output, result required | `CliRun.RunAsync` (string-args overloads) |
| `RedirectStandardOutput`/`Error` + `ReadToEnd` + `WaitForExit` (buffered) | Buffered | `CliRun.RunBufferedAsync` |
| `OutputDataReceived`, `StandardInput` writes, event-based or long-running, interactive | Interactive | `IExternalProcess` (via `IExternalProcessFactory`) |

Ambiguous call sites resolve to the highest-capability branch: interactive > buffered > RunAsync > FireAndForget.

**Method-name note**: `CliRun` exposes `RunAsync`, `RunBufferedAsync`, and `FireAndForget`; the `IProcessInvoker`/`ProcessInvoker` pattern exposes `ExecuteAsync` and `ExecuteBufferedAsync` instead. The `Run*`/`FireAndForget` names do not exist on `IProcessInvoker`. When the override row maps a call site to `IProcessInvoker`, translate the legacy call to the `Execute*Async` method that matches the branch's result shape (`RunAsync` → `ExecuteAsync`, `RunBufferedAsync` → `ExecuteBufferedAsync`).

## Disposal Mapping

- Only three types are disposable in CliInvoke v3: `IExternalProcess`, `UserCredential`, `UserCredentialSpec`.
- Legacy `using (var p = Process.Start(...))` maps to disposing the returned `IExternalProcess`; `ProcessResult` is returned un-disposed and disposal remains the caller's responsibility.
- `ProcessConfiguration` is not disposable; `StandardInput`/`UserCredential` placed inside it remain the caller's responsibility.

## Documentation Lookup

Do not rely on memory alone for CliInvoke APIs; verify against an authoritative source before writing migration code:

1. **Hosted docs portal**: https://alastairlundy.github.io/CliInvoke for the API reference and guides for the installed version.
2. **In-repo docs source**: the CliInvoke repo's `site/docs/` markdown (usage, migration guides, API docs pages).
3. **Repo-domain docs**: `README.md` (Resource Disposal section, invocation-pattern overview), `DESIGN_PATTERNS.md` (pattern decision tree), `GLOSSARY.md` (domain conventions such as `Get*`/`Enumerate*` naming and PATH-lookup ordering).
4. **XML doc comments**: the public API of the installed `CliInvoke.Core`/`CliInvoke` NuGet packages ships XML documentation; consult the packages via IntelliSense/decompiler for exact signatures.

When the hosted portal and the local packages disagree (version skew), trust the XML docs of the actually referenced package version and note the discrepancy in the migration plan.

## Transitions

- `package-installation-choice`: load in Workflow Step 1 when no CliInvoke package is referenced and the correct package set (Library vs App, Abstractions vs Implementation) must be selected before installing.
- `select-execution-pattern`: load when the branch choice is contested or the user wants the trade-off rationale (DI, testability, middleware) before committing.
- `v2-to-v3-migration`: load when the same project also contains v2-style CliInvoke code (e.g., `ProcessConfigurationFactory`, `ArgumentsList`, `CliRun.UseExternalProcessFactory`).
- `generate-process-configuration`: load when the migration needs `ProcessConfigurationBuilder` for argument escaping, `UserCredentialSpec`, or resource-policy callback flows.

This is a pure knowledge skill and does not invoke external tools.

## Validation

- [ ] Package check (Workflow Step 1) resolved: required CliInvoke package references exist (or were added respecting Central Package Management) before any call-site edit.
- [ ] The scan (Workflow Step 2) found every call site: inventory has `file:line` anchors and re-running the scan finds nothing new.
- [ ] Every call site in the inventory carries exactly one branch label; no site is unlabeled or double-labeled.
- [ ] Every call site carries a proposed CliInvoke v3 target and a per-site note listing each removed/renamed construct.
- [ ] No-output call sites requiring exit info map to `CliRun.RunAsync`; background no-result sites map to `CliRun.FireAndForget`. The two are not conflated.
- [ ] Every CliInvoke API used in the applied migration was verified against Documentation Lookup sources; none invented from memory.
- [ ] The user explicitly confirmed the Phase A plan before any edit (Phase B) was made.
- [ ] `ProcessConfiguration` uses init-style construction; `ProcessConfigurationBuilder` appears only for argument escaping, `UserCredentialSpec`, or resource-policy flows.
- [ ] After Phase B, re-running the scan finds no mapped `Process`/`ProcessStartInfo` constructs at confirmed sites.
- [ ] `dotnet build` succeeds after Phase B.
- [ ] No new disposable types beyond `IExternalProcess`, `UserCredential`, `UserCredentialSpec` were introduced; disposal follows the Disposal Mapping.
- [ ] Where the user requested testability, DI, or middleware, the chosen target is `IProcessInvoker` (+ `Use*` extensions), not `CliRun`, and the rewritten calls use `ExecuteAsync`/`ExecuteBufferedAsync` (not `Run*Async`/`FireAndForget`).
- [ ] Remaining `Process` usage after migration is either intentionally kept (user confirmed) or out of scope; none silently dropped.
