## Contributing to the Project

### Suggestions and Bug Reports
If you have either a suggestion or bug report you can file it on this project's [GitHub Issues page](https://github.com/alastairlundy/CliInvoke/issues/).

### Submitting a Pull Request
If you'd like to add a feature or change part of this project's code, please:
1) Fork this repository
2) Make a new branch from main with a name that very briefly describes the changes you want to make
3) Test the changes to make sure they don't break any existing code and pass on all platforms the project supports and is tested on in CI: **Windows, macOS, and Linux**.
4) Submit a Pull Request describing the changes you've made and why you've made them (particularly if it's not immediately apparent or obvious)
5) If you haven't had a response within a reasonable time, feel free to tag the maintainer (@alastairlundy) in the Pull Request so they can take a look.

If you have multiple features or changes that you want to add that don't rely on each other, please create separate branches for each separate change or feature. 

You might find it duplicative to do this, but it helps to ensure that: 
A) individual changes are accepted or declined based on their own merits
B) submitted code is reviewed and tested carefully, which is normal, healthy open-source practice that keeps quality high and catches regressions before they reach the released package
and C) code is safely added to this project without causing this package to suffer any regressions as a result of accepting the contribution. 

If you follow these steps, and your contribution makes a helpful change, the maintainer is likely to merge it.

### How to build & test

CliInvoke targets .NET 10 and .NET 11 (see the `TargetFrameworks` in the csproj files; `global.json` pins the .NET 11 SDK needed to build `net11.0`) and uses the [TUnit](https://www.tunit.dev/) test framework. CI runs the test suite on Windows, macOS, and Linux; your changes must pass on all three. Some tests are OS-specific by design (for example, Windows-only cmd-runner tests) and skip automatically elsewhere — that is expected behavior, not a failure.

1. Install the .NET 11 SDK (matching `global.json`). The .NET 10 SDK/runtime is also needed to run the `net10.0` test leg.
2. Restore and build the solution from the repo root:
   ```bash
   dotnet build src/CliInvoke.sln -c Debug
   ```
3. Run the tests from the main test project directory:
   ```bash
   cd tests/CliInvoke.Tests
   dotnet test
   ```
   Run `dotnet test` from inside the project directory, not against a project path. On the .NET 11 RC SDKs, `dotnet test <path>` fails with a doubled-path error ([dotnet/sdk#56196](https://github.com/dotnet/sdk/issues/56196)); the fix ships with the .NET 11 GA release, and an absolute path works as a workaround until then.
4. For full build, release, and packaging guidance, see [building-cliinvoke.md](site/docs/building-cliinvoke.md).

### Development setup, standards & PR conventions

- **SDK & target frameworks:** Use the .NET 11 SDK (matching `global.json`) and respect the `net10.0`/`net11.0` target frameworks declared in the project files.
- **Code style:** [`src/.editorconfig`](src/.editorconfig) is the required coding standard — IDEs with EditorConfig support enforce its rules automatically. Beyond what it can check, match the surrounding code's naming and layout, and keep changes small and focused. You can auto-fix formatting with `dotnet format src/CliInvoke.sln`, but scope it to the files you change — the solution still contains pre-existing formatting debt, so a whole-solution `--verify-no-changes` pass is not currently treated as a merge gate.
- **ConfigureAwait:** Every `await` in library code (`src/`) must call `.ConfigureAwait(false)` — without it, continuations marshal back to the caller's `SynchronizationContext`, which can deadlock UI applications that invoke the library and avoids unnecessary context hops elsewhere. CI enforces this with `scripts/guard-configureawait.sh`; `await foreach` and `await using` are exempt because those constructs cannot accept ConfigureAwait. Executable applications (tests, examples, benchmarks) are exempt from the `false` requirement — their continuations typically have no meaningful SynchronizationContext, so use an explicit `.ConfigureAwait(true)` there to show the decision was made deliberately. The `MA0004` analyzer rule treats missing ConfigureAwait calls as build errors in `src/` (`src/.editorconfig`); `scripts/guard-configureawait.sh` remains the CI backstop.
- **Tests:** New behavior requires a new [TUnit](https://www.tunit.dev/) test; bug fixes require a regression test that fails before the fix passes. Tests must pass on Windows, macOS, and Linux (see [How to build & test](#how-to-build--test)).
- **Architecture decisions (ADRs):** ADRs by contributors should be carefully considered and discussed with maintainers prior to creation. If you believe an ADR is warranted, discuss it in an issue (if the work is in the conceptual/planning stage) or a Pull Request (if the code is ready for review).
- **Pull requests:** When you open a PR, fill in every section of [`.github/pull_request_template.md`](.github/pull_request_template.md) — including the Testing and Contribution Policy sections, and the authorship declaration (human-authored or AI-co-authored).
- **Issues:** Use the provided templates under [`.github/ISSUE_TEMPLATE/`](.github/ISSUE_TEMPLATE/) (bug report or feature request) when filing an issue.
- **Contribution policy:** By contributing, you confirm you have read and followed this `CONTRIBUTING.md` and that your contribution is your own original work or properly licensed for inclusion — see [Contribution licensing](#contribution-licensing).

### Contribution licensing

- **New code you write for the project** is licensed under the project's license, [MPL 2.0](LICENSE.txt) — by submitting a contribution you agree to license that new code under it.
- **Pre-existing code** under one of these licenses is suitable: MIT, Apache 2.0, BSD 2-Clause, BSD 3-Clause, MPL 2.0, CC Zero (CC0), and CC BY.
- Other permissively licensed code to be contributed may be reviewed on a case-by-case basis.
- **Not allowed:** copyleft-licensed code (LGPL, GPL, AGPL, or CC BY-SA), non-commercial licenses, and proprietary/source-available licenses.

### InternalsVisibleTo (IVT) grant minimization

CliInvoke packages minimize cross-package coupling by keeping their internal surfaces private. A new `InternalsVisibleTo` grant is **not** a default and **requires justification**:

- **Justify new grants.** A new IVT grant must be defended: explain why the consuming assembly strictly requires access to internals and why the needed types cannot instead be promoted to a public, stable API or relocated to a shared package (see `docs/adr/0001-ivt-minimization.md`).
- **Remove unused grants.** Grants that no longer have a consumer are removed. Do not leave dead coupling points in place.
- **Test grants follow the same rule.** IVT grants to test assemblies (`CliInvoke.Tests`, `CliInvoke.Specializations.Tests`) are kept only while actually used; an unused test-assembly grant is removed like any other unused grant (unused-grant removal). Being same-repo and not a shipping package does not exempt a test grant from reduction.
