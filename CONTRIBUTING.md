## Contributing to the Project

### Suggestions and Bug Reports
If you have either a suggestion or bug report you can file it on this project's [GitHub Issues page](https://github.com/alastairlundy/CliInvoke/issues/).

### Submitting a Pull Request
If you'd like to add a feature or change part of this project's code, please:
1) Fork this repository
2) Make a new branch from main with a name that very briefly describes the changes you want to make
3) Test the changes to make sure it doesn't break any existing code and works on the Platforms that the project currently works on
4) Submit a Pull Request describing the changes you've made and why you've made them (particularly if it's not immediately apparent or obvious)
5) If you haven't had a response within a reasonable time, feel free to tag the maintainer (@alastairlundy) in the Pull Request so they can take a look.

If you have multiple features or changes that you want to add that don't rely on each other, please create separate branches for each separate change or feature. 

You might find it duplicative to do this, but it helps to ensure that: 
A) individual changes are accepted or declined based on their own merits
B) submitted code is reviewed and tested carefully, which is normal, healthy open-source practice that keeps quality high and catches regressions before they reach the released package
and C) code is safely added to this project without causing this package to suffer any regressions as a result of accepting the contribution. 

If you follow these steps, and your contribution makes a helpful change, the maintainer is likely to merge it.

### How to build & test

CliInvoke targets .NET 10 (see `global.json`) and uses the [TUnit](https://www.tunit.dev/) test framework.

1. Install the .NET 10 SDK (matching `global.json`).
2. Restore and build the solution from the repo root:
   ```bash
   dotnet build src/CliInvoke.sln -c Debug
   ```
3. Run the tests from the main test project directory:
   ```bash
   cd tests/CliInvoke.Tests
   dotnet test
   ```
4. For full build, release, and packaging guidance, see [building-cliinvoke.md](site/docs/building-cliinvoke.md).

### Development setup, standards & PR conventions

- **SDK & target frameworks:** Use the .NET 10 SDK and respect the `net10.0` target frameworks declared in the project files.
- **Code style:** Match the existing C# conventions in the repository. Keep changes small and focused, and make sure `dotnet test` passes before submitting.
- **Pull requests:** When you open a PR, fill in every section of [`.github/pull_request_template.md`](.github/pull_request_template.md) — including the Testing and Contribution Policy sections, and the authorship declaration (human-authored or AI-co-authored).
- **Issues:** Use the provided templates under [`.github/ISSUE_TEMPLATE/`](.github/ISSUE_TEMPLATE/) (bug report or feature request) when filing an issue.
- **Contribution policy:** There is currently no CLA or DCO required for this project. By contributing, you confirm you have read and followed this `CONTRIBUTING.md` and that your contribution is your own original work (or properly licensed).

### InternalsVisibleTo (IVT) grant minimization

CliInvoke packages minimize cross-package coupling by keeping their internal surfaces private. A new `InternalsVisibleTo` grant is **not** a default and **requires justification**:

- **Justify new grants.** A new IVT grant must be defended: explain why the consuming assembly strictly requires access to internals and why the needed types cannot instead be promoted to a public, stable API or relocated to a shared package (see `docs/adr/0001-ivt-minimization.md`).
- **Remove unused grants.** Grants that no longer have a consumer are removed. Do not leave dead coupling points in place.
- **Test grants follow the same rule.** IVT grants to test assemblies (`CliInvoke.Tests`, `CliInvoke.Specializations.Tests`) are kept only while actually used; an unused test-assembly grant is removed like any other unused grant (unused-grant removal). Being same-repo and not a shipping package does not exempt a test grant from reduction.
