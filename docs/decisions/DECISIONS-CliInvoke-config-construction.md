# Decision Ledger — CliInvoke configuration-construction collapse

> Spec: conversation context — architecture-review candidate 1, "Collapse the configuration-construction cluster" (ProcessConfigurationFactory + ProcessConfigurationBuilder + BuilderProcessConfiguration bridge).
> Ledger created lazily on first append (code-implementation-grilling).

### [D001] — session goal

- **Driver**: the user wants to evaluate the configuration-construction cluster collapse before committing to an implementation.
- **Resolved Answer**: "Understand trade-offs first."
- **Normalized Requirement**: The session shall surface the trade-offs of each strategy for collapsing ProcessConfigurationFactory, ProcessConfigurationBuilder, and the BuilderProcessConfiguration bridge before any implementation decision is locked.
- **Constraints**: `None.`

### [T001] — primary language

- **Driver**: the repo already uses C# 13; no motivation to change the language for this refactor.
- **Resolved Answer**: "Option A — C# (repo-locked)."
- **Normalized Requirement**: The configuration-construction cluster shall remain implemented in C# 13.
- **Constraints**: `None.`
- **Cites**: D001

### [T002] — framework/runtime

- **Driver**: global.json mandates net10.0 and CI targets it; no reason to widen the TFM.
- **Resolved Answer**: "Option A — .NET 10 (repo-locked)."
- **Normalized Requirement**: The cluster shall target net10.0 only.
- **Constraints**: `None.`
- **Cites**: D001

### [T003] — project type

- **Driver**: the repo is a class library and the cluster lives inside the existing CliInvoke/CliInvoke.Core projects.
- **Resolved Answer**: "Option A — Class library (repo-locked)."
- **Normalized Requirement**: The cluster shall remain a class library inside the existing CliInvoke/CliInvoke.Core projects.
- **Constraints**: `None.`
- **Cites**: D001

### [T004] — bridge elimination

- **Driver**: the builder must construct a ProcessConfiguration without the BuilderProcessConfiguration hack, while keeping the 15-param ctor hidden from ordinary users (I001).
- **Resolved Answer**: "Option A — Internal ctor + existing InternalsVisibleTo."
- **Normalized Requirement**: The 15-parameter ProcessConfiguration constructor shall be internal (not protected/public); CliInvoke.Core already grants InternalsVisibleTo to CliInvoke, so ProcessConfigurationBuilder calls it directly and BuilderProcessConfiguration is deleted.
- **Constraints**: The 15-param ctor must not enter the public API; a future Core-internal type may rely on the internal ctor.
- **Cites**: D001, I001

### [T005] — factory collapse

- **Driver**: the factory is a deliberate convenience for external library consumers of the main package who find the builder unwieldy (I002); it must not be removed.
- **Resolved Answer**: "Option B — Keep thin, reduce overloads."
- **Normalized Requirement**: ProcessConfigurationFactory shall be retained as a class but collapsed from three near-identical Create overloads to one or two; its external convenience surface is preserved.
- **Constraints**: The factory's public convenience for main-package consumers must not be removed; the 33 existing call sites need not all change.
- **Cites**: D001, I002

### [T006] — thinned factory API shape

- **Driver**: the thinned factory's API must decouple from the concrete builder and stay a static class (T005=B), without forcing a main-package dependency (I004).
- **Resolved Answer**: "Option D — Static factory, direct config construction (no builder)."
- **Normalized Requirement**: The thinned ProcessConfigurationFactory shall be a static class whose Create(...) methods directly construct a ProcessConfiguration via the internal 15-param ctor (T004=A), assembling fields using the existing spec types (ArgumentsSpec, EnvironmentVariablesSpec, ProcessResourcePolicySpec, UserCredentialSpec) rather than instantiating any IProcessConfigurationBuilder or ProcessConfigurationBuilder.
- **Constraints**: The factory reuses the spec types for assembly and validation; it must not duplicate the validation logic those specs already contain. The factory remains a separate class (T005=B).
- **Cites**: D001, T004, T005, I002, I004

### [T007] — factory placement

- **Driver**: the thinned factory's placement must preserve its main-package convenience surface for external consumers while keeping Core for primitives (I002).
- **Resolved Answer**: "Option A — Keep in main package (CliInvoke)."
- **Normalized Requirement**: The thinned ProcessConfigurationFactory (static, spec-based per T006=D) shall remain in the CliInvoke main package, using Core's spec types and the internal ctor via the existing InternalsVisibleTo.
- **Constraints**: Core stays for primitives; the factory is not moved to Core (B) or Extensions (C).
- **Cites**: D001, T006, I002

### [T008] — thinned factory exact API surface

- **Driver**: the thinned factory's exact API surface must be genuinely reduced (not a rename of today's API) while staying spec-based (T006=D) and within T005=B's 1–2 overloads.
- **Resolved Answer**: "Option B — Two overloads (string + IEnumerable), both with spec callbacks, dropping params."
- **Normalized Requirement**: The thinned ProcessConfigurationFactory shall expose exactly two static Create overloads: (1) Create(string targetFilePath, string arguments, string? workingDirectory = null, bool outputRedirection = true, bool enableWindowCreation = false); (2) Create(string targetFilePath, IEnumerable<string> arguments, string? workingDirectory = null, bool outputRedirection = true, bool enableWindowCreation = false, Action<EnvironmentVariablesSpec>? configureEnvironmentVariables = null, Action<ProcessResourcePolicySpec>? configureResourcePolicy = null, Action<UserCredentialSpec>? configureCredential = null). No params overload; no IProcessConfigurationBuilder.
- **Constraints**: The params overload from today's API is removed; call sites using params must wrap to new[]{...} or the string overload. Advanced config is exposed via spec callbacks, not configureBuilder.
- **Cites**: D001, T005, T006, T007

### [I001] — T004 ctor visibility rationale

- **Prompt**: "For T004 – bridge elimination: pick an option, hybridize, or provide your own answer."
- **User Response**: "I'm sure this has been documented in the codebase why Option A wasn't chosen because it's been previously identified. The full ctor becoming public is too complex and unwieldy for ordinary users of ProcessConfiguration and is really just meant to be used by a Builder or Factory - not a user."
- **Resolution**: Clarified that the user's reasoning rejects Option B (public full ctor), not Option A (internal ctor). Option A keeps the 15-param ctor internal (hidden from the public API via the existing InternalsVisibleTo), consistent with the user's concern that ordinary users must not see it. T004 recommendation remains A; B is ruled out.
- **Notes**: Verification found no explicit written rationale in the codebase rejecting the internal-ctor approach. The BuilderProcessConfiguration doc (ProcessConfigurationBuilder.cs:453-467) states verbatim: "Cross-assembly access to the protected constructor requires this internal wrapper class as the legitimate access path." / "Do not delete this class without first choosing a long-term replacement." / "A long-term solution to eliminate this wrapper is being developed." The first two lines explain WHY the bridge exists and why not to delete it carelessly; none reject the internal-ctor approach. "A long-term solution to eliminate this wrapper is being developed" points toward eliminating the bridge (Option A), not rejecting it.

### [I002] — T005 factory purpose

- **Prompt**: "For T005 – factory collapse: pick an option, hybridize, or provide your own answer."
- **User Response**: "ProcessConfigurationFactory exists to bridge the gap because A) ProcessConfiguration's public ctor was too unwieldly and B) ProcessConfigurationBuilder used to be a wider and more complex interface than it is now. ... ProcessConfigurationFactory's main internal consumer is CliRun and its main external consumer is libraries that use CliInvoke's main package where the Builder pattern via DI or explicitly was unwieldy or unnecessarily verbose."
- **Resolution**: Reframes T005 recommendation to Option B (keep thin, reduce overloads). The factory is a deliberate convenience for external library consumers of the main package; deleting or folding it would remove that convenience. The collapse should reduce its internal overload redundancy, not remove the surface.
- **Notes**: Factory has 1 production caller (CliRun.cs:106) and 32 test call sites; external consumers are libraries using the main package. Builder is available to users via main package or via IProcessConfigurationBuilder DI in core.

### [I003] — middle-ground ctor exploration

- **Prompt**: "Would a middle ground ctor in ProcessConfiguration (bigger than smallest public ctor, smaller than 15 params hidden ctor) help to fill the role that ProcessConfigurationFactory now plays or would that be a code smell?"
- **User Response**: User asked whether a public "middle" constructor (between the 3-param public ctor and the 15-param protected ctor) would fill the factory's role or be a smell.
- **Resolution**: Assessed as likely a code smell. It would duplicate, not replace, the factory; a ctor cannot replicate the factory's configureBuilder escape hatch or its string/IEnumerable/params overload flexibility, so the factory would still be needed — net more construction paths. It is orthogonal to T004 (does not eliminate the bridge; the builder still calls the full ctor internally). It also pulls more surface into the public API, contradicting I001/I002 (keep complex ctors from ordinary users). Prefer T005 Option B (keep factory thin) or a single static factory method over a new ctor.
- **Notes**: The factory already delegates to the builder internally, so a middle ctor would create a third parallel construction path (ctor + factory + builder) rather than consolidating.

### [I004] — reorder: grill API shape before placement

- **Prompt**: "For T006 – factory placement: pick an option, hybridize, or provide your own answer."
- **User Response**: "Without deciding/seeing the thinned factory proposal I reject your premise that a thinned factory needs to reference the main package to use the Builder. We need to grill on the thinned factory's new API before we can resolve T006."
- **Resolution**: Accepted the correction. The thinned factory's API shape is a prerequisite to placement; the assumption that it must reference the concrete builder (CliInvoke) is premature. Reordered: T006 becomes the thinned-factory API-shape branch; placement becomes T007.
- **Notes**: The thinned factory could depend only on IProcessConfigurationBuilder (Core interface), enabling Core placement; API shape must be grilled first.

### [I005] — missing option D (static factory, no builder)

- **Prompt**: "For T006 – thinned factory API shape: pick an option, hybridize, or provide your own answer."
- **User Response**: "You're missing a 4th option where the factory stays static but internally creates a new configuration object without a builder interface or implementation."
- **Resolution**: Added Option D to T006: a static factory whose Create(...) methods directly new a ProcessConfiguration (via the internal 15-param ctor from T004=A) and assemble the fields, without instantiating any IProcessConfigurationBuilder or ProcessConfigurationBuilder.
- **Notes**: D is the most decoupled (no builder dependency at all) and matches the user's "factory stays static" steer; it must replicate the minimal assembly/validation the builder's Build() performs, or accept pre-built spec objects.

### [T009] — validation surface

- **Driver**: The user prefers the thinned factory validate only inputs it owns (targetFilePath, arguments, workingDirectory existence); spec callbacks handle spec-field validation; the 15-param ctor handles null/empty. Since the factory's T008 surface does not expose the cross-constraint inputs, that check stays in builder.Build() and is not replicated in the factory.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The thinned ProcessConfigurationFactory shall validate inputs it owns (targetFilePath, arguments, workingDirectory existence), invoke spec callbacks so spec types validate their own fields, and rely on the internal 15-param ProcessConfiguration constructor for null/empty validation. The cross-constraint check in builder.Build() shall not be replicated in the factory.
- **Constraints**: Spec type internal validation is unchanged. The factory must not duplicate validation logic already present in spec types. Factory callers do not get the cross-constraint check (the factory's T008 surface does not expose those inputs).
- **Cites**: D001, T004, T006, T008, I005

### [T010] — migration strategy

- **Driver**: The user prefers the thinned factory's two-overload surface (T008=B) without a deprecation shim; the 21 affected test call sites (18 zero-arg params + 2 named-bool params + 1 collection-expression params) wrap to the new surface.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The 21 call sites using the removed `params` overload (18 zero-arg `Create(targetFilePath)` sites + 2 named-bool `Create(_targetFilePath, outputRedirection: true)` sites + 1 collection-expression `Create("echo", [...])` site) shall wrap to the new two-overload surface: zero-arg sites become `Create(targetFilePath, "")`; named-bool sites become `Create(_targetFilePath, "", outputRedirection: true)`; the collection-expression site becomes `Create("echo", new[] { ... })`. No `[Obsolete]` shim shall be retained.
- **Constraints**: The thinned factory exposes exactly two `Create` overloads per T008=B; no `params` overload shall be added back; the 12 unaffected call sites (10 OL2 + 2 production/AOT) need not change.
- **Cites**: D001, T005, T008

### [T011] — InternalsVisibleTo scope

- **Driver**: The cluster collapse as scoped (T001–T009) operates within the existing InternalsVisibleTo graph — Core grants CliInvoke (which holds the thinned factory per T007=A) and Tests; no new grants are required to make the collapse work.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: No new `InternalsVisibleTo` declarations shall be added for the cluster collapse. The thinned `ProcessConfigurationFactory` (in `CliInvoke`) shall access the internal 15-param `ProcessConfiguration` constructor via the existing `CliInvoke.Core` → `CliInvoke` grant. Tests shall continue to use the existing `CliInvoke.Core` → `CliInvoke.Tests` grant.
- **Constraints**: Core's `InternalsVisibleTo` list remains `CliInvoke` and `CliInvoke.Tests` only; future factory relocations (e.g., to `CliInvoke.Extensions` or `CliInvoke.Specializations`) shall require separate InternalsVisibleTo decisions.
- **Cites**: D001, T004, T007

### [T014] — spec callback shape

- **Driver**: The user prefers the existing Extensions cross-assembly mutation pattern (ConfigurationExtensions.cs:67–100) where callbacks mutate a freshly-constructed spec and the caller reads `spec.Build()`; callback exceptions propagate unchanged.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: For each `configureEnvironmentVariables`, `configureResourcePolicy`, and `configureCredential` callback, the thinned `ProcessConfigurationFactory` shall instantiate a fresh spec, invoke the user callback against it, read the spec's `Build()` result, and pass that to the internal 15-param `ProcessConfiguration` constructor. If the user callback throws, the exception shall propagate unchanged (no wrapping, no logging, no swallowing).
- **Constraints**: No `Validate()` method shall be added to spec types; spec `Add`/`Set` methods continue to throw synchronously and serve as the validation surface; the spec object passed to the user callback shall not be retained beyond the factory call.
- **Cites**: D001, T008, T009

### [T015] — default value alignment

- **Driver**: The user prefers the thinned factory match the public 3-param `ProcessConfiguration` ctor (the common user entry point) and preserve existing factory behavior; the builder's `_outputRedirection = false` default is a separate design point worth documenting but not changing in this collapse.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The thinned `ProcessConfigurationFactory` shall retain T008=B's defaults (`outputRedirection = true`, `enableWindowCreation = false`). An XML doc comment on the factory's parameters shall note that the `ProcessConfigurationBuilder`'s `_outputRedirection` field defaults to `false`, surfacing the divergence between the two construction paths.
- **Constraints**: The builder's `_outputRedirection = false` field initializer (ProcessConfigurationBuilder.cs:70) shall not change in this collapse; no behavior change for existing factory callers; the divergence is documented, not eliminated.
- **Cites**: D001, T006, T008

### [T020] — `enableWindowCreation` + `outputRedirection` cross-check

- **Driver**: The user prefers no validation or documentation for the `enableWindowCreation + outputRedirection` combination; the runtime is permissive, and T009 already locks in no factory-level cross-constraint.
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The thinned `ProcessConfigurationFactory` shall not validate the combination of `enableWindowCreation = true` and `outputRedirection = true`. No XML doc warning shall be added for the combination. The combination is valid at the .NET `ProcessStartInfo` level (BaseProcessControlAdapter.cs:36, 40–41) and produces a process with a visible window plus redirected stdout/stderr.
- **Constraints**: T009's "no factory cross-constraint" stance is preserved; no XML doc note is added for this specific combination; the combination is left to runtime semantics.
- **Cites**: D001, T008, T009

## Consolidated Implementation Plan

### Scope Binding
- Source spec: architecture-review candidate 1, "Collapse the configuration-construction cluster" (conversation context).
- Decision Ledger: docs/decisions/DECISIONS-CliInvoke-config-construction.md

### File changes

#### src/CliInvoke.Core/Primitives/ProcessConfiguration.cs — T004
- Change the 15-parameter constructor (lines 54–69) from `protected` to `internal`.
- The public 3-parameter ctor's `: this(...)` delegation is unaffected (same-class call).
- Enables `ProcessConfigurationBuilder` (CliInvoke) to call it directly via the existing `InternalsVisibleTo("CliInvoke")`.

#### src/CliInvoke/Builders/ProcessConfigurationBuilder.cs — T004
- In `Build()` (lines 435–441), replace `new BuilderProcessConfiguration(...)` with `new ProcessConfiguration(...)`.
- Delete the `BuilderProcessConfiguration` class (lines 468–485) and its doc block.
- Verified: no other references to `BuilderProcessConfiguration` exist.

#### src/CliInvoke/Extensions/ProcessConfigurationFactory.cs — T005, T006, T007, T008
- Collapse the three `Create` overloads (lines 30–119) to two (T005=B), reimplemented as a **static** factory (T006=D) that directly `new`s a `ProcessConfiguration` via the internal 15-param ctor, assembling fields with the existing spec types; no `IProcessConfigurationBuilder`/`ProcessConfigurationBuilder` instantiation. Keep the class in `CliInvoke` (T007=A).
- Exact surface (T008=B) — two overloads, no `params`, no `configureBuilder`:
  ```csharp
  public static ProcessConfiguration Create(
      string targetFilePath, string arguments,
      string? workingDirectory = null, bool outputRedirection = true, bool enableWindowCreation = false);

  public static ProcessConfiguration Create(
      string targetFilePath, IEnumerable<string> arguments,
      string? workingDirectory = null, bool outputRedirection = true, bool enableWindowCreation = false,
      Action<EnvironmentVariablesSpec>? configureEnvironmentVariables = null,
      Action<ProcessResourcePolicySpec>? configureResourcePolicy = null,
      Action<UserCredentialSpec>? configureCredential = null);
  ```
- Reuse spec types for assembly/validation; do not duplicate their logic.
- The removed `params` overload means call sites using `Create("exe", "a", "b")` become `Create("exe", new[] { "a", "b" })` or the string overload.

#### Tests — T005
- Review the 32 test call sites of `ProcessConfigurationFactory.Create`; adjust only those using a removed overload. `CliRun.cs:106` (the 1 production caller) is unaffected if a compatible overload remains.

### Ledger Reference
- D001 (goal); T001–T003 (base lock: language/framework/type); T004 (bridge elimination via internal ctor); T005 (factory kept thin); T006 (static factory, spec-based, no builder); T007 (keep in main package); T008 (two-overload spec-based surface); T009 (factory validates own inputs + spec callbacks + ctor null/empty; cross-constraint stays in Build()); T010 (21 affected params-overload call sites wrap to the new surface; no [Obsolete] shim); T011 (no new InternalsVisibleTo grants; existing Core → CliInvoke + Core → Tests suffice); T014 (fresh spec per callback; read spec.Build(); exceptions propagate unchanged); T015 (factory defaults match the public 3-param ctor; XML doc note surfaces builder's separate `_outputRedirection = false` default); T020 (no validation or documentation for the `enableWindowCreation + outputRedirection` combination; runtime is permissive).
- I001–I005 (clarifying interactions).

### [I006] — T001–T003 excluded from ticket citations

- **Prompt**: (User volunteered this clarification in response to the spec-to-tickets decomposition proposal for the cluster collapse; no agent question was presented.)
- **User Response**: "T1 through T3 can be ignored. They aren't part of any ticket."
- **Resolution**: User confirmed that the repo-locked base records — T001 (language = C#), T002 (framework = .NET 10), T003 (project type = class library) — are not cited in any ticket's acceptance criteria or context pointers. The remaining 12 citable records (D001, T004, T005, T006, T007, T008, T009, T010, T011, T014, T015, T020) are cited across TK001–TK004 in the resulting ticket set under `tickets/config-construction-collapse/`.
- **Notes**: T001–T003 describe what the repo is, not what the cluster collapse must do. They are satisfied implicitly by the code location (C# source files in a .NET 10 class library); no ticket-level action is required. The user's exclusion is a scope decision at the spec-to-tickets boundary, not an amendment to the records themselves. The Ixxx discipline's pre-question append was skipped because the clarification was user-volunteered; both phases of the discipline are completed in this single append. The records remain in the ledger as background context for the collapse.

<!-- next-d: D002 -->
<!-- next-t: T021 -->
<!-- next-i: I007 -->
