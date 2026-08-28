# Decision Ledger — CliInvoke v3 Internals Visibility

Session goal: tighten encapsulation and reduce cross-package coupling via InternalsVisibleTo scope reduction, so a small internal tweak does not ripple compile errors across downstream CliInvoke packages.

### [D001] - session goal

- **Driver**: the user wants tighter encapsulation and fewer cross-package coupling points; the current IVT system over-exposes internal types to consuming packages and is fragile — a small internal tweak causes rippling compile errors ("Polyfill errors") across all downstream CliInvoke packages.
- **Resolved Answer**: "I want tighter encapsulation and cross-package coupling points. Coupling to some degree is inevitable because they are structurally designed to provide different entrypoints into the CliInvoke ecosystem but the current InternalsVisibleTo system is fragile and gives greater knowledge of types to each consuming package than should be provided. I don't want to accidentally have a small tweak cause rippling Polyfill errors in all downstream CliInvoke packages."
- **Normalized Requirement**: The session shall define a principle and scope for reducing InternalsVisibleTo grants across the CliInvoke packages so that each consuming package receives only the internal knowledge it legitimately needs, reducing cross-package coupling and the blast radius of internal changes.
- **Constraints**: Coupling between the packages is partially by design (they are separate entrypoints into the ecosystem); the goal is fewer/reduced coupling points, not zero coupling.

### [I001] - meaning of "Polyfill errors"

- **Prompt**: You said a small internal tweak causes "rippling Polyfill errors" across downstream packages. Polyfill is a PrivateAssets="all" dependency of CliInvoke.Core and CliInvoke (used via global using Polyfills;). What specifically do you mean by "Polyfill errors" — is it Polyfill's generated internal helper types (Guard/Ensure/polyfill extensions) leaking into the IVT-visible surface so consumers can't resolve them, or something else?
- **User Response**: Selected "Polyfill helpers leak via IVT" — Core/CliInvoke's Polyfill-generated internal helpers appear in signatures of IVT-exposed internals, so downstream consumers referencing those internals can't resolve the Polyfill types and fail to compile; a tweak ripples instantly.
- **Resolution**: Confirms the failure mode is leakage of Polyfill-generated internal helper types (Guard/Ensure/polyfill extensions) through the IVT-visible surface of granting packages into downstream consumers that cannot resolve those internal Polyfill types. Shapes D002: the principle must minimize the IVT-visible surface regardless of whether a grant is "required" today, because the risk is transitive third-party (Polyfill) internal exposure, not just first-party internals.
- **Notes**: Polyfill is PrivateAssets="all" in CliInvoke.Core and CliInvoke; global using Polyfills; in CliInvoke/GlobalUsings.cs. Blast radius = packages with IVT into Core/CliInvoke (CliInvoke, CliInvoke.Specializations, CliInvoke.Tests, CliInvoke.Specializations.Tests for Core; CliInvoke.Tests for CliInvoke).

### [D002] - legitimacy principle

- **Driver**: the user wants tighter encapsulation; .NET IVT is assembly-scoped, so by-need must be achieved via grant-minimization plus promoting or relocating the specific required types.
- **Resolved Answer**: "Option A — Minimize + promote/relocate: drop unneeded grants; promote specific needed types to public API or a shared package."
- **Normalized Requirement**: A CliInvoke package shall grant IVT only to assemblies that strictly require it; where a consumer needs only specific internal types, those types shall be promoted to a public stable API or relocated to a shared package so the IVT grant is removed rather than narrowed.
- **Constraints**: IVT is assembly-scoped in .NET; per-symbol grants are impossible. Public-API promotion is a breaking-change commitment (resolved in D006). Test-assembly grants are excluded per D004.

### [D003] - reduction aggressiveness

- **Driver**: the user wants fewer coupling points without premature breaking public-API changes.
- **Resolved Answer**: "Option B — Unused now, required later: remove unused grants now; schedule required-grant reduction after D002 is set."
- **Normalized Requirement**: In this pass, remove the 4 unused IVT grants (CliInvoke→Specializations, CliInvoke→Specializations.Tests, CliInvoke→Extensions, Specializations→Specializations.Tests). Reduction of the required grants shall be scheduled as follow-up work sequenced behind the D002 principle.
- **Constraints**: The fragile required grants remain exposed until the follow-up. The follow-up must use the D002 promote/relocate approach. Public promotions may be breaking (D006).

### [D004] - test-assembly IVT scope

- **Driver**: the user's goal targets downstream packages; test projects are same-repo and not the blast radius described in I001.
- **Resolved Answer**: "Option A — Tests excluded: keep IVT to test assemblies; they are not shipping packages."
- **Normalized Requirement**: IVT grants to test assemblies (CliInvoke.Tests, CliInvoke.Specializations.Tests) are out of scope for reduction in this effort and remain as-is.
- **Constraints**: Test projects still couple to internals and a refactor can break them, but they are same-repo and not downstream packages; the D002 principle does not apply to test grants.

### [D005] - mechanism for required grants

- **Driver**: the user wants the promote-vs-relocate decision made deliberately per type, not by a single global rule.
- **Resolved Answer**: "I want to make that determination on whether to promote to public, relocate to Core, or otherwise on a per type basis."
- **Normalized Requirement**: For each required IVT grant, the reduction mechanism (promote the specific needed type to public API, relocate it to CliInvoke.Core, or another approach) shall be decided per internal type during the follow-up work, applying the D002 principle case-by-case.
- **Constraints**: No single global mechanism is mandated; each type's choice follows D002 (minimize + promote/relocate). The per-type choice drives the breaking-change implication resolved in D006.

### [D006] - version / breaking-change window

- **Driver**: the user wants the version-window decision to follow from the per-type mechanism choices.
- **Resolved Answer**: "DEFERRED — depends on D005's answer (per-type mechanism determination)."
- **Normalized Requirement**: Deferred pending the D005 per-type analysis. The window shall be applied per-type: any type promoted to public API requires the v3 major-version window (SemVer major); types relocated to Core are non-breaking for consumers. Finalize when the per-type list is known.
- **Constraints**: Blocked on D005's per-type analysis (implementation work). Re-open after the per-type determination or resolve within the implementation handoff.

### [D007] - enforcement / verification

- **Driver**: the user wants the decision recorded durably and contributors guided, without necessarily adding CI automation now.
- **Resolved Answer**: "Create new ADR and document in CONTRIBUTING.md."
- **Normalized Requirement**: A new ADR shall record the D002 principle and the D003–D006 decisions; CONTRIBUTING.md shall document the IVT-minimization principle so contributors know a new grant requires justification.
- **Constraints**: No automated CI guard was selected; enforcement relies on the ADR + CONTRIBUTING.md + code review. Differs from D007 Option A, which also added a CI guard.

### [D008] - glossary terms accepted

- **Driver**: the session introduced domain vocabulary not present in GLOSSARY.md; the user accepted the proposed terms so the domain model is unambiguous.
- **Resolved Answer**: "Accept as proposed" — InternalsVisibleTo grant, Cross-package coupling point, Polyfill leakage, and Entrypoint package are added to GLOSSARY.md with the definitions below.
- **Normalized Requirement**: GLOSSARY.md shall define the four terms with the exact definitions below, kept in sync with this record.
- **Constraints**: None.

Terms and definitions (mirror of GLOSSARY.md):
- **InternalsVisibleTo grant (IVT grant)** — An assembly-scoped grant (via `<InternalsVisibleTo>` in a `.csproj` or `InternalsVisibleToAttribute`) by which one CliInvoke package exposes all of its internal types to a named friend assembly.
- **Cross-package coupling point** — A dependency surface where one CliInvoke package consumes another package's internal types through an IVT grant, so a change to the internal can break the consuming package.
- **Polyfill leakage** — The failure mode where a granting package's internal helper types (e.g., Guard/Ensure/polyfill extensions) appear in the signatures of IVT-exposed internals, so the consuming package cannot resolve them and fails to compile; a small internal tweak then ripples errors across downstream packages.
- **Entrypoint package** — One of the CliInvoke packages (Core, CliInvoke, Extensions, Specializations) that provides a distinct consumer entrypoint into the ecosystem; by design it may require limited internal access to other packages.

### [D009] - reconcile D003 ↔ D004 (test-grant scope)

- **Driver**: Conflict pre-check found D003 lists 4 unused grants for removal including two test-assembly grants (`CliInvoke→CliInvoke.Specializations.Tests`, `CliInvoke.Specializations→CliInvoke.Specializations.Tests`), but D004 states test-assembly grants "are out of scope for reduction in this effort and remain as-is." The two decisions contradict on whether unused test grants are removed now.
- **Resolved Answer**: "Option B — Reconcile: D004 means 'used test grants stay'; remove all unused grants, including the 3 unused test grants."
- **Normalized Requirement**: In this pass, remove every unused IVT grant regardless of whether the granted assembly is a test project. D004's exclusion applies only to *used* test grants (those with active internal cross-usage); unused test grants are removed. The removable set is: `CliInvoke→CliInvoke.Tests`, `CliInvoke→CliInvoke.Extensions`, `CliInvoke→CliInvoke.Specializations`, `CliInvoke→CliInvoke.Specializations.Tests`, and `CliInvoke.Specializations→CliInvoke.Specializations.Tests` (5 grants). The `CliInvoke→CliInvoke.Tests` grant is redundantly declared in both `CliInvoke.csproj` (AssemblyAttribute) and `AssemblyInfo.cs`; both declarations are removed as one logical grant. D003's "4 unused" list is corrected/expanded to this 5-grant set.
- **Constraints**: Used grants remain untouched — `CliInvoke.Core→CliInvoke`, `CliInvoke.Core→CliInvoke.Specializations`, `CliInvoke.Core→CliInvoke.Tests`, `CliInvoke.Core→CliInvoke.Specializations.Tests`, `CliInvoke.Extensions→CliInvoke.Tests`, and `CliInvoke.Specializations→CliInvoke.Extensions` (all verified actively used). D004's intent (tests are not the downstream blast radius) is preserved; only dead grants are removed. No public-API or relocation changes occur in this pass (those are deferred per D003/D005).

### [I002] - verify CliInvoke.Core→CliInvoke.Tests usage (closure of D009 set)

- **Prompt**: D009 removes every unused grant. The explorer's summary marked `CliInvoke.Core→CliInvoke.Tests` as "used" but its narrative cross-usage list omitted `CliInvoke.Tests` entirely, leaving the removable-set size ambiguous (5 vs 6). Does `CliInvoke.Tests` actually consume a `CliInvoke.Core` internal type? If not, that grant must also be removed and D009's constraint list corrected.
- **Resolution**: Verified by grep — `CliInvoke.Tests` references `MiddlewareChain` (an `internal sealed class` at `src/CliInvoke.Core/Middleware/MiddlewareChain.cs:16`) in `ChainDisposalTests.cs`, `MiddlewareChainTests.cs`, and `UseWhenTests.cs`. The grant `CliInvoke.Core→CliInvoke.Tests` is therefore genuinely *used* and remains. Removable set is confirmed at exactly 5 grants; D009's constraint list is correct and needs no correction. (The explorer's narrative was merely incomplete; its summary table was right.)
- **Notes**: This also confirms `MiddlewareChain` is a Core internal consumed by tests — relevant later if the follow-up (D005) considers relocating/promoting Core internals.

### [T001] - keep CliInvoke.Core→CliInvoke IVT grant for MiddlewareChain/ConditionalMiddleware

- **Driver**: User wants to resolve MiddlewareChain/ConditionalMiddleware exposure; relocation to CliInvoke is blocked by ProcessMiddlewareBuilder (Core public) constructing ConditionalMiddleware, and promotion adds a v3-breaking public surface the user declined.
- **Resolved Answer**: "Option C" — Keep the IVT grant (leave Core→CliInvoke as-is for this type).
- **Normalized Requirement**: Retain the CliInvoke.Core→CliInvoke InternalsVisibleTo grant; do not promote MiddlewareChain/ConditionalMiddleware to public and do not relocate them. The grant remains required because ProcessInvoker consumes MiddlewareChain.
- **Constraints**: The grant stays in place; it is a used grant per D009 and is not removed in this pass. No public-API or relocation change occurs for these types. This is consistent with D002's acceptance that some required grants remain.
- **Cites**: D002, D005, D009.

### [T002] - relocate PathEnvironmentVariable to CliInvoke; remove FilePathResolverBase

- **Driver**: User wants to eliminate the CliInvoke.Core→CliInvoke coupling for path resolution without a public promotion, by removing the Core base class and moving the PATH utility into CliInvoke.
- **Resolved Answer**: "I think the cleaner approach is to remove FilePathResolverBase and move PathEnvironmentVariable to CliInvoke. FilePathResolver gets reworked."
- **Normalized Requirement**: Remove the public abstract `FilePathResolverBase` from CliInvoke.Core; relocate `PathEnvironmentVariable` (PATH/PATHEXT enumeration) into the CliInvoke project as an internal type; rework `FilePathResolver` to resolve paths without depending on the removed Core base class or Core's internal escaper; update the `TestableFilePathResolver` test stub. After this, CliInvoke consumes no CliInvoke.Core internal for path resolution.
- **Constraints**: Removing the public `FilePathResolverBase` is a breaking change and must ship in the v3 major window (D006). Core must retain zero references to `PathEnvironmentVariable` (verified: only `FilePathResolverBase` used it at FilePathResolverBase.cs:117,131). The reworked `FilePathResolver` must preserve existing public path-resolution behavior.
- **Cites**: D002, D005, D006, D009.

### [T003] - relocate ShellArgumentEscaper PowerShell/Cmd logic to Specializations; rework ArgumentsSpec escaping

- **Driver**: User wants to eliminate the CliInvoke.Core→CliInvoke.Specializations coupling by moving the shell escaping logic into Specializations (kept internal) and making ArgumentsSpec's escaping self-contained and less restrictive than Cmd.
- **Resolved Answer**: "ShellArgumentEscaper's Powershell and Cmd escaping logic should be moved to Specializations but kept internal. The ArgumentSpec escaping logic should be reworked to be A) self contained and B) less restrictive than the Cmd escaping logic."
- **Normalized Requirement**: Relocate `EscapeForPowerShell` and `EscapeForCmd` from `CliInvoke.Core.Internal.ShellArgumentEscaper` into a new `internal static` helper in CliInvoke.Specializations (consumed by `PowerShellMiddleware`/`CmdMiddleware`). Rework `ArgumentsSpec` (Core) to use a new self-contained Core-internal escaper that escapes a strictly smaller metacharacter set than `EscapeForCmd` (which caret-escapes `^ & | < > %`, doubles `"`, drops newlines). Delete the now-unused Core `ShellArgumentEscaper`. After this, Specializations consumes no CliInvoke.Core internal, so the Core→Specializations grant becomes unused and is removed per D009.
- **Constraints**: The Specializations escaper must stay `internal` (not promoted). `ArgumentsSpec`'s new escaper must remain in Core (Core must not depend on Specializations) and be self-contained. The reduced metacharacter set is finalized in implementation but must be a strict subset of `EscapeForCmd`'s set. `ShellArgumentEscaper` is internal, so its removal is non-breaking for the external public API.
- **Cites**: D002, D005, D009.

### [T004] - keep CliInvoke.Specializations→CliInvoke.Extensions IVT grant for PowerShellMiddleware

- **Driver**: User wants to resolve PowerShellMiddleware's exposure to Extensions; promotion exposes an implementation type as public (v3), and relocation (a public AddShellSpecializations DI method) adds a new public API the user declined.
- **Resolved Answer**: "Option C" — Keep the IVT grant (leave Specializations→Extensions as-is for this type).
- **Normalized Requirement**: Retain the CliInvoke.Specializations→CliInvoke.Extensions InternalsVisibleTo grant; do not promote PowerShellMiddleware to public and do not add a relocation DI method. The grant remains required because Extensions registers PowerShellMiddleware via typeof + new.
- **Constraints**: The grant stays in place; it is a used grant per D009 and is not removed in this pass. No public-API or relocation change occurs for this type. Consistent with D002's acceptance that some required grants remain.
- **Cites**: D002, D005, D009.

### [T005] - keep CliInvoke.Specializations→CliInvoke.Extensions IVT grant for CmdMiddleware

- **Driver**: User wants to resolve CmdMiddleware's exposure to Extensions; promotion exposes an implementation type as public (v3), and relocation (sharing the AddShellSpecializations DI method) adds a new public API the user declined.
- **Resolved Answer**: "Option C" — Keep the IVT grant (leave Specializations→Extensions as-is for this type).
- **Normalized Requirement**: Retain the CliInvoke.Specializations→CliInvoke.Extensions InternalsVisibleTo grant; do not promote CmdMiddleware to public and do not add a relocation DI method. The grant remains required because Extensions registers CmdMiddleware via typeof + new.
- **Constraints**: The grant stays in place; it is a used grant per D009 and is not removed in this pass. No public-API or relocation change occurs for this type. Consistent with D002's acceptance that some required grants remain.
- **Cites**: D002, D005, D009.

### [T006] - ArgumentsSpec self-contained platform-aware escaper

- **Driver**: User wants ArgumentsSpec escaping self-contained and correct cross-platform; a Windows-only C-runtime escaper is wrong on Unix where ProcessStartInfo.Arguments is parsed by the POSIX shell.
- **Resolved Answer**: "Option A" — Platform-aware escaper: self-contained Core escaper branching on OS (Windows C-runtime quoting; POSIX shell quoting on Unix).
- **Normalized Requirement**: Replace ArgumentsSpec's use of ShellArgumentEscaper.EscapeForCmd with a new self-contained Core-internal escaper that branches on the runtime OS: on Windows apply C-runtime argument quoting (quote only when needed, double embedded `"`, leave shell metacharacters unescaped, drop newlines); on Unix apply POSIX shell quoting (single-quote when needed, escape embedded `'`). The escaper must not reference CliInvoke.Core.Internal.ShellArgumentEscaper, removing the Core→Specializations coupling dependency. This refines T003's "less restrictive than Cmd" to "platform-correct."
- **Constraints**: The escaper must remain in CliInvoke.Core (Core must not depend on Specializations). It must be self-contained (no ShellArgumentEscaper dependency). Existing ArgumentsSpec builder tests (drop newlines, double quotes, pass through `\`/`\t`) must continue to pass on Windows; the Unix branch is new behavior. Escaping is coupled to the runtime OS, acceptable for local process invocation.
- **Cites**: D002, D005, T003, D009.

### [T007] - ArgumentsSpec new escaper API shape (EscapeInner + NeedsQuoting)

- **Driver**: User wants the concrete API surface of the new self-contained platform-aware escaper (T006) decided, not just its behavior.
- **Resolved Answer**: "Option B" — Two methods: `EscapeInner(string?)` (returns escaped text, no quotes) and `NeedsQuoting(string?)` (platform-aware predicate); ArgumentsSpec keeps its existing escape-then-wrap structure.
- **Normalized Requirement**: Add `internal static class ArgumentEscaper` in `CliInvoke.Core.Internal` with `public static string EscapeInner(string? argument)` (platform-aware inner escaping: Windows doubles `"`, leaves shell metacharacters unescaped, drops newlines; Unix escapes `'`) and `public static bool NeedsQuoting(string? argument)` (true when the argument requires double-quote wrapping on the current OS). `ArgumentsSpec.EscapeCharacters` becomes `var e = ArgumentEscaper.EscapeInner(v); return ArgumentEscaper.NeedsQuoting(v) ? $"\"{e}\"" : e;`; `EscapeCharactersWithoutWrapping` calls `EscapeInner` per value before the existing group-wrap. Remove the `using CliInvoke.Core.Internal;` import if it was only for ShellArgumentEscaper.
- **Constraints**: `ArgumentEscaper` is internal (only `ArgumentsSpec` consumes it) per D002 minimize-public-surface. It must not reference `ShellArgumentEscaper` (no Core→Specializations dependency). Existing Windows builder tests (drop newlines, double quotes, pass through `\`/`\t`) must keep passing. The enumerable group-wrap behavior is unchanged.
- **Cites**: D002, D005, T003, T006.

<!-- next-d: D010 -->
<!-- next-i: I003 -->
<!-- next-t: T008 -->
