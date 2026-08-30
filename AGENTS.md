# CliInvoke Agent Guidelines

## What this repo is
CliInvoke — a .NET/C# library for running and interacting with command-line processes (builders, configuration models, invokers, DI helpers and optional specializations).

- Languages & targets: C#/.NET. Primary target: .NET 10. 
- Structure: Small-to-medium .NET library with multiple projects under src/.

## Codebase Organization
- **Solution**: `src/CliInvoke.sln`
- **Projects**:
  - `src/CliInvoke.Core/` - Abstractions and models
  - `src/CliInvoke/` - Main implementation
  - `src/CliInvoke.Extensions/` - DI and helpers
  - `src/CliInvoke.Specializations/` - Platform-specific features
- **Tests**: Located in `tests/` folder, uses TUnit framework.
- **Infrastructure**:
  - Root: `README.md`, `CONTRIBUTING.md`
  - Build Guidance: `site/docs/building-cliinvoke.md`
  - CI Workflows: `.github/workflows/*` (test.yml, publish.yml, scorecard.yml)
  - Other: `benchmarks/`, `.assets/`, `THIRD_PARTY_NOTICES.txt`
- **SDK**: Respect `global.json` (currently .NET 10.0) - matches CI.

## Important Conventions
- **Working directory**: CI tests are executed from `tests/CliInvoke.Tests/` (see `.github/workflows/test.yml`). Match this when reproducing issues.
- **Target framework**: net10.0 (see csproj files)
- **Testing**: Uses TUnit framework - `dotnet test` discovers and runs tests.
- **Resource disposal**: Refer to the Resource Cleanup section in `README.md` for guidance on disposing key types: `IExternalProcess`, `UserCredential`, `UserCredentialSpec`.
- **Versioning**: Update csproj version and changelog for releases.

## Common Gotchas
- SDK mismatch: Check `dotnet --version` vs `global.json` if build fails with TFM errors.
- Packaging: Use the `cliinvoke-publish-and-package` skill when testing changes across projects or preparing releases.
- External processes: Tests calling executables may behave differently across OSes. Prefer running in Ubuntu-latest/containers to replicate CI failures accurately.
- SourceLink / CI: release builds in CI expect SourceLink and symbol generation. Use `/p:ContinuousIntegrationBuild=true` to replicate CI/release behavior.

## Developer Expectations for PRs
- Small, focused changes with tests passing.
- If adding features or changing behavior, include or update tests under tests/ and run dotnet test locally.
- Update docs/README where usage or public API changed.
- If modifying core that other projects consume, validate by packing core and restoring the other projects from that local feed.

When making changes, prefer to open/modify files under src/ and run tests targeting src/CliInvoke/ for fast feedback.

## Contributing and PR workflow
- **Follow `CONTRIBUTING.md`** for all contributions. Read it before making non-trivial changes; it defines branching, commit, and review expectations.
- **Use `.github/pull_request_template.md`** whenever you open or draft a pull request on behalf of a user. Fill in every section (What changed, Why it was changed, Testing, Documentation, Contribution Policy compliance, Authorship) so the PR is ready for review without further editing.

## Specialized Workflows
The following skills are available to handle specific operational tasks. Load them when the corresponding scenario arises:

| Scenario | Skill to Load |
|----------|--------------|
| Daily development: restoring, building, and running tests | `cliinvoke-inner-loop` |
| Local NuGet packing for cross-project testing | `cliinvoke-publish-and-package` |
| Production release: versioning and publishing to NuGet | `cliinvoke-publish-and-package` |

## Middleware Patterns

### Middleware asymmetry across invocation patterns

Middleware applies to `ProcessInvoker` and `IProcessInvoker` only. The three invocation patterns have different middleware coverage:

- **`ProcessInvoker` / `IProcessInvoker`** — full middleware support. Every invocation flows through the registered middleware chain.
- **`IExternalProcess`** — does **not** flow through middleware. This is the bypass pattern for direct process execution.
- **`CliInvoke.Specializations`** — can be used via middleware extensions (e.g., `UsePowerShell`, `UseCmd`) but is not required to do so. Specializations that bypass the invoker do not go through middleware.

### DI and `MiddlewareItems` coexistence

DI and `MiddlewareItems` serve complementary purposes and coexist in the middleware pipeline:

- **DI** handles framework services with proper lifetime and scope management — loggers, validators, options, and other registered services.
- **`MiddlewareItems`** handles ad-hoc per-invocation state shared between middleware instances — a custom validation result, a per-invocation correlation id, or any other ad-hoc data that does not belong in the DI container.

This mirrors the ASP.NET Core precedent where `HttpContext.RequestServices` provides DI-resolved services and `HttpContext.Items` provides an ad-hoc dictionary for per-request state.

## Agent skills

### Issue tracker

Issues live in GitHub Issues (uses the gh CLI). See `docs/agents/issue-tracker.md`.

### Triage labels

Uses default label names: needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one GLOSSARY.md + docs/adr/ at repo root. See `docs/agents/domain.md`.
