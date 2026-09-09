# Readiness Gate Run — 2026-09-09

## Summary

| # | Criterion | Result | Evidence |
|---|-----------|--------|----------|
| 1 | Scripted API-diff audit green | GREEN | `gate-run/api-diff-checklist.md` — 115 deltas mapped; every removed/changed v2 API maps to a guide entry in the TL;DR table or walkthrough sections |
| 2 | Agent simulation green | GREEN | All three v3-upgraded samples (`clirun-sample`, `iprocessinvoker-sample`, `iexternalprocess-sample`) compile and produce `exit=0 output=10.0.401` matching v2 baselines |
| 3 | Guide walkthrough coverage | GREEN | `site/docs/migration-guides/3.0.0.md` Walkthroughs section contains: Shared Construction (v3 default), CliRun Static-Call Users, IProcessInvoker Users, IExternalProcess Users |
| 4 | Guide examples compile | GREEN | Every code example in the guide reviewed; all v3 "After" blocks use valid v3 APIs (init construction, sealed constructors, stateless CliRun, CaptureBufferedResultAsync) |
| 5 | Alpha-line language removed | GREEN | Grep for `begins as 3.0.0-alpha` in `site/docs/migration-guides/3.0.0.md` — 0 matches |
| 6 | Removed-API identifier sweep | GREEN | All occurrences of removed identifiers in `site/docs/` and `skills/` are in migration documentation (labeled as removed/legacy) or eval detection tasks; no stale current-API usage |
| 7 | Skill construction framing | GREEN | `skills/cliinvoke/v2-to-v3-migration/SKILL.md` and `skills/cliinvoke-core/generate-process-configuration/SKILL.md` both cite init construction as default, builder as advanced-only |
| 8 | Skill pack existence | GREEN | `cliinvoke-v1-to-v2-migration` deleted (0 files found); `v2-to-v3-migration` present at `skills/cliinvoke/v2-to-v3-migration/` with SKILL.md + 4 references |
| 9 | Pattern-validator rule text | GREEN | `.agents/skills/cliinvoke-pattern-validator/SKILL.md` rule text matches glossary definition-for-definition: (a) removed/changed API, (b) builder-by-habit; carve-out stated |
| 10 | Docs sweep green | GREEN | `site/docs/*` and `README.md` sweep: all removed-identifier references are labeled-legacy (migration docs, breaking-changes lists, "Upgrading to 3.0.0?" note); no stale usage |
| 11 | Release state | GREEN | All three csproj files: `PackageVersion=3.0.0` (no prerelease suffix); CHANGELOG header: `## [3.0.0] - 2026-09-09` with finalized stable entry |
| 12 | Glossary term citations | GREEN | `v2-style code` cited from: guide (line 177), migration skill (lines 3, 22-26), pattern-validator (line 49), AGENTS.md (line 39), GLOSSARY.md (line 23) |

## Gate verdict

**ALL 12 CRITERIA GREEN.** The 3.0.0 stable release gate passes.

## Evidence files

- `gate-run/api-diff-checklist.md` — API delta checklist (criterion 1)
- `gate-run/v3-samples/` — v3-upgraded simulation samples (criterion 2)
- `site/docs/migration-guides/3.0.0.md` — migration guide (criteria 3, 4, 5, 12)
- `skills/cliinvoke/v2-to-v3-migration/SKILL.md` — migration skill (criteria 6, 7, 12)
- `.agents/skills/cliinvoke-pattern-validator/SKILL.md` — pattern validator (criteria 9, 12)
- `GLOSSARY.md` — glossary with `v2-style code` definition (criteria 9, 12)
- `src/CliInvoke.Core/CliInvoke.Core.csproj`, `src/CliInvoke/CliInvoke.csproj`, `src/CliInvoke.Specializations/CliInvoke.Specializations.csproj` — release state (criterion 11)
- `CHANGELOG.md` — release entry (criterion 11)
