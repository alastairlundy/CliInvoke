---
title: ConfigurationExtensions redirect-collapse documentation
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Document the lossy conversion from per-stream redirect flags to the configuration's single `OutputRedirection` flag honestly, with zero public surface change.

## What to build

- Add an XML remark on `FromProcessStartInfo` in `src/CliInvoke/Extensions/Configuration/ConfigurationExtensions.cs` (remark reported at `:69`) stating that per-stream redirect flags collapse via OR into the configuration's single `OutputRedirection` flag (DECISIONS-CliInvoke-bug-audit-fixes.md#T023).
- Binding constraint: no public surface change. No per-stream redirect properties are added in this session; any future per-stream surface is a separate v4 decision.

## Size

- **Files** - 1

## Recommended Workflow

### Step 1 - Confirm the conversion site

Where: src/CliInvoke/Extensions/Configuration/ConfigurationExtensions.cs

- Read the method; confirm where the per-stream flags OR into `OutputRedirection`.

Verify: the collapse behavior is confirmed in source before documenting it.

### Step 2 - Add the XML remark

Where: src/CliInvoke/Extensions/Configuration/ConfigurationExtensions.cs

- Add the XML remark describing the OR collapse.

Verify: XML documentation builds without warnings; no signature changed.

## Context pointers

##### Files

- `src/CliInvoke/Extensions/Configuration/ConfigurationExtensions.cs` - the only file this ticket edits

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T023` - document the collapse; explicit constraint that there is no surface change; future per-stream surface is a separate v4 decision
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001` - existing files only
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - docs-only fix, no runtime tests

## Acceptance criteria

- [ ] The XML remark states that per-stream redirect flags collapse via OR into the single `OutputRedirection` flag (T023)
- [ ] No public surface change — signatures, types, and behavior untouched (T023)

## Dependencies

**Blocked by** - None - can start immediately
