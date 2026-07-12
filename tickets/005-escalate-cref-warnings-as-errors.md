---
title: Escalate targeted cref warnings as errors in Directory.Build.props
classification: Independent
blocked_by: []
parent: docs/decisions/DECISIONS-CliInvoke-process-configuration-shape.md
---

## Goal

Make the build fail on broken `<see cref="..." />` cross-references in XML doc comments, without the broader disruption of global `<TreatWarningsAsErrors>`. This is the build-config counterpart to `004-add-remarks-block-to-wrapper` — the wrapper's cref to `ProcessConfigurationBuilder` (and any other cref in the project) gets build-failure protection.

## What to build

In `Directory.Build.props`, add a new `<WarningsAsErrors>` element to the existing `<PropertyGroup>` (lines 2–6) with the following content:

```xml
<WarningsAsErrors>CS1574;CS1580;CS1581;CS1584;CS1658;CS1734;CS1762</WarningsAsErrors>
```

The list applies project-wide. The wrapper's remarks block (added by `004-add-remarks-block-to-wrapper`) is the primary motivator, but the protection is not scoped to the wrapper.

Existing `#pragma warning disable` blocks are unaffected: CS0618 at `ProcessConfiguration.cs:91–93`, CS8602 at `ProcessConfiguration.cs:217, 233`, and CA1416 at `ProcessConfigurationBuilder.cs:18, 445` continue to suppress those specific warnings.

## Recommended Workflow

### Step 1 — Add the WarningsAsErrors property

Where: `Directory.Build.props`

- Open the existing `<PropertyGroup>` (lines 2–6).
- Add a new line: `<WarningsAsErrors>CS1574;CS1580;CS1581;CS1584;CS1658;CS1734;CS1762</WarningsAsErrors>`.
- Leave the existing `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>` and `<GenerateDocumentationFile>true</GenerateDocumentationFile>` lines untouched.

Verify: Reading the file, the `<PropertyGroup>` now contains three child elements: the two existing ones and the new `<WarningsAsErrors>` line.

### Step 2 — Confirm the build succeeds with all crefs valid

Where: N/A

- Run `dotnet test` from `tests/CliInvoke.Tests/` per the AGENTS.md working-directory convention.
- If `004-add-remarks-block-to-wrapper` has landed, the wrapper's `<see cref="ProcessConfigurationBuilder" />` cref is valid and the build must succeed.
- If `004-add-remarks-block-to-wrapper` has not yet landed, the build must still succeed because all existing crefs in the codebase are valid.

Verify: The build succeeds on net8.0, net9.0, and net10.0 with no new errors.

### Step 3 — Verify escalation with a temporary broken cref

Where: any `.cs` file with an XML doc that already has a `<see cref="..." />` (e.g., `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` once `004-add-remarks-block-to-wrapper` has landed)

- Introduce a temporary broken cref (e.g., `<see cref="NonExistentClass" />` in the wrapper's remarks block, or in any existing cref site).
- Run `dotnet test` and confirm the build fails with one of the listed warning codes (CS1574, CS1580, CS1581, CS1584, CS1658, CS1734, CS1762).
- Revert the broken cref.

Verify: A broken cref causes the build to fail with a cref-related warning promoted to an error; reverting restores the green build. The temporary broken cref is removed before the ticket is marked complete.

## Context pointers

**Files**
- `Directory.Build.props` — the file being modified; the `<PropertyGroup>` is at lines 2–6.
- `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs` — adjacent file with `#pragma warning disable` blocks (CS0618 at lines 91–93, CS8602 at lines 217 and 233); these are unaffected by the new `<WarningsAsErrors>` line.
- `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs` — adjacent file with a CA1416 pragma on lines 18 and 445; unaffected. The wrapper's remarks block (added by `004-add-remarks-block-to-wrapper`) is the primary target of the new escalation.

**ADRs** — None directly relevant.

**Domain terms**
- cref — an XML doc cross-reference. A broken cref (a target that the compiler cannot resolve) emits one of the CS1574, CS1580, CS1581, CS1584, CS1658, CS1734, or CS1762 warnings. With `<GenerateDocumentationFile>true</GenerateDocumentationFile>` already enabled in `Directory.Build.props:5`, the warning is emitted; this ticket escalates it to an error.

**Ledger records**
- `DECISIONS-CliInvoke-process-configuration-shape.md#D008` — the remarks block format choice; escalation protects the remarks block from silently broken crefs.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D009` — the remarks block scope; escalation applies project-wide but is motivated by the remarks block.
- `DECISIONS-CliInvoke-process-configuration-shape.md#D010` — the `<see cref="ProcessConfigurationBuilder" />` cross-reference; escalation guarantees the cref points to a real type at build time.
- `DECISIONS-CliInvoke-process-configuration-shape.md#T003` — the targeted list of warning codes: CS1574, CS1580, CS1581, CS1584, CS1658, CS1734, CS1762. The list must be reviewed during .NET upgrades to catch any new codes the compiler adds.

## Acceptance criteria

- [ ] `Directory.Build.props` contains a `<WarningsAsErrors>` element with the value `CS1574;CS1580;CS1581;CS1584;CS1658;CS1734;CS1762`, per `DECISIONS-CliInvoke-process-configuration-shape.md#T003`.
- [ ] The existing `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>` and `<GenerateDocumentationFile>true</GenerateDocumentationFile>` lines are preserved.
- [ ] The existing `#pragma warning disable` blocks are unaffected: CS0618 at `ProcessConfiguration.cs:91–93`, CS8602 at `ProcessConfiguration.cs:217, 233`, and CA1416 at `ProcessConfigurationBuilder.cs:18, 445` continue to suppress those warnings.
- [ ] The build succeeds on net8.0, net9.0, and net10.0 with all valid crefs.
- [ ] A temporary broken cref (e.g., `<see cref="NonExistentClass" />`) causes the build to fail with one of the listed warning codes, per `DECISIONS-CliInvoke-process-configuration-shape.md#T003`. The temporary change is reverted before the ticket is marked complete.

## Dependencies

**Blocked by** — None; this ticket can be done at any time. The blueprint's recommended order is Step 1 → Step 2 → Step 3 → Step 4 → Step 5, but Step 5 (`005-escalate-cref-warnings-as-errors`) is independent and can be moved earlier if desired.
