# Implementation Blueprint: VersionParseExtensions StringSegment Rework

## Scope Binding

- **Linked Spec**: `C:\Users\alast\AppData\Local\Temp\opencode\handoff-clinvoke-versionparseextensions.md`
- **Decision Ledger**: `docs/decisions/DECISIONS-CliInvoke-versionparseextensions-rework.md`

**NOTICE**: This blueprint is a context pointer valid ONLY for the linked spec. It must not be applied to other specifications without explicit authorization. Every technical statement in this blueprint that satisfies a functional requirement references a `Dxxx` or `Txxx` record from the Decision Ledger using the `filename#<Dxxx|Txxx>` format.

---

## Implementation Plan

### 1. Port the Test File

Port `DotExtensions.Tests/VersionParseTests.cs` to `tests/CliInvoke.Tests/Internal/Versions/VersionParseExtensionsTests.cs` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T002`].

- Change namespace from `DotExtensions.Tests` to `CliInvoke.Tests.Internal.Versions`
- Keep the original MIT license header unchanged [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T002`]
- Add provenance comment: `// Ported from DotExtensions.Tests/VersionParseTests.cs (same author, 2026).`
- The ported tests become the spec for the reworked implementation [`DECISIONS-CliInvoke-versionparseextensions-rework.md#D002`]

**Test Strategy**: Port the tests first, then identify gaps [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T001`]. The 4 currently-different cases (`"1.2.3-beta.1"`, `"10.20.300-beta.3"`, `"1 . 2 . 3"`, `"v 1 . 2 . 3"`) shall produce the DotExtensions expected result [`DECISIONS-CliInvoke-versionparseextensions-rework.md#D002`].

### 2. Rework `FindSeparator`

Change signature to `private static char FindSeparator(ReadOnlySpan<char> input)` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T005`].

- Replace `input.Contains('.', StringComparison.OrdinalIgnoreCase)` with `input.IndexOf('.') != -1` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T005`]
- Fix the `First()` re-walk bug: use `output = currentChar;` instead of `output = versionString.First(c => char.IsSeparator(c) || char.IsPunctuation(c))` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T005`]
- The function is called once per `GracefulParse` from 3 call sites in `ShellDetector.cs:94, 121, 140` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T005`]

### 3. Drop `SanitizeInput`

Delete the `SanitizeInput` method entirely [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T007`].

- `GracefulParse` shall pass the raw input directly to `ParseComponents` and `ParseChars` via `versionString.AsSpan()` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T007`]
- Add XML doc comment to `ParseComponents` and `ParseChars` explaining that these functions handle non-digit chars within a segment by finding the first digit and reading digits only, so preprocessing is not required [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T007`]
- The `StringBuilder` allocation in `SanitizeInput` is also removed [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T007`]

**Rationale**: `SanitizeInput` is the cause of all 4 deltas in the ported tests [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T007`]. Dropping it is the cleanest fix.

### 4. Rework `ParseComponents`

Change signature to `private static (int major, int minor, int build, int revision) ParseComponents(ReadOnlySpan<char> input, char separator)` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T003`].

- Split the input internally via a `while` loop using `IndexOf(separator)` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T003`]
- Accumulate segments into an internal `Span<Range> components = stackalloc Range[4];` (4 is the component cap) [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T003`]
- Filter empty segments [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T003`]
- Parse each non-empty segment by finding the first digit (`IndexOfAny(['0'..'9'])`) and reading digits until a non-digit [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T003`]
- Zero heap allocation by construction [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T003`]

**Component Cap**: The 4-component cap is load-bearing per the ported test `"1.2.3.4.5"` → `Version(1, 2, 3, 4)` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T003`]. If the cap ever grows above 4, the function silently truncates; the function's XML doc and the ledger flag the cap as a code-review concern.

### 5. Rework `ParseChars`

Simplify to use a `while` loop to find the leading-digit run, then `int.Parse(chars[..end])` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T006`].

- Find the end of the leading-digit run: `while (end < chars.Length && char.IsDigit(chars[end])) end++;` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T006`]
- If `end == 0` (no digits), return `(-1, -1, -1, -1)` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T006`]
- Otherwise return `(int.Parse(chars[..end], NumberStyles.Integer, CultureInfo.InvariantCulture), -1, -1, -1)` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T006`]
- Remove the `StringBuilder` allocation [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T006`]

**Sentinel Preservation**: The `-1` sentinel is preserved; the ported test `GracefulParse_NoDigits_ThrowsArgumentException` exercises the all-`-1` path via the `Version(char, int)` fallback [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T006`].

### 6. Keep the `.`-Only Branch

`GracefulParse` shall preserve the `if (input.Contains('.') && separator != ' ') { ... ParseComponents ... } else { ... ParseChars ... }` branch structure [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T004`].

- Only `.`-separated input shall go through `ParseComponents` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T004`]
- Other separators (e.g. `,`, `;`, `-`) shall fall through to `ParseChars` and return `Version(<first-digits>, 0)` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T004`]
- The branch is a `.`-only quirk preserved as-is; the ported DotExtensions tests do not constrain the branch [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T004`]
- `ShellDetector.cs:94, 121, 140` is all `.`-separated, so the branch does not affect the call sites [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T004`]

### 7. Preserve the `Version(char, int)` Fallback

The `Version(char, int)` fallback at `VersionParseExtensions.cs:192` shall be preserved as-is [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T008`].

- The fallback is not exercised by the ported DotExtensions tests [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T008`]
- The `GracefulParse_NoDigits_ThrowsArgumentException` test hits the `catch` branch, not the `(firstDigitChar, 0, 0, 0)` branch [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T008`]
- A future branch may decide whether to keep, expose, or remove it [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T008`]

### 8. Deferred: `ShellDetector` Characterization Test

The `ShellDetector` characterization test is deferred to a follow-up after the rework ships [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T009`].

- Per D002's forward risk, the test should be added before or shortly after the rework ships to pin the new behavior at `ShellDetector.cs:94, 121, 140` [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T009`]
- The test is a follow-up, not part of the core rework [`DECISIONS-CliInvoke-versionparseextensions-rework.md#T009`]

---

## Ledger Reference

This blueprint cites the following records from `DECISIONS-CliInvoke-versionparseextensions-rework.md`:

- **D001**: Scope of the VersionParseExtensions rework (superseded by D002)
- **D002**: Behavior target for the rework (D001 re-opened)
- **T001**: Test strategy for the rework (revised by user)
- **T002**: Test file porting mechanics
- **T003**: `ParseComponents` API shape
- **T004**: The `.`-only branch in `GracefulParse`
- **T005**: `FindSeparator` rework
- **T006**: `ParseChars` simplification
- **T007**: `SanitizeInput` disposition
- **T008**: `Version(char, int)` fallback preservation
- **T009**: `ShellDetector` characterization test (deferred)
