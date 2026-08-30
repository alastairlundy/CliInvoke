# ADR 0001: IVT-Minimization Principle

- **Status:** Accepted
- **Date:** 2026-08-28
- **Deciders:** CliInvoke maintainers
- **Related ledger records:** `DECISIONS-CliInvoke-v3-internals-visibility.md`

## Context

CliInvoke is split across several packages (Core, CliInvoke, Extensions, Specializations). Historically these packages exposed their internals to one another through broad `InternalsVisibleTo` (IVT) grants so that consumers could reach specific internal helpers. Because .NET IVT is assembly-scoped, a single grant leaks the *entire* internal surface to the friend assembly, creating cross-package coupling points and a "polyfill leakage" failure mode where a granting package's internal helper types appear in the signatures of IVT-exposed internals.

The goal is tighter encapsulation and fewer coupling points. This ADR records the governing principle and the version/reduction decisions so contributors understand when a new IVT grant is justified.

## Decision

### Legitimacy principle

A CliInvoke package shall grant IVT only to assemblies that **strictly require it**. Where a consumer needs only specific internal types, those types shall be **promoted to a public stable API or relocated to a shared package** so the IVT grant is **removed rather than narrowed**.

- **Driver:** the user wants tighter encapsulation; .NET IVT is assembly-scoped, so by-need must be achieved via grant-minimization plus promoting or relocating the specific required types.
- **Resolution:** Option A — Minimize + promote/relocate: drop unneeded grants; promote specific needed types to public API or a shared package.
- **Normalized requirement:** A CliInvoke package shall grant IVT only to assemblies that strictly require it; where a consumer needs only specific internal types, those types shall be promoted to a public stable API or relocated to a shared package so the IVT grant is removed rather than narrowed.

### Reduction aggressiveness

In this pass, remove the unused IVT grants now; schedule the reduction of *required* grants as follow-up work sequenced behind the principle.

- **Driver:** the user wants fewer coupling points without premature breaking public-API changes.
- **Resolution:** Option B — Unused now, required later: remove unused grants now; schedule required-grant reduction after the principle is set.
- **Normalized requirement:** Remove unused IVT grants in this pass. Reduction of the required grants shall be scheduled as follow-up work sequenced behind the principle.

### Test-assembly IVT scope

IVT grants to **test assemblies** are **not** a blanket exception: a test-assembly grant is kept only while it is actually consumed, and an unused test-assembly grant is removed in this pass (applies to test grants too).

- **Driver:** the user's goal is fewer coupling points in general; a dead test grant is still a coupling point even though the test project is same-repo and not the shipping blast radius described in I001.
- **Resolution:** Option B — Used test grants stay, unused removed: keep IVT to test assemblies that are actually consumed; remove test grants that no longer have a consumer.
- **Normalized requirement:** IVT grants to test assemblies (CliInvoke.Tests, CliInvoke.Specializations.Tests) remain only while actually used; an unused test-assembly grant is removed in this pass. The principle applies to test grants on a used/unused basis.

### Mechanism for required grants

For each required IVT grant, the reduction mechanism (promote the specific needed type to public API, relocate it to CliInvoke.Core, or another approach) shall be decided **per internal type**, applying the principle case-by-case.

- **Driver:** the user wants the promote-vs-relocate decision made deliberately per type, not by a single global rule.
- **Resolution:** Decide per type whether to promote to public, relocate to Core, or otherwise.
- **Normalized requirement:** For each required IVT grant, the reduction mechanism shall be decided per internal type during the follow-up work, applying case-by-case. No single global mechanism is mandated.

### Version / breaking-change window

The version window is applied per type: any type **promoted to public API** requires the **v3 major-version window** (SemVer major); types **relocated to Core** are **non-breaking** for consumers.

- **Driver:** the user wants the version-window decision to follow from the per-type mechanism choices.
- **Resolution:** Deferred in the original decision pending the per-type analysis; once known, apply per-type — public promotion = v3 major, Core relocation = non-breaking.
- **Normalized requirement:** The window shall be applied per-type: any type promoted to public API requires the v3 major-version window; types relocated to Core are non-breaking for consumers. Finalize when the per-type list is known.

### Documentation without a CI guard

The IVT-minimization principle is recorded in this ADR and in `CONTRIBUTING.md`. **No CI guard** is added to enforce it (Option B).

## Consequences

- New IVT grants require explicit justification; reviewers should challenge any grant that could be replaced by promotion/relocation.
- Unused grants are removed aggressively; used test-assembly grants stay while unused ones are removed.
- Required grants are reduced deliberately per type, respecting the v3 breaking-change window for public promotions.
- Contributors have clear guidance (this ADR + `CONTRIBUTING.md`) but no automated enforcement, keeping the rule a reviewed convention rather than a build gate.
