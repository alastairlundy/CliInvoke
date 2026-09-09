# Implementation Blueprint — CliInvoke v3 stable readiness

## Scope Binding

- **Linked Spec**: `docs/decisions/DECISIONS-CliInvoke-v3-stable-readiness.md` (goal record `D001` — the session goals serve as the spec; no separate spec file exists)
- **Decision Ledger**: `docs/decisions/DECISIONS-CliInvoke-v3-stable-readiness.md`
- **Notice**: This blueprint is a context pointer valid ONLY for the linked spec and must not be applied to other specifications without explicit authorization.
- **Locked inputs**: The GA construction story and builder positioning are locked by `DECISIONS-CliInvoke-v4-improvements.md#D005`, `#D006`, `#D007`, `#T005`, `#T006` (recorded here as `#D002`, `#D003`). The public API surface is frozen — no API changes are in scope.

## Execution order

1. Verification tooling (Part 5) — build first so the gate is runnable.
2. Migration guide (Part 1).
3. Skills (Part 2).
4. Docs sweep (Part 3).
5. Release mechanics (Part 4).
6. Run the readiness gate (Part 0) — release blocks until all 12 criteria are green.

## Part 0 — Readiness gate [`DECISIONS-CliInvoke-v3-stable-readiness.md#T002`]

One flat blocking gate; the release blocks until every criterion is green. Wording finalized here (criterion 3 reworded per [`DECISIONS-CliInvoke-v3-stable-readiness.md#T007`]).

| # | Criterion | Check mechanism |
|---|-----------|-----------------|
| 1 | Scripted API-diff audit green — every v2→v3 public-API delta maps to ≥1 guide entry | `tools/api-diff.ps1` output reviewed against the guide's TL;DR table |
| 2 | Agent simulation green — all 3 pattern samples upgraded using only the guide; each compiles and behaves correctly | Simulation runs per sample |
| 3 | Guide has a task walkthrough for each of the 3 canonical invocation patterns (`CliRun`, `IProcessInvoker`, `IExternalProcess`) plus a shared construction section | Manual check against guide TOC |
| 4 | Every code example in the guide compiles against v3 stable | Compile example snippets (script or IDE pass) |
| 5 | Alpha-line release-state language removed from the guide (no "begins as 3.0.0-alpha.x…") | Grep `site/docs/migration-guides/3.0.0.md` |
| 6 | Zero removed-API identifiers (`ProcessConfigurationFactory`, `PipedProcessResult`, `CliRun.UseExternalProcessFactory`, `ExitConfiguration` setter usage, `Piped*Async` methods) in `site/docs/` and `skills/` outside labeled legacy content | Grep sweep with allowlist |
| 7 | Every SKILL.md and reference teaches init construction as default; `ProcessConfigurationBuilder` appears only as advanced | Grep + manual review of skills packs |
| 8 | `skills/cliinvoke/cliinvoke-v1-to-v2-migration/` deleted; `skills/cliinvoke/v2-to-v3-migration/` present | File existence check |
| 9 | `cliinvoke-pattern-validator` flags v2-style code per the broadened glossary definition | Manual review of the validator's rule text |
| 10 | No v2-era instructions in `site/docs/*` and `README.md` except the labeled legacy v1→v2 guide | Grep sweep + manual review |
| 11 | CHANGELOG finalized for 3.0.0 stable; `csproj` versions carry no prerelease suffix | Grep csproj + CHANGELOG header |
| 12 | `GLOSSARY.md` `v2-style code` entry present, cited from the guide, the migration skill, and the pattern-validator | Grep for term references |

## Part 1 — Migration guide [`DECISIONS-CliInvoke-v3-stable-readiness.md#D004`]

**File: `site/docs/migration-guides/3.0.0.md`** — extend the existing draft; it is the single v2→v3 guide.

1. **Catalog (TL;DR table)** — keep the existing alpha-line rows; add the GA construction story rows with inline citations:
   - `ProcessConfigurationFactory` removed — construct `ProcessConfiguration` directly with init setters [`#D002`, `#D006`-v4]
   - `TargetFilePath` becomes `required` — `[SetsRequiredMembers]` on the convenience constructor [`#T005`-v4]
   - `ArgumentsList` merged into init-only `ArgumentList` [`#T005`-v4]
   - Working-directory validation moved into `ProcessConfiguration` construction (throws on non-existent directory) [`#D006`-v4]
   - Builder kept, positioned advanced — escaping, `UserCredentialSpec`/resource-policy callbacks [`#D003`]
   - `ProcessConfigurationBuilder` init setters delegate where applicable; DI registration unchanged [`#T006`-v4]
2. **Task walkthroughs** — one per canonical invocation pattern plus a shared construction section [`#T007`]:
   - `CliRun` static-call users (facade changes, config now passed as the object)
   - `IProcessInvoker` users (factory removal fallout, DI `configure` bindings)
   - `IExternalProcess` users (constructor C only, sealed, `ExitConfiguration` at construction)
   - Shared construction section: init construction as the default; builder as the advanced path; DI unchanged [`#D002`, `#D003`]
   - Each walkthrough: "Before (v2.x)" and "After (v3)" code, compiles against v3 stable (criterion 4)
3. **Remove the alpha caveat** — delete the "begins as 3.0.0-alpha.x… will reach a stable 3.0.0" blockquote; state the set is final (criterion 5).
4. **Cross-links** — cite `GLOSSARY.md#v2-style code` where patterns are classified (criterion 12).

## Part 2 — Skills [`DECISIONS-CliInvoke-v3-stable-readiness.md#D005`, `#T004`]

### New skill
- **`skills/cliinvoke/v2-to-v3-migration/SKILL.md`** — trigger conditions, the `v2-style code` definition citation [`#D009`], the 3-pattern partition [`#T007`].
- **References, one per walkthrough unit** (mirroring the guide's structure for mutual auditability [`#T004`]):
  - `references/CliRun.md` — static-call replacements
  - `references/IProcessInvoker.md` — invoker/factory replacements
  - `references/IExternalProcess.md` — lifecycle/constructor replacements
  - `references/Construction.md` — init-first, builder-advanced mapping (builder methods → init properties table; keep-builder cases: escaping, `UserCredentialSpec`) [`#D003`]

### Deleted
- **`skills/cliinvoke/cliinvoke-v1-to-v2-migration/`** — removed wholesale [`#D007`] (criterion 8).

### Rewritten to v3-first [`#D005`]
- `skills/cliinvoke/select-execution-pattern/SKILL.md` + `references/CliRun.md`, `references/IProcessInvoker.md`, `references/IExternalProcessCreation.md` — replace v2 construction snippets with init construction; note middleware-asymmetric coverage [`#D005`-v4 context].
- `skills/cliinvoke-core/generate-process-configuration/references/ConfiguringWithBuilders.md`, `references/SettingValues.md` — rewrite init-first; builder only as advanced [`#D003`]; cover the `ArgumentsList` merge and `required` `TargetFilePath` [`#T005`-v4].
- `skills/cliinvoke-core/implement-resource-lifecycle/references/IExternalProcess.md`, `references/ProcessConfiguration.md` — v3 constructor seams, no-mutation contract, disposal guidance per README.
- `skills/cliinvoke/package-installation-choice` — refresh version guidance (3.0.0 stable, no prerelease).
- **`skills/evals/`** — update or remove evals referencing the deleted skill; add eval coverage for the migration skill.

### Repo skills [`.agents/skills/`]
- **`cliinvoke-pattern-validator/SKILL.md`** — rule text flags `v2-style code` per the broadened definition: removed/changed API usage plus builder-by-habit; carve-out for deliberate advanced-builder use [`#D009`, `#D003`] (criterion 9).
- **`cliinvoke-publish-and-package`** — update for 3.0.0 stable (no prerelease suffix; SourceLink/CI notes unchanged).
- **`cliinvoke-inner-loop`** — no content change expected; verify references still hold post-sweep.

## Part 3 — Docs sweep [`DECISIONS-CliInvoke-v3-stable-readiness.md#D001`, `#T005`]

**Full rewrites** (high-traffic pages get coherent v3 stories):
- `site/docs/getting-started.md`, `site/docs/getting-started-quickstart.md`, `site/docs/getting-started-readme.md` — init construction throughout, builder absent or advanced-labeled
- `site/docs/guides/configuration.md` — the construction story as the centerpiece [`#D002`, `#D003`]
- `site/docs/guides/choosing-invocation-pattern.md` — three-pattern canon, v3 examples, cross-link to the migration guide

**Targeted refresh** (keep structure, purge v2-era content):
- `site/docs/guides/architecture.md`, `guides/middleware.md`, `guides/resource-disposal.md`, `guides/troubleshooting.md`
- `site/docs/guides/migrating-from-sub-builder-interfaces.md` — refresh to state v3's final construction shape (verify the sub-builder seam's v3 status against source before writing)
- `site/docs/comparison.md`, `site/docs/everything-wrong-with-process-in-csharp.md`, `site/docs/building-cliinvoke.md`, `site/docs/Supported-OperatingSystems.md`
- `site/docs/readme.md`, `site/docs/guides/readme.md`, `site/docs/migration-readme.md`, `site/docs/migration-guides/readme.md`, `site/docs/guides/menu.yml`, `site/docs/menu.yml`, `site/docs/migration-guides/menu.yml` — labels, links, and any v2-era framing
- **`README.md`** (repo root) — v3 usage, resource-disposal guidance, version badges

**Legacy exception** [`#D007`]:
- `site/docs/migration-guides/migrating-v1-to-v2.md` + `site/docs/migration-guides/v1-to-v2/` — kept, clearly labeled "legacy — for users still on v1", and excluded from criterion 6/10 sweeps via the allowlist.

## Part 4 — Release mechanics [`DECISIONS-CliInvoke-v3-stable-readiness.md#D006`]

- **CHANGELOG.md** — finalize the 3.0.0 stable entry: consolidate the alpha-line entries plus the GA construction story (criterion 11).
- **csproj versions** — all four packages at `3.0.0` with no prerelease suffix (Core, CliInvoke, Specializations; Extensions is dissolved) (criterion 11).
- Release publishing follows the existing `cliinvoke-publish-and-package` skill; no workflow changes [`#T001`].

## Part 5 — Verification tooling [`DECISIONS-CliInvoke-v3-stable-readiness.md#T003`, `#T006`]

- **`tools/api-diff.ps1`** — reflection-based public-surface dump of the v2 and v3 assemblies, diff, and a checklist template mapping each delta to a guide entry. One-off script; no new packages (`System.Reflection.Metadata` via a throwaway console program is acceptable) [`#T003`]. Evidence feeds criterion 1.
- **`simulation/v2-samples/`** (repo root, excluded from `CliInvoke.sln`) — three small v2 sample projects pinned to CliInvoke 2.x, one per invocation pattern [`#T007`]:
  - `clirun-sample/` — static facade usage incl. config construction
  - `iprocessinvoker-sample/` — invoker + DI + middleware usage
  - `iexternalprocess-sample/` — direct lifecycle control
  - Each compiles against v2, upgrades to v3 using only the guide, and behavior-checks its output (criterion 2) [`#D008`].

## Ledger Reference

Cited records, all in `docs/decisions/DECISIONS-CliInvoke-v3-stable-readiness.md`:
`D001`, `D002`, `D003`, `D004`, `D005`, `D006`, `D007`, `D008`, `D009`
`T001`, `T002`, `T003`, `T004`, `T005`, `T006`, `T007`, `T008`, `T009`

Locked external records in `docs/decisions/DECISIONS-CliInvoke-v4-improvements.md`:
`D005`, `D006`, `D007`, `T005`, `T006` (cited above as `#D002`/`#D003` equivalents where relevant).
