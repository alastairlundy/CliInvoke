---
title: Document the encoding default as Encoding.Default (UTF-8 on net10.0)
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-handoff-process-flaws-doc.md
---

# TK003 - Document the encoding default as Encoding.Default (UTF-8 on net10.0)

## Goal

Keep the default consistent with what the .NET team itself would set and make the value discoverable. The three encoding properties on `ProcessConfiguration` keep `Encoding.Default` as their default; the documentation states that `Encoding.Default` is UTF-8 on .NET 10, so callers are not surprised by the value.

## What to build

Documentation-only change (zero code change to defaults):

- Add or extend the XML doc comments on the three `Encoding` properties of `ProcessConfiguration` in `src/CliInvoke.Core/` (`StandardInputEncoding`, `StandardOutputEncoding`, `StandardErrorEncoding`) to state that the default is `Encoding.Default` and that this is UTF-8 on net10.0, and that callers can override per stream via the init properties, which the control adapter applies explicitly (`StandardInputEncoding` only applies when standard input is redirected, and likewise for output and error).
- Update the encoding-related claims in `site/docs/` - the everything-wrong article's encoding section (around lines 252-257) and any encoding coverage in `site/docs/guides/configuration.md` - so they describe CliInvoke's default rather than only raw `System.Diagnostics.Process` behavior.

An earlier proposal to make the encoding nullable ("OS decides") was rejected; do not change any type signatures.

Note that XML doc comments are build errors in this repo (`Directory.Build.props` escalates CS1574/1580/1581/1584/1658/1734/1762), so the XML docs must be valid.

## Size

- Files - 3 (1 edit - `ProcessConfiguration.cs` XML docs, 2 edits - `site/docs/everything-wrong-with-process-in-csharp.md` and `site/docs/guides/configuration.md`)

## Recommended Workflow

### Step 1 - Locate the three encoding properties and current claims

Where: `src/CliInvoke.Core/`, `site/docs/`

- Find the three `Encoding` properties on `ProcessConfiguration` and their current XML docs.
- Read the encoding sections in the everything-wrong article and the configuration guide.

Verify: You know exactly which doc surfaces exist today and what they currently claim.

### Step 2 - Update the XML docs

Where: `src/CliInvoke.Core/` (the file holding `ProcessConfiguration`)

- State the default (`Encoding.Default`), its net10.0 value (UTF-8), and the per-stream override path.
- Note the redirect-gated application of each property.

Verify: `dotnet build src/CliInvoke.sln` passes with no XML doc warnings (they are errors here).

### Step 3 - Update the site docs

Where: `site/docs/everything-wrong-with-process-in-csharp.md`, `site/docs/guides/configuration.md`

- Rework the encoding claims so CliInvoke's default and the net10.0 value of `Encoding.Default` are explicit.

Verify: The claims match the code (see Step 4).

### Step 4 - Fact-check against the code

Where: `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs`

- Confirm the adapter applies each encoding property exactly as documented, and that the default flows from `Encoding.Default` untouched.

Verify: Every documented statement traces to a code statement.

## Context pointers

### Files

- `src/CliInvoke.Core/` - `ProcessConfiguration` and its three `Encoding` properties
- `site/docs/everything-wrong-with-process-in-csharp.md` - encoding section around lines 252-257
- `site/docs/guides/configuration.md` - configuration property documentation
- `src/CliInvoke/Processes/Internal/ControlAdapters/BaseProcessControlAdapter.cs` - `ApplyConfiguration` applies the encodings when redirected

### ADRs

- None constraining

### Domain terms

- None - no glossary terms materially bound this ticket

### Ledger records

- `DECISIONS-CliInvoke-process-flaw-mitigations.md#D001` - session scope covering the five mitigation items
- `DECISIONS-CliInvoke-process-flaw-mitigations.md#T002` - keep `Encoding.Default`, document the net10.0 value, no nullable "OS decides" encoding

## Acceptance criteria

- [ ] The three `Encoding` properties' XML docs state the `Encoding.Default` default and its UTF-8 value on net10.0
- [ ] The XML docs note the per-stream init-property override and the redirect gating
- [ ] The site docs' encoding claims describe CliInvoke's default accurately
- [ ] No code changes to defaults; no type signature changes
- [ ] `dotnet build src/CliInvoke.sln` passes (XML doc warnings are errors)

## Dependencies

**Blocked by** - None - can start immediately
