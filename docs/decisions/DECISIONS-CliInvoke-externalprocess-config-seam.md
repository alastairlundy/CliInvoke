# Decision Ledger — CliInvoke ExternalProcess config seam

## Session goal

### [D001] — session goal

- **Driver**: the user wants a fix for the ExternalProcess configuration-mutation problem that is simple, intuitive, and still gives callers accurate resolved file-path info in results.
- **Resolved Answer**: "I want the solution to the external process configuration mutation problem to be simple, make sense, whilst giving the user accurate file path info when they receive the results."
- **Normalized Requirement**: The fix shall stop ExternalProcess from mutating the caller's ProcessConfiguration in place, while keeping the resolved file path accurately available to the caller (via the results or an equivalent non-mutating surface), and the change shall be simple and intuitive.
- **Constraints**: Must be simple; must make sense to users; caller must receive accurate file path info in results.

### [I001] — nature of the mutation objection

- **Prompt**: "Is your issue/problem with the Configuration mutation a problem of mechanics (don't allow internal/non-public setting of the configuration's file path since it's not allowed publicly, but other means of achieving the same outcome are fine via a different means) or a problem of philosophy (don't update an input the user has sent after it's been configured by the user, even if the file path needs resolving properly)?"
- **User Response**: "I can see both arguments. I am definitely at least concerned with the mechanics of it. The philosophy also makes sense but I'm unsure if users will be happy with the philosophy choice."
- **Resolution**: Resolved in conjunction with D002/D003 — both branches converge on removing the internal setter; the mechanics-vs-philosophy split decides only the downstream follow-up (middleware vs docs).
- **Notes**: User later clarified that the philosophy branch also removes the mutation (by removing the setter and updating call sites), not "document the existing mutation." The earlier options A/B/C in D002 are superseded by the user's remove-setter plan.

### [D002] — remove internal TargetFilePath setter; stop mutating caller's Configuration

- **Driver**: the user wants the mutation gone, and identified removing the internal `TargetFilePath` setter on `ProcessConfiguration` as the mechanism that makes it impossible; the resolved path is still needed accurately in results.
- **Resolved Answer**: "Regardless of motivation I'd suggest removing the internal setter for the TargetFilePath... first." (Followed by: "by removing the setter and updating the call sites the mutation would be removed.")
- **Normalized Requirement**: `ProcessConfiguration.TargetFilePath` shall not have an internal (or otherwise non-public) setter that allows `ExternalProcess` to rewrite the caller's instance. `ExternalProcess.Start()` and `StartAsync()` shall resolve the file path via the `IFilePathResolver` and apply the resolved path to the `ProcessWrapper` directly (so `result.FileName` / `StartInfo.FileName` remains accurate), without writing back to `Configuration.TargetFilePath`.
- **Constraints**: Specializations (e.g., PowerShell, Cmd) locate the executable at runtime, not at configuration time; removing the internal setter removes the mechanism they currently use to surface a runtime-resolved path, so an alternative legitimate path is required for them (see D004). The change must be simple and must not break the result's accurate file-path info.

### [D003] — conditional follow-up: middleware (mechanics) vs documentation (philosophy)

- **Driver**: the user accepts both motivations but is uncertain which downstream follow-up will satisfy library users; the choice shapes whether the fix lands as a runtime mechanism or as a documented contract.
- **Resolved Answer**: "Option B - We're not mutating the configuration after it's been sent. We need to document this behaviour so the user is conscious of it."
- **Normalized Requirement**: The code documentation on `ProcessConfiguration` (and `ExternalProcess.Start` / `StartAsync`) and the external documentation (README / site docs / ADRs as applicable) shall explicitly state that `ProcessConfiguration` is **not** mutated after the user submits it, including the resolved `TargetFilePath`. Users shall be pointed to the `ProcessResult` / `BufferedProcessResult` / `PipedProcessResult` for the accurate resolved file path.
- **Constraints**: No runtime update mechanism is sanctioned (I002: Option A's middleware contradicts the philosophy). Docs must be simple, clear, and visible at the call site (XML doc on `Configuration` / `Start` / `StartAsync`) and in external docs. Must not reintroduce in-place mutation of the caller's `Configuration`.

### [D004] — Specializations alternative path (runtime executable resolution)

- **Driver**: removing the internal `TargetFilePath` setter (D002) closes the door that the `CliInvoke.Specializations` (PowerShell, Cmd, etc.) currently use to surface a runtime-resolved executable back into the configuration; without an alternative, D002 cannot ship.
- **Resolved Answer**: "The other way to resolve the issue in Specializations is just provide the known executable name per platform (for PowerShell use pwsh.exe on Windows and pwsh on Unix, for Cmd use cmd.exe on Windows) and rely on the the file path resolution inside ExternalProcess to find the executable file path."
- **Normalized Requirement**: `CliInvoke.Specializations` shall provide the executable *name* per platform via the `ProcessConfiguration` constructor (e.g. `pwsh.exe`/`pwsh`, `cmd.exe`). `ExternalProcess` shall resolve the name at Start time via `IFilePathResolver` and apply the resolved path to `ProcessWrapper` so the result carries the accurate path. `ProcessConfiguration.TargetFilePath` shall not be writable post-construction (init-only is the preferred visibility); the caller's `Configuration` instance shall not be mutated.
- **Constraints**: The Specializations' `IFilePathResolver` injection in their constructors becomes unnecessary and should be removed as part of the change (eliminates the double resolution). `init`-only is preferred over get-only so object-initializer syntax (`new ProcessConfiguration { TargetFilePath = ... }`) continues to work. The mechanism must be simple, must integrate with D002/D003, and must not re-introduce any setter that allows post-construction writes.

### [I002] — Option A conflicts with the philosophy motivation

- **Prompt**: "Doesn't Option A violate the Philosophy approach that spawned this improvement candidate?"
- **User Response**: (challenge — Option A's "validates/updates ProcessConfiguration" mechanism contradicts the philosophy of not updating the user's input after configuration, even when producing a new instance.)
- **Resolution**: D003 recommendation shifted from A to B. Option A reframed as mechanics-only. The options table remains valid but the recommendation must respect the philosophy motivation that drove the candidate.
- **Notes**: Producing a new ProcessConfiguration with updated fields is still the system updating what the user configured, which violates the philosophy regardless of reference sharing. C does not resolve the tension because it still ships A.

### [T001] — Resolution timing: always resolve at Start

- **Driver**: eliminate redundant resolution while keeping timing predictable; confirmed `FilePathResolver.ResolveFilePath` short-circuits rooted paths (`FilePathResolver.cs:37-40`: `if (Path.IsPathRooted(filePathToResolve)) return new FileInfo(filePathToResolve);`), so always-resolve is cheap.
- **Resolved Answer**: "I agree with Option A in principle (though I'm sure @src/CliInvoke/FilePathResolver.cs actually special cases fully resolved file paths and returns quickly in those cases) but the location of where it's resolved determines whether ProcessWrapper needs a new overload or if the existing ctor needs replacing."
- **Normalized Requirement**: `ExternalProcess.Start()` and `StartAsync()` shall always call `_filePathResolver.ResolveFilePath(Configuration.TargetFilePath)`. The `IFilePathResolver` contract shall treat rooted paths as a fast-path (`Path.IsPathRooted` → return `new FileInfo(path)`), making the always-resolve pattern effectively free for absolute inputs.
- **Constraints**: Resolution location (Start) couples to T002 — the resolved path must reach `ProcessWrapper`. No write-back to `Configuration.TargetFilePath` (D002).

### [T002] — ProcessWrapper ctor shape: take resolved path (param or bag)

- **Driver**: the resolved path must enter `ProcessWrapper` without creating a dead/niche overload (Option A in the prior round was rejected for that reason) and without contradicting T001.
- **Resolved Answer**: "Option B. Follow up with a new TDP on the Param vs Option bag question."
- **Normalized Requirement**: The `ProcessWrapper` constructor shall be updated to accept the resolved file path via a new parameter or an options bag/struct. `ExternalProcess` (per T001) shall resolve the path at Start and pass it. The existing ctor shall not be left as a dead or niche-only overload.
- **Constraints**: Sub-choice of new-parameter vs options-bag is deferred to T003. Consistent with T001 (resolution lives in `ExternalProcess`). No mutation of `Configuration`.

### [T003] — ProcessWrapper ctor parameter shape: `FileInfo resolvedFilePath`; drop vestigial policy param

- **Driver**: T002 left param vs bag open; user refined Option A to preserve resolution integrity and drop the redundant policy parameter.
- **Resolved Answer**: "Option A with some tweaks: the field should be a FileInfo object to maintain the argument and resolution integrity. IFilePathResolver returns a FileInfo object. This simplifies transferring it safely to the ProcessWrapper where it can be applied. If the current ctor takes a ResourcePolicy separately then that needs removing as a separate parameter"
- **Normalized Requirement**: The `ProcessWrapper` constructor signature shall be updated to `(ProcessConfiguration configuration, FileInfo resolvedFilePath)`. The separate `ProcessResourcePolicy` parameter shall be removed (vestigial — `Configuration` owns `ResourcePolicy`). The resolved `FileInfo` is supplied by `ExternalProcess` (per T001) and applied to `StartInfo.FileName`.
- **Constraints**: `FileInfo` preserves the resolver's return type and resolution integrity end-to-end. Removing the policy param updates call sites: `ExternalProcess` ctors and `Start()` no longer pass `configuration.ResourcePolicy` separately. Must remain consistent with T001 (resolution in `ExternalProcess`).

### [I003] — init-only mechanics: caller can still initialize via object initializer

- **Prompt**: "Would users be able to still initialize it themselves under the ``init`` approach if the ctor assigns a value to it passed via parameters?"
- **User Response**: (clarification question — TBD confirmation)
- **Resolution**: Confirmed `init`-only allows both ctor-parameter assignment and object-initializer assignment during the initialization phase; only post-construction assignment is forbidden. This preserves caller ergonomics and aligns with D004's preference.
- **Notes**: C# `init` accessor is callable from object initializers (which run after the ctor body as part of initialization). Object initializer overrides ctor-assigned value. `config.TargetFilePath = "..."` after construction will not compile.

### [T004] — TargetFilePath setter visibility: `init`-only

- **Driver**: prevent post-construction mutation of `TargetFilePath` while preserving caller ergonomics (object initializer + ctor param).
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: `ProcessConfiguration.TargetFilePath` shall be declared `{ get; init; }`. The `init` accessor is callable from constructors and object initializers; post-construction assignment shall not compile. This enforces the D002 no-mutation contract at the type system level.
- **Constraints**: Breaking change for external consumers who assign `TargetFilePath` post-construction (intended). `init` is callable from Specializations' constructors, so their pattern continues to work. Aligns with D004's stated preference.

### [T005] — Test strategy for no-mutation contract: unit test on ExternalProcess

- **Driver**: enforce D002's no-mutation contract with an automated, maintainable test (D001).
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: A unit test on `ExternalProcess` shall assert that `Configuration.TargetFilePath` is unchanged after `Start()` and `StartAsync()`, and that the result's `FileName` reflects the resolved path. The test shall be CI-enforceable.
- **Constraints**: Test must avoid platform-specific filesystem quirks. One new test with a stub process target. Complements T004's compile-time enforcement (type system + runtime test).

### [T006] — Specializations cleanup: drop IFilePathResolver from PowershellProcessConfiguration ctor

- **Driver**: with T001 fixing resolution in `ExternalProcess`, the resolver injection in the PowerShell specialization is dead weight and contradicts D004's "name per platform" mechanism.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: `PowershellProcessConfiguration`'s constructor shall no longer accept an `IFilePathResolver` parameter. The ctor shall only take the inputs it actually needs (arguments, options). Resolution is handled entirely by `ExternalProcess` per T001. `PowershellProcessInvoker` and `PowerShellMiddleware` call sites shall be updated accordingly. `CmdProcessConfiguration` already conforms.
- **Constraints**: Breaking change for any direct constructor caller passing a resolver (intended). Eliminates the double-resolution path. Aligns with D004 and T001.

### [T007] — ProcessWrapper StartInfo.FileName plumbing

- **Driver**: Resolving in `ExternalProcess` and applying the resolved `FileInfo` locally keeps `BaseProcessControlAdapter` focused on configuration copy (D001/D002/T001/T003).
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: `ProcessWrapper`'s constructor shall set `StartInfo.FileName = resolvedFilePath.FullName` after `BaseProcessControlAdapter.ApplyConfiguration` runs, so the resolved path supersedes the unresolved `Configuration.TargetFilePath` write. The override applies to all three `ProcessWrapper` call sites: the three `ExternalProcess` constructors, the sync `Start()` path, and the `StartAsync(ProcessConfiguration, CancellationToken)` path (per T008).
- **Constraints**: Adapter still writes the unresolved path briefly during the same ctor body; future contributors must not reorder ctor statements to put the override before `ApplyConfiguration`. Must work for both `WindowsProcessControlAdapter` and `UnixProcessControlAdapter`. Consistent with T001/T002/T003.

### [T008] — `StartAsync(ProcessConfiguration, CancellationToken)` resolution source

- **Driver**: The parameter's name says what it does; D002/T004 forbid writing to `Configuration.TargetFilePath`, so the dual-source/dual-write current behaviour must collapse to a single resolution source (D001/D002/T001/T004).
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The `ExternalProcess.StartAsync(ProcessConfiguration configuration, CancellationToken cancellationToken)` overload shall resolve `configuration.TargetFilePath` via `IFilePathResolver` and pass the result to a fresh `ProcessWrapper`, leaving `this.Configuration` (the field) and its `TargetFilePath` untouched. The wrapper must be constructed with `(configuration, resolvedFilePath)` per T003/T007.
- **Constraints**: Field's `Configuration` is not read or mutated by this overload. The overload and the parameterless `StartAsync(CancellationToken)` differ in resolution source (parameter vs. field); callers must pick intentionally. The parameterless overload's `await StartAsync(Configuration, cancellationToken);` redirect continues to satisfy the "start with the current Configuration" semantic because the inner overload now resolves its parameter (which is the field), so no separate field-resolution path is needed. Consistent with T001/T004.

### [T009] — PowershellProcessInvoker / PowerShellMiddleware resolver-parameter ripple

- **Driver**: With T006, the resolver no longer reaches `PowershellProcessConfiguration`'s constructor body; carrying the parameter forward into `PowerShellMiddleware` and `PowershellProcessInvoker` is dead weight (D001/D004/T006).
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The `IFilePathResolver` parameter shall be removed from the constructors of both `PowershellProcessInvoker` and `PowerShellMiddleware`. The default `CliInvoke.FilePathResolver` previously allocated inside `PowerShellMiddleware` shall continue to be allocated there (lazily) when no override is supplied, so behaviour is preserved for callers that did not pass a resolver.
- **Constraints**: Breaking change for direct constructor callers passing a resolver (already accepted in T006). `CmdProcessInvoker` and `CmdMiddleware` are unaffected (they never took a resolver). Call sites in `CliInvoke.Extensions.DependencyInjection.FilePathResolverRegistration` and any `UsePowerShell()` overloads must be updated. Consistent with D004/T006.

### [T010] — `ExternalProcess.Configuration` setter visibility

- **Driver**: D002 forbids mutating the caller's `Configuration` instance; the spirit of the philosophy branch is broader — don't update the user's input after configuration, including swapping the field's reference to a new instance — and Option B aligns `Configuration`'s visibility with `TargetFilePath`'s (T004).
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: `ExternalProcess.Configuration` and `IExternalProcess.Configuration` shall be `init`-only (`{ get; init; }`). The configuration must be supplied via the constructor; post-construction reassignment shall not compile. The interface change ripples to every `IExternalProcess` implementation in the repo.
- **Constraints**: Breaking change for any caller that does `externalProcess.Configuration = newConfig;` (intended). Aligns with T004 (init-only on `TargetFilePath`). The `Configuration` parameter on the `StartAsync(ProcessConfiguration, CancellationToken)` overload (per T008) is unaffected — it is a method parameter, not a property. Consistent with D002/T004.

### [T011] — Test stub executable for T005's unit test

- **Driver**: T005 calls for a CI-runnable test that exercises a real `Start`; `dotnet --info` is cross-platform, always available in the CI image (per AGENTS.md), and fast-exiting (per the `ProcessWrapper.cs:90` race-guard comment).
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The T005 unit test shall construct an `ExternalProcess` with `dotnet` as `Configuration.TargetFilePath`, invoke `Start()` and `StartAsync()`, and assert (a) `Configuration.TargetFilePath` is unchanged from the user-supplied string (`"dotnet"`), and (b) `result.ExecutedFilePath` (the resolved path returned via `ProcessResult`/`BufferedProcessResult`/`PipedProcessResult`) equals the resolved `dotnet` binary path returned by `IFilePathResolver`.
- **Constraints**: Requires .NET SDK in CI (already mandated by `AGENTS.md §Testing`). The fast-exiting-process race is handled by the existing `ProcessWrapper` guard. One new test under `tests/CliInvoke.Tests/`. Test must avoid asserting on the resolved path bytes verbatim — assert that the returned `FileInfo` from the resolver equals `result.ExecutedFilePath` to stay portable. Consistent with T005/T007/T008.

### [T012] — XML doc wording on `Configuration.TargetFilePath` / `Start` / `StartAsync`

- **Driver**: D003 mandates docs visible at the call site; a brief `<remarks>` plus a `<see cref>` keeps IntelliSense clean while still pointing users to the resolved-path surface — matching the goal of simple, intuitive discoverability.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: `ProcessConfiguration.TargetFilePath`, `ExternalProcess.Start`, `ExternalProcess.StartAsync(CancellationToken)`, and `ExternalProcess.StartAsync(ProcessConfiguration, CancellationToken)` shall each carry a one-line `<remarks>` block stating that `Configuration` is not mutated after construction (per D002/T004) and that the resolved file path is available via `ProcessResult.ExecutedFilePath` (and its derived types `BufferedProcessResult`, `PipedProcessResult`). Each `<remarks>` shall include a `<see cref="ProcessResult.ExecutedFilePath"/>` cross-reference.
- **Constraints**: Cross-refs must be kept current if result types rename. External docs (README, ADR under `docs/decisions/` or `site/docs/`) are out of scope for this branch — a follow-up branch handles them. Consistent with D003.

### [T013] — Changelog / release-artefact classification (v3 context)

- **Driver**: v3 is pre-release with a major semver bump already planned; this change needs a more substantive consumer-facing artifact than a routine "Changed" entry to convey the no-mutation contract clearly (D001/D002/D003/T004–T012).
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: A migration guide shall be added (location: `docs/decisions/`, `site/docs/`, or `README.md` — to be decided during implementation, with `docs/decisions/` as the default for consistency with this ledger) that walks consumers through the no-mutation contract, the init-only setters (`ProcessConfiguration.TargetFilePath`, `ExternalProcess.Configuration` per T004/T010), and the removed constructor parameters (`PowershellProcessConfiguration` per T006, `PowershellProcessInvoker`/`PowerShellMiddleware` per T009). The guide shall include before/after code samples and shall be linked from `CHANGELOG.md` so consumers scanning the changelog find the upgrade path.
- **Constraints**: Location (README vs `docs/decisions/` ADR vs `site/docs/`) deferred to implementation. A complementary `### BREAKING` entry in `CHANGELOG.md` linking to the migration guide is encouraged (and aligned with v3's pre-release posture) but not required by this branch. The migration guide must be kept in sync with code; an automated cross-ref check between the guide and the changed APIs is desirable. Consistent with D003.

<!-- next-d: D005 -->
<!-- next-t: T014 -->
<!-- next-i: I004 -->
