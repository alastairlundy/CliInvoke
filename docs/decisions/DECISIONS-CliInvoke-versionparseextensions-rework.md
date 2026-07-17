# Decision Ledger — VersionParseExtensions StringSegment Rework

Topic: rework `src/CliInvoke/Internal/Versions/VersionParseExtensions.cs` to drop `StringSegment` and `StringTokenizer` and use `char`- or `ReadOnlySpan<char>`-based parsing. The file currently does not compile on `net10.0` because `Microsoft.Extensions.Primitives` is no longer a transitive reference; the migration out of `DotExtensions` (per a prior review) removed the dep that previously provided `StringSegment` and `StringTokenizer`.

Scope: decisions in this ledger affect `src/CliInvoke/Internal/Versions/VersionParseExtensions.cs` and any new tests added under `tests/CliInvoke.Tests/` (TUnit framework, `net10.0` target — see `tests/CliInvoke.Tests/CliInvoke.Tests.csproj`). The test project currently references `DotExtensions` as a `PackageReference` and ships `Microsoft.Extensions.Primitives.dll` in its `bin/` output, so the Primitives assembly is reachable from the test project but not from the production project.

Cross-references:
- Build guidance: `docs/docs/building-cliinvoke.md`; CI workflow: `.github/workflows/test.yml`.
- Call sites for `GracefulParse`: `src/CliInvoke/ShellDetector.cs:94, 121, 140`.
- Domain glossary: `CONTEXT.md`.
- Test framework note: `AGENTS.md` documents TUnit; the `CliInvoke.Tests` test project uses TUnit. Other test projects in the repo (e.g. `CliInvoke.Specializations.Tests`) use xUnit v3; the two frameworks coexist.

---

### [D001] — scope of the VersionParseExtensions rework

- **Resolved Answer**: "Option 2 is sufficient" (Option 2 = modernize all parsing helpers in the file: `FindSeparator`, `SanitizeInput`, `ParseChars`, `GracefulParse`, and `ParseComponents` all become span-based; preserve the existing `.`-separated → `ParseComponents` vs other-separator → `ParseChars` branch behaviour and the `Version(char, int)` fallback at line 192).
- **Normalized Requirement**: The rework shall replace `StringSegment` and `StringTokenizer` with `ReadOnlySpan<char>`-based parsing in every parsing helper in `src/CliInvoke/Internal/Versions/VersionParseExtensions.cs`; the existing branch behaviour at lines 176–186 (`.`-separated input → `ParseComponents`; otherwise → `ParseChars`) and the `Version(char, int)` fallback at line 192 shall be preserved.
- **Constraints**: The build is currently broken on `net10.0` (`CS0246: The type or namespace name 'StringSegment' could not be found` at `VersionParseExtensions.cs:92`); the rework fixes the build. `GracefulParse` is called from `src/CliInvoke/ShellDetector.cs:94, 121, 140`; the rework does not change call-site behaviour. The migration out of `DotExtensions` is intentional and ongoing; this rework is one piece of it. The `Microsoft.Extensions.Primitives` package is not currently a direct reference on `src/CliInvoke/CliInvoke.csproj`; the rework does not add it back. The preserved branch quirk and the `Version(char, int)` fallback are not yet covered by tests; a future branch decides the test strategy. **Superseded by D002** — the "preserve existing CliInvoke behaviour" requirement was relaxed in favour of porting the DotExtensions test suite as the spec.

### [D002] — behavior target for the rework (D001 re-opened)

- **Resolved Answer**: "Option 2" (in context: the user is the author of both `CliInvoke` and `DotExtensions`; porting the `DotExtensions.Tests/VersionParseTests.cs` file as the spec aligns the rework with the more capable implementation the user has already shipped in `DotExtensions`).
- **Normalized Requirement**: The rework shall be a behavior-improving migration: the ported `DotExtensions.Tests/VersionParseTests.cs` file shall be the spec for the new `CliInvoke` implementation; the 4 currently-different cases (`"1.2.3-beta.1"`, `"10.20.300-beta.3"`, `"1 . 2 . 3"`, `"v 1 . 2 . 3"`) shall produce the `DotExtensions` expected result, not the current `CliInvoke` result. The "preserve existing `CliInvoke` behavior" requirement from D001 is replaced with "match the ported `DotExtensions` tests."
- **Constraints**: `Supersedes: D001`. The rework ships (a) the build fix on `net10.0` (`CS0246: StringSegment`), (b) the ported `DotExtensions` tests as the new spec at `tests/CliInvoke.Tests/Internal/Versions/VersionParseExtensionsTests.cs` (location/naming decided in T002), and (c) the reworked `GracefulParse` that passes them. The 3 call sites in `src/CliInvoke/ShellDetector.cs:94, 121, 140` will return richer `Version`s than before (e.g. `Version(1, 2, 3, 1)` for `1.2.3-beta.1` instead of `Version(1, 2, 3)`). A `ShellDetector` characterization test shall be added to pin the new behavior at the three call sites, so the behavior change is explicit and observable in CI (forward risk of D002). The ported test file is dual-licensed: MIT (per the `DotExtensions` origin) and MPL-2.0 (per the `CliInvoke` license), with the original MIT copyright noted in the file header. The `Version(char, int)` fallback at `VersionParseExtensions.cs:192` is preserved as-is per the original D001, but the fallback is not exercised by the ported tests; a future branch may decide whether to keep, expose, or remove it.

### [T001] — test strategy for the rework (revised by user)

- **Resolved Answer**: "Why don't we port existing `DotExtensions` `GracefulParse` tests to `CliInvoke` for this port and then identify any missing tests that need adding rather than creating tests from scratch?" (the user countered the Option 1/2/3 framing of the prior T1 turn with a fourth approach: port the `DotExtensions.Tests/VersionParseTests.cs` file as a baseline, then identify gaps).
- **Normalized Requirement**: The test strategy shall be: (a) port the `DotExtensions.Tests/VersionParseTests.cs` file (MIT, TUnit syntax) to a `CliInvoke.Tests` location with the namespace swap and license handling per T002; (b) run the ported tests against the reworked `GracefulParse` and identify any cases that still fail; (c) add tests for any `CliInvoke`-specific call-site scenarios in `src/CliInvoke/ShellDetector.cs:94, 121, 140` that the ported suite does not cover. Tests are written test-first so the implementation has a concrete pass/fail criterion.
- **Constraints**: **Cites**: D002 (`DotExtensions` tests are the spec). The ported test file shall use TUnit syntax (`[Test]`, `[Arguments]`, `await Assert.That(...).IsEqualTo(...)`) consistent with the rest of `CliInvoke.Tests`. The "identify missing" step is the gap analysis between the ported tests and the `ShellDetector` call-site scenarios; the gap is a follow-up to T002, not a new branch. The original `DotExtensions` MIT license header shall be preserved in the ported file (or noted) per the user's relicensing authority as the author of both projects.

### [T002] — test file porting mechanics

- **Resolved Answer**: "Option 1" (Option 1 = port with file/location adjustments; keep the original MIT license header).
- **Normalized Requirement**: The ported test file shall be placed at `tests/CliInvoke.Tests/Internal/Versions/VersionParseExtensionsTests.cs` (mirrors the source path `src/CliInvoke/Internal/Versions/VersionParseExtensions.cs`; matches the project convention of `<TypeUnderTest>Tests.cs`); the namespace shall be `CliInvoke.Tests.Internal.Versions`; the original `DotExtensions` MIT license header shall be preserved unchanged; a single-line comment below the header shall note the port: `// Ported from DotExtensions.Tests/VersionParseTests.cs (same author, 2026).`
- **Constraints**: **Cites**: T001, D002. The MIT license header is load-bearing and shall not be replaced with the project-standard MPL-2.0 header on a future license-cleanup pass; the decision ledger records this as a code-review note. The `// Ported from DotExtensions...` comment is a visible-but-not-load-bearing provenance marker. The new `tests/CliInvoke.Tests/Internal/Versions/` subdirectory is created as part of the port.

### [T003] — `ParseComponents` API shape

- **Resolved Answer**: "Option 1" (Option 1 = encapsulated split inside `ParseComponents`).
- **Normalized Requirement**: The reworked `ParseComponents` shall have the signature `private static (int major, int minor, int build, int revision) ParseComponents(ReadOnlySpan<char> input, char separator)`. The function shall split the input internally via a `while` loop using `IndexOf(separator)`; the segments shall be accumulated into an internal `Span<Range> components = stackalloc Range[4];` (4 is the component cap from the current `Take(4)`); empty segments shall be filtered; each non-empty segment shall be parsed by finding the first digit (`IndexOfAny(['0'..'9'])`) and reading digits until a non-digit. Zero heap allocation by construction.
- **Constraints**: **Cites**: D002 (ported tests are the spec), T004 (the `.`-only branch routes input to `ParseComponents`). The internal `stackalloc Range[4]` is a fixed-size buffer; the 4-component cap is load-bearing per the ported test `"1.2.3.4.5"` → `Version(1, 2, 3, 4)`. If the cap ever grows above 4, the function silently truncates; the function's XML doc and this ledger flag the cap as a code-review concern.

### [T004] — the `.`-only branch in `GracefulParse`

- **Resolved Answer**: "Option 1" (Option 1 = keep the `.`-only branch; current behavior preserved for non-`.` separators).
- **Normalized Requirement**: The reworked `GracefulParse` shall keep the same `if (input.Contains('.') && separator != ' ') { ... ParseComponents ... } else { ... ParseChars ... }` branch structure; only `.`-separated input shall go through `ParseComponents`; other separators (e.g. `,`, `;`, `-`) shall fall through to `ParseChars` and return `Version(<first-digits>, 0)`. The branch is a `.`-only quirk preserved as-is; the ported `DotExtensions` tests do not constrain the branch (no non-`.` separator test case), and `ShellDetector.cs:94, 121, 140` is all `.`-separated, so the branch does not affect the call sites.
- **Constraints**: **Cites**: D002 (ported `DotExtensions` tests are the spec; the branch is unconstrained by them). The `.`-only branch is a quirk; a future contributor who generalizes it (e.g. to any non-space separator) opens a behavior change that the ported tests do not justify. The branch is load-bearing per T004; the function's XML doc and this ledger flag it as a code-review concern. The `Version(char, int)` fallback at `VersionParseExtensions.cs:192` is preserved as-is per the original D001 (now superseded by D002 but the fallback was not relaxed); the fallback is not exercised by the ported tests; a future branch may decide whether to keep, expose, or remove it.

### [T005] — `FindSeparator` rework

- **Resolved Answer**: "Option 1" (Option 1 = minimal fix: `IndexOf('.')` + fix the `First()` re-walk + change parameter to `ReadOnlySpan<char>`).
- **Normalized Requirement**: The reworked `FindSeparator` shall have the signature `private static char FindSeparator(ReadOnlySpan<char> input)`. The function shall replace `input.Contains('.', StringComparison.OrdinalIgnoreCase)` with `input.IndexOf('.') != -1`; the `foreach` loop at lines 80–87 shall use `output = currentChar;` (the iterated char) instead of `output = versionString.First(c => char.IsSeparator(c) || char.IsPunctuation(c))`; the parameter shall be `ReadOnlySpan<char>` to align with the rest of the rework.
- **Constraints**: **Cites**: T003, T004. The `First()` re-walk is load-bearing per T005; a future contributor who re-introduces it (thinking it is more "expressive") doubles the work for no gain. The function's XML doc and this ledger flag the `currentChar` capture as a code-review concern. The function is called once per `GracefulParse` from 3 call sites in `ShellDetector.cs:94, 121, 140`; the `SearchValues<char>` optimization is rejected as over-engineering.

### [T006] — `ParseChars` simplification

- **Resolved Answer**: "Option 1" (Option 1 = `while` loop to find the first non-digit, then `int.Parse(chars[..end])`).
- **Normalized Requirement**: The reworked `ParseChars` shall find the end of the leading-digit run via a `while` loop (`while (end < chars.Length && char.IsDigit(chars[end])) end++;`); if `end == 0` (no digits), return `(-1, -1, -1, -1)`; otherwise return `(int.Parse(chars[..end], NumberStyles.Integer, CultureInfo.InvariantCulture), -1, -1, -1)`. The `StringBuilder` allocation is removed; the function operates directly on the input span.
- **Constraints**: **Cites**: T003, T004. The `-1` sentinel is preserved; the ported test `GracefulParse_NoDigits_ThrowsArgumentException` exercises the all-`-1` path via the `Version(char, int)` fallback at `VersionParseExtensions.cs:192`. The span-based `int.Parse` is load-bearing per T006; a future contributor who re-adds the `StringBuilder` thinking it is "safer" for culture handling re-introduces the allocation the rework removes.

### [T007] — `SanitizeInput` disposition

- **Resolved Answer**: "Option 1 but we need to add an xml doc comment or a code comment concisely explaining why SanitizeInput isn't needed anymore."
- **Normalized Requirement**: The `SanitizeInput` method shall be deleted from `VersionParseExtensions.cs`. The reworked `GracefulParse` shall pass the raw input directly to `ParseComponents` and `ParseChars` (via `versionString.AsSpan()`); the `SanitizeInput` call site is removed. A concise XML doc comment shall be added to `ParseComponents` and `ParseChars` (or a `//` comment at the `GracefulParse` call site) explaining that these functions handle non-digit chars within a segment by finding the first digit and reading digits only, so preprocessing is not required.
- **Constraints**: **Cites**: D002 (ported tests are the spec; the 4 deltas are caused by `SanitizeInput`), T003, T004. The comment is load-bearing per the user's explicit request; a future contributor who re-adds `SanitizeInput` or removes the comment loses the rationale. The `StringBuilder` allocation in `SanitizeInput` is also removed (consistent with T006's `StringBuilder` removal in `ParseChars`).

### [T008] — `Version(char, int)` fallback preservation

- **Resolved Answer**: "Preserve the fallback as per T004 constraint."
- **Normalized Requirement**: The `Version(char, int)` fallback at `VersionParseExtensions.cs:192` shall be preserved as-is. The fallback is not exercised by the ported `DotExtensions` tests (the `GracefulParse_NoDigits_ThrowsArgumentException` test hits the `catch` branch, not the `(firstDigitChar, 0, 0, 0)` branch). A future branch may decide whether to keep, expose, or remove it.
- **Constraints**: **Cites**: T004 (the fallback was preserved per the original D001; D002 superseded D001 but did not relax the fallback). The fallback is dead code for the new spec but preserved per the T004 decision.

### [T009] — `ShellDetector` characterization test (deferred)

- **Resolved Answer**: "T009 is deferred."
- **Normalized Requirement**: The `ShellDetector` characterization test is deferred to a follow-up after the rework ships. Per D002's forward risk, the test should be added before or shortly after the rework ships to pin the new behavior at `ShellDetector.cs:94, 121, 140`.
- **Constraints**: **Cites**: D002 (forward risk). The test is a follow-up, not part of the core rework.
