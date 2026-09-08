---
title: ProcessWrapper truncation cap semantics and decoder-based decode
classification: Independent
blocked_by: ["004-processwrapper-lifecycle"]
parent: IMPLEMENTATION-bug-audit-fixes.md
---

## Goal

Give the truncation cap boundary distinct, explicit meanings and make multibyte content truncate cleanly instead of producing replacement characters at the cut point.

## What to build

- `ReadStreamCappedAsync`: `maxBytes` that is `null` or negative means no cap; `0` is a valid zero-byte cap producing empty text with the truncated flag set. XML documentation spells out all three spellings (null = no cap, negative = no cap, 0 = zero-byte cap). No exception-based validation — the boundary stays permissive (DECISIONS-CliInvoke-bug-audit-fixes.md#T021).
- Decode the capped bytes incrementally with `encoding.GetDecoder()` so a split trailing multibyte sequence is held back and dropped cleanly instead of becoming U+FFFD (the Unicode replacement character). The decode step is reported at `src/CliInvoke/Processes/Internal/ProcessWrapper.cs:423` (DECISIONS-CliInvoke-bug-audit-fixes.md#T022).
- The drain loop and the cap semantics are otherwise unchanged.

## Size

- **Files** - 3 (1 source edit, 2 test edits)

## Recommended Workflow

### Step 1 - Confirm the cap check and decode step

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Read `ReadStreamCappedAsync`; confirm the current `<= 0` no-cap branch and the single-pass decode near line 423. Line numbers may have shifted after ticket 004 landed — re-locate by symbol, not by line.

Verify: both sites located in the post-004 source.

### Step 2 - Implement the three-way cap spelling

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Change the no-cap condition from `<= 0` to `< 0` (null and negative join as no cap).
- Make `0` a valid zero-byte cap: empty text output with the truncated flag set.

Verify: a zero-cap call returns empty text with the truncated flag set.

### Step 3 - Switch to incremental decoding

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Replace the single-pass decode with `encoding.GetDecoder()`-based incremental decoding so a split trailing multibyte sequence is held back and dropped cleanly.

Verify: a byte sequence cut mid-character decodes without U+FFFD in the output.

### Step 4 - Document the cap spellings

Where: src/CliInvoke/Processes/Internal/ProcessWrapper.cs

- Update the XML documentation for `ReadStreamCappedAsync` to state all three spellings: null = no cap, negative = no cap, 0 = zero-byte cap.

Verify: XML documentation builds without warnings.

### Step 5 - Add boundary and fuzz tests

Where: tests/CliInvoke.Tests/Processes/TruncationTests.cs, tests/CliInvoke.Tests/Fuzzing/TruncationCapFuzzTests.cs

- Add multibyte boundary cases (split trailing sequences in UTF-8 and other encodings) and zero-cap cases (DECISIONS-CliInvoke-bug-audit-fixes.md#T027).
- Extend the fuzz suite with cap-boundary inputs.

Verify: dotnet test passes for both suites.

## Context pointers

##### Files

- `src/CliInvoke/Processes/Internal/ProcessWrapper.cs` - the only source file this ticket edits
- `tests/CliInvoke.Tests/Processes/TruncationTests.cs` - boundary tests
- `tests/CliInvoke.Tests/Fuzzing/TruncationCapFuzzTests.cs` - fuzz tests

##### Domain terms

- Invocation Capability - the truncation cap is a caller-stated parameter of the invocation contract; the three-way spelling makes the caller's stated meaning explicit rather than a silent unbounded sentinel.

##### Ledger records

- `DECISIONS-CliInvoke-bug-audit-fixes.md#T021` - null and negative = no cap, 0 = valid zero-byte cap; XML docs state all three; no exception-based validation
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T022` - decoder-based incremental decode; drain loop and cap semantics unchanged
- `DECISIONS-CliInvoke-bug-audit-fixes.md#T001`, `DECISIONS-CliInvoke-bug-audit-fixes.md#T027` - existing files only; multibyte boundary tests required

## Acceptance criteria

- [ ] `null` and negative `maxBytes` mean no cap; `0` produces empty text with the truncated flag set (T021)
- [ ] XML documentation states all three spellings (T021)
- [ ] A split trailing multibyte sequence is dropped cleanly — no U+FFFD in truncated output (T022)
- [ ] Multibyte boundary tests and fuzz tests pass

## Dependencies

**Blocked by** - 004-processwrapper-lifecycle (same file: `src/CliInvoke/Processes/Internal/ProcessWrapper.cs`)
