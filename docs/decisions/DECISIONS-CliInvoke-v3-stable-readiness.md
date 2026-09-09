# Decision Ledger — CliInvoke v3 stable readiness

Session: technical-grilling over the v3 stable readiness plan (v2→v3 migration guide, skills update, v3-stable docs), re-baselined against repo state `3.0.0-alpha` line with a frozen public API surface.

### [D001] - session goal

- **Driver**: the user wants v3 stable consumable by both humans and AI agents — one self-sufficient upgrade guide for v2 users, v3-first fluency for agents, and a ready-to-ship stable release.
- **Resolved Answer**: "Goals: (1) A user should be able to use just the migration guide to safely upgrade from v2 to v3. (2) Agents shouldn't use CliInvoke v2 syntax or design patterns going forward — they should be able to use v3 syntax and design patterns. (3) Getting v3 ready for stable release."
- **Normalized Requirement**: The session shall resolve the v3 stable readiness plan — the v2→v3 migration guide, the repo skills (`.agents/skills/`), the user-facing skill packs (`skills/cliinvoke`, `skills/cliinvoke-core`), and the v3-stable docs sweep — into decision records an implementer can act on without re-asking the user.
- **Constraints**: track: concept-then-implementation. User clarification (recorded at branch-open time): **all public-facing documentation must be updated to v3** — this makes the v3 docs sweep a first-class in-scope work item alongside the guide and skills. Public API surface is frozen — no API changes are in scope. Locked from `DECISIONS-CliInvoke-v4-improvements.md` per Gate A: GA construction story (D005/D006/D007, T005/T006) and builder positioning (T006); confirmed locked by the user this session and recorded as D002/D003. In scope: 3 repo skills (`cliinvoke-inner-loop`, `cliinvoke-pattern-validator`, `cliinvoke-publish-and-package`), user-facing packs (`skills/cliinvoke` incl. the existing `cliinvoke-v1-to-v2-migration` skill, `skills/cliinvoke-core`), and the existing partial guide `site/docs/migration-guides/3.0.0.md` (103 lines, alpha-line breaking changes only — the GA construction story is absent). Ledger path: `docs/decisions/DECISIONS-CliInvoke-v3-stable-readiness.md` — confirmed by proposal, no objection.

### [D002] - locked: GA construction story

- **Driver**: locked item carried in from Gate A.
- **Resolved Answer**: Resolved (by provided spec) — `DECISIONS-CliInvoke-v4-improvements.md#D005`, `#D006`, `#D007`
- **Normalized Requirement**: `ProcessConfigurationFactory` is removed; init construction with `required` `TargetFilePath` is the one documented construction path; the final construction shape ships at v3 GA with no deprecation ceremony.
- **Constraints**: confirmed locked by the user this session; not re-grilled. Constrains migration-guide content (D004) and skill teaching material (D005).

### [D003] - locked: builder positioning

- **Driver**: locked item carried in from Gate A.
- **Resolved Answer**: Resolved (by provided spec) — `DECISIONS-CliInvoke-v4-improvements.md#T006`
- **Normalized Requirement**: Docs position init construction as the default and `ProcessConfigurationBuilder` as advanced (argument escaping, `UserCredentialSpec`/resource-policy callback flows); the builder remains in the main package.
- **Constraints**: confirmed locked by the user this session; not re-grilled. Constrains migration-guide wording (D004) and skill references (D005).

### [D004] - migration guide shape

- **Driver**: the user wants v2 users to be able to upgrade using only the guide; only a shape that provably covers both common and rare breaking surfaces satisfies goal 1.
- **Resolved Answer**: "D004: Option A" — Catalog + task paths (hybrid)
- **Normalized Requirement**: The v2→v3 migration guide shall combine an exhaustive breaking-change catalog (TL;DR table) with task-oriented walkthroughs per v2 usage pattern, and its completeness shall be audited against the frozen v3 public API surface.
- **Constraints**: Subsumes and extends the existing `site/docs/migration-guides/3.0.0.md` alpha-line draft. The catalog audit is mechanical because the API surface is frozen (D001). Self-sufficiency verification method decided separately (D008).

### [D005] - agent skill target state

- **Driver**: the user wants agents to stop emitting v2 patterns at the source AND to modernize existing v2 code; a guard alone fails the first half, a rewrite alone fails the second.
- **Resolved Answer**: "D005: Option A" — v3-first rewrite + v2-to-v3 migration skill
- **Normalized Requirement**: The user-facing packs (`skills/cliinvoke`, `skills/cliinvoke-core`) shall be rewritten to teach v3 syntax (init construction as default, builder as advanced per D003), and a v2-to-v3 migration skill shall be added mapping v2 patterns to v3 replacements.
- **Constraints**: Every reference file re-reviewed against the frozen surface. Fate of the existing `cliinvoke-v1-to-v2-migration` skill decided separately (D007). Repo skill `cliinvoke-pattern-validator` updates included in scope.

### [D006] - stable-readiness definition

- **Driver**: the user wants the release itself shipped, but with verifiable readiness rather than an ad-hoc "done" judgment.
- **Resolved Answer**: "D006: Option A" — Criteria gate, then release sweep
- **Normalized Requirement**: Explicit stable-readiness acceptance criteria shall be defined first; the release sweep (all public-facing docs updated to v3 per D001, alpha-line language removed, changelog finalized, version 3.0.0 stable) shall be executed and verified against them.
- **Constraints**: Criteria must trace to goals 1 and 2 (D001) — readiness is proven, not assumed. Criteria enumeration is Phase 2 work.

### [D007] - legacy v1→v2 artifact fate

- **Driver**: goal 2 governs what agents are taught — a v1→v2 skill would generate migration to a non-current major; static docs carry no such risk.
- **Resolved Answer**: "D007: Option B" — Retire the skill, keep the guide
- **Normalized Requirement**: `skills/cliinvoke/cliinvoke-v1-to-v2-migration` shall be deleted (superseded by the new v2-to-v3 migration skill, D005); the site's v1→v2 migration guide shall remain, clearly labeled for legacy users.
- **Constraints**: The guide labeling must not confuse the v2→v3 guide's audience. No inbound-link breakage from the docs side.

### [D008] - self-sufficiency verification method

- **Driver**: goal 1 has a coverage half ("safely") and a self-sufficiency half ("just the migration guide") — each needs its own evidence.
- **Resolved Answer**: "D008: Option C" — Diff audit + simulation
- **Normalized Requirement**: Stable readiness (D006) shall be evidenced by two streams: a mechanical API-diff audit proving every frozen-surface delta appears in the guide, and an agent-driven simulation upgrading a sample v2 project using only the guide (success = compiles and behaves correctly).
- **Constraints**: Both streams must pass for the criteria gate. Simulation sample selection and the diff-audit tooling are Phase 2 work.

### [D009] - normative vocabulary for "v2 patterns"

- **Driver**: the guide, migration skill, and pattern-validator must point at a single definition instead of three; goal 2 names syntax and design patterns in one breath.
- **Resolved Answer**: "D009: Option B" — Broaden the term (glossary revision pending exact-definition confirmation)
- **Normalized Requirement**: `v2-style code` shall be redefined in `GLOSSARY.md` to cover both removed/changed API usage (surface) and defaulting to v3-advanced construction styles (e.g., builder-first where init construction suffices), with builder-as-advanced explicitly legitimate per D003; all three artifact families shall adopt the term.
- **Constraints**: Supersedes: prior `GLOSSARY.md` definition of `v2-style code` (surface-only, pre-dating this ledger). The builder carve-out must be stated to respect D003. Definition confirmed by the user on 2026-09-08 with one revision: consumer-agnostic (no named consumers; artifacts cite the glossary). Glossary entry written and verified to match this record's definition.

### [T001] - foundation items

- **Driver**: the user wants release readiness without structural churn; the release is about docs and skills accuracy, not code shape.
- **Resolved Answer**: "T001: Foundation locked" — Option A — Locked in place
- **Normalized Requirement**: All foundation items are locked by repo state — C#/.NET 10, the three shipping projects (Core, CliInvoke, Specializations), no new packages; readiness artifacts are markdown in `site/docs/` and `skills/`.
- **Constraints**: No new dependencies or toolchain additions; repo conventions inherited as-is.
- **Cites**: D001

### [T003] - diff-audit method

- **Driver**: the user's goal-1 evidence must come from the binaries themselves — "every delta appears in the guide" is only provable when the delta list comes from the assemblies, not prose.
- **Resolved Answer**: "T003: Option B" — Scripted public-API diff
- **Normalized Requirement**: The D008 stream-1 audit shall extract the public API surface from the v2 and v3 assemblies by reflection, diff them, and require every delta to map to at least one guide entry.
- **Constraints**: A one-off extraction script is acceptable tooling; the frozen surface (D001) keeps the delta finite. The audit's green/red output feeds the D006 criteria gate (shape decided in T002).
- **Cites**: D001, D004, D008

### [T002] - criteria gate structure

- **Driver**: the user wants a provably ready release — a concrete, checkable pool with no advisory escape hatch and no deferrable items.
- **Resolved Answer**: "Option A" — One flat blocking gate (re-opened once for concrete options; resolved on the 12-item criteria pool)
- **Normalized Requirement**: The 12-criteria pool (guide: 1–5; skills: 6–9; docs sweep: 10–12) shall form one flat blocking gate — the release blocks until every criterion is green; the pool is enumerated in the blueprint.
- **Constraints**: Criterion wording is finalized in the blueprint against this record. Re-open history: first emission offered abstract gate-shape options; user requested concrete options; re-opened with the concrete pool.
- **Cites**: D001, D002, D003, D004, D005, D006, D007, D008, D009, T003

### [T004] - v2-to-v3 migration skill structure

- **Driver**: the user wants agents to upgrade v2 code from one surface — skill references mirror the guide's walkthroughs so the two artifacts stay auditable against each other.
- **Resolved Answer**: "T004: Option A" — One self-contained skill
- **Normalized Requirement**: The v2-to-v3 migration skill shall live at `skills/cliinvoke/v2-to-v3-migration/` with a SKILL.md plus one reference per usage pattern, each mapping v2-style code to v3 replacements, citing the `GLOSSARY.md` `v2-style code` term (D009).
- **Constraints**: Some mapping content is intentionally duplicated between skill and guide (accepted cost). Detection logic stays in-skill rather than leaning on `cliinvoke-pattern-validator`.
- **Cites**: D004, D005, D007, D009

### [T005] - docs-sweep file treatment

- **Driver**: the pages users actually land on need coherent v3 stories; the rest must at minimum lose v2-era content.
- **Resolved Answer**: "T005: Option B" — Rewrite high-traffic, refresh the rest
- **Normalized Requirement**: Full rewrites for the high-traffic pages (`getting-started*`, `configuration`, `choosing-invocation-pattern`); targeted v3 refresh for every other public-facing doc (`site/docs/**`, `README.md`); per-file verdicts enumerated in the blueprint.
- **Constraints**: Rewrites must not drift from refreshed pages' terminology — shared vocabulary comes from `GLOSSARY.md` (D009). No pruning was chosen; files about removed concepts are refreshed to state their removal rather than deleted.
- **Cites**: D001, D002, D007, T001

### [T007] - v2 usage-pattern taxonomy

- **Driver**: the user wants one coherent migration story aligned with the repo's canonical partition; v2 users self-identify by invocation pattern, and the construction change cuts across all three.
- **Resolved Answer**: "Option A" — Three patterns, construction orthogonal
- **Normalized Requirement**: The migration guide's walkthrough units, T002 criterion 3, and the T006 simulation samples shall be partitioned by the canonical three invocation patterns (`CliRun`, `IProcessInvoker`, `IExternalProcess`, per `choosing-invocation-pattern.md`); init-vs-builder-vs-DI construction migration is an orthogonal dimension handled within each walkthrough plus a shared construction section.
- **Constraints**: Opened when the user asked where the "5 usage patterns" came from — the 5-way split was the agent's ungrounded synthesis. T002 criterion 3 rewords to "each of the 3 canonical invocation patterns + shared construction section"; the blueprint's criteria pool must reflect this. No new glossary term — "invocation pattern" is already canon.
- **Cites**: D001, D002, D003, D004, T002, T005, T006

### [T006] - simulation sample selection

- **Driver**: the user wants per-pattern attribution — a green run certifies each walkthrough unit the guide claims to cover.
- **Resolved Answer**: "T006: Option B" — Three pattern samples
- **Normalized Requirement**: The D008 stream-2 simulation shall upgrade three small committed v2 sample projects — one per canonical invocation pattern (T007), each exercising that pattern's construction migration — independently, with success per sample defined as compiles-and-behaves using only the guide.
- **Constraints**: Samples are committed in-repo for gate repeatability. Runner mechanics and harness locations are blueprint work. Re-open history: user first asked where the "5 usage patterns" came from; clarified, taxonomy resolved in T007, branch re-issued.
- **Cites**: D001, D004, D008, T002, T007

### [T008] - output format

- **Driver**: the user wants decisions and implementation detail in separate, scannable artifacts, consistent with both prior grilling sessions.
- **Resolved Answer**: "T008: Option A" — Implementation Blueprint
- **Normalized Requirement**: The session shall produce a standalone blueprint at the repo root with a Scope Binding section linking the ledger, inline `filename#Dxxx/Txxx` citations for every technical statement, and a Ledger Reference section.
- **Constraints**: Filename confirmed by the user: `IMPLEMENTATION-v3-stable-readiness.md` at the repo root. Downstream consumer recorded in T009.
- **Cites**: D001, D002, D003, D004, D005, D006, D007, D008, D009, T001, T002, T003, T004, T005, T006, T007

### [T009] - downstream consumer

- **Driver**: the user takes the artifacts and drives the v3-stable sweep themselves, as in both prior sessions.
- **Resolved Answer**: "T009: Option C" — Manual handoff
- **Normalized Requirement**: No automated ticket or issue decomposition follows the blueprint; the user drives downstream work from `IMPLEMENTATION-v3-stable-readiness.md` and the ledger.
- **Constraints**: The agent does not launch downstream workflows.
- **Cites**: T008

### [I001] - output target and pull-request count

- **Prompt**: Where should the tickets be published — GitHub Issues (the repo's configured tracker) or local markdown files? And should this land as one pull request or multiple?
- **User Response**: "Local markdown files"; "1 PR"
- **Resolution**: Tickets will be written as local markdown files under `tickets/` at the repo root; the decomposition anticipates a single pull request, so tickets are grouped under one set with no PR-level partition.
- **Notes**: Asked during spec-to-tickets Step 5. The user's explicit request to decompose to tickets supersedes T009's manual-handoff constraint for this session.

<!-- next-d: D010 -->
<!-- next-t: T010 -->
<!-- next-i: I002 -->
