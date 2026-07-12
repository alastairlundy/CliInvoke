---
title: Add remarks block to BuilderProcessConfiguration
classification: Independent
blocked_by: [003-rename-wrapper-to-builder-process-configuration]
parent: docs/decisions/DECISIONS-CliInvoke-process-configuration-shape.md
---

## Goal

Add an agent-facing XML doc `<remarks>` block to the renamed `BuilderProcessConfiguration` class so that future maintainers and AI agents understand (a) why the wrapper exists, (b) why it must not be deleted, and (c) that a long-term solution to eliminate the wrapper is being developed. The block uses three `<para>` elements, a single `<see cref="ProcessConfigurationBuilder" />` cross-reference, and no references to any decision-ledger record.

## What to build

In `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`, add an XML doc to the renamed `BuilderProcessConfiguration` class (line 426) with the following structure:

- A minimal `<summary>` — "An internal subclass of `ProcessConfiguration` used by `ProcessConfigurationBuilder` to invoke the protected multi-parameter constructor."
- A `<remarks>` block containing three `<para>` elements:
  1. The ctor-availability problem statement: the builder lives in a different assembly (`CliInvoke`) than `ProcessConfiguration` (`CliInvoke.Core`), the multi-parameter constructor is `protected`, and this wrapper is the legitimate cross-assembly access path.
  2. The no-delete rule: "Do not delete this class without first choosing a long-term replacement."
  3. A plain-language mention that a long-term solution to eliminate the wrapper is being developed, with a `<see cref="ProcessConfigurationBuilder" />` cross-reference to the only consumer.

The block must not reference any `Dxxx` ID or any decision-ledger filename (per `DECISIONS-CliInvoke-process-configuration-shape.md#D009`). The plain-language long-term mention survives any future ledger rename.

This ticket assumes that `005-escalate-cref-warnings-as-errors` may run in parallel; if it has not yet been applied, a temporary broken-cref test cannot be run, but a valid-cref build is sufficient to confirm the remarks block compiles.

## Recommended Workflow

### Step 1 — Add the XML doc to the renamed class

Where: `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- Open the renamed `BuilderProcessConfiguration` class declaration (line 426, after `003-rename-wrapper-to-builder-process-configuration` has landed).
- Insert the `<summary>` and `<remarks>` block immediately above the class declaration.
- Ensure the three `<para>` elements are present, with the `<see cref="ProcessConfigurationBuilder" />` in the third `<para>` per `DECISIONS-CliInvoke-process-configuration-shape.md#D010`.
- Ensure no `Dxxx` ID, ledger filename, or any decision-ledger content is referenced in the block per `DECISIONS-CliInvoke-process-configuration-shape.md#D009`.

Verify: Reading the file, the class has a `<summary>` and a `<remarks>` block with three `<para>` elements, and the only structured cross-reference is `<see cref="ProcessConfigurationBuilder" />` in the third `<para>`.

### Step 2 — Confirm the build succeeds

Where: N/A

- Run `dotnet test` from `tests/CliInvoke.Tests/` per the AGENTS.md working-directory convention.
- If `005-escalate-cref-warnings-as-errors` has already landed, the build must succeed with all crefs valid.

Verify: The build succeeds on net8.0, net9.0, and net10.0; no new warnings or errors related to the new XML doc appear.

### Step 3 — (Optional) Verify cref-warning escalation

Where: `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`

- If `005-escalate-cref-warnings-as-errors` has landed, introduce a temporary broken cref (e.g., `<see cref="NonExistentClass" />`), confirm the build fails, then revert.
- If `005-escalate-cref-warnings-as-errors` has not yet landed, skip this step — the escalation itself is verified in that ticket.

Verify: A broken cref fails the build (when escalation is in force); reverting restores the green build.

## Context pointers

**Files**
- `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` — the file being modified; the renamed `BuilderProcessConfiguration` class is at line 426. The CA1416 pragma on lines 18 and 445 is unaffected.

**ADRs** — None directly relevant. The cross-references in the remarks block point to source code only.

**Domain terms**
- Wrapper — an internal subclass of `ProcessConfiguration` whose only purpose is to call the `protected` 15-param ctor from a different assembly. The wrapper is the legitimate cross-assembly access path; a future direction may eliminate it (`DECISIONS-CliInvoke-process-configuration-shape.md#D006`, deferred).
- Resolution slot — see `001-move-arguments-null-check`'s context pointers; not directly relevant to this ticket beyond noting that the wrapper exists because the resolution slot's surrounding ctor is `protected`.

**Ledger records**
- `DECISIONS-CliInvoke-process-configuration-shape.md#D008` — the remarks block is the only agent-facing comment on the class; the `<summary>` is minimal and the block uses `<remarks>` (not an extended `<summary>` and not an inline `// note:` block).
- `DECISIONS-CliInvoke-process-configuration-shape.md#D009` — the three required elements: ctor-availability problem statement, no-delete rule, and a plain-language long-term mention; no `Dxxx` ID or ledger filename is referenced.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D010` — the single `<see cref="ProcessConfigurationBuilder" />` cross-reference, placed in the third `<para>`. The base type is already obvious from the `: ProcessConfiguration` declaration, so no second cref is added.
- `DECISIONS-CliInvoke-process-configuration-shape.md#T004` — the three-`<para>` structure: one paragraph per D009 element, with the cref in the third.

## Acceptance criteria

- [ ] The renamed `BuilderProcessConfiguration` class at `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs:426` carries a `<summary>` and a `<remarks>` block, per `DECISIONS-CliInvoke-process-configuration-shape.md#D008`.
- [ ] The `<remarks>` block contains exactly three `<para>` elements, per `DECISIONS-CliInvoke-process-configuration-shape.md#T004`:
  1. The ctor-availability problem statement, per `DECISIONS-CliInvoke-process-configuration-shape.md#D009`.
  2. The no-delete rule ("do not delete this class without first choosing a long-term replacement"), per `DECISIONS-CliInvoke-process-configuration-shape.md#D009`.
  3. A plain-language long-term mention, per `DECISIONS-CliInvoke-process-configuration-shape.md#D009`.
- [ ] The third `<para>` contains a `<see cref="ProcessConfigurationBuilder" />` cross-reference, per `DECISIONS-CliInvoke-process-configuration-shape.md#D010`.
- [ ] The remarks block contains no `Dxxx` ID and no decision-ledger filename, per `DECISIONS-CliInvoke-process-configuration-shape.md#D009`.
- [ ] The `<summary>` is minimal (does not duplicate the remarks content), per `DECISIONS-CliInvoke-process-configuration-shape.md#D008`.
- [ ] The build succeeds on net8.0, net9.0, and net10.0; no new warnings or errors are introduced by the new XML doc.
- [ ] If `005-escalate-cref-warnings-as-errors` has already landed, a temporary broken cref in the remarks block fails the build (verified by introducing and reverting), per `DECISIONS-CliInvoke-process-configuration-shape.md#T003`.

## Dependencies

**Blocked by** — `003-rename-wrapper-to-builder-process-configuration` (the class must be renamed first, because the remarks block documents the renamed `BuilderProcessConfiguration` and the file no longer references `ProcessConfigurationWrapper` after that ticket lands).
