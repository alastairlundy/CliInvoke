# Minimal Repro: Lunet Docs Site Build Timeout

Reproduces the issue from https://github.com/alastairlundy/CliInvoke/actions/runs/33768022903/job/100690773422

## The Problem

The `lunet build` command hangs indefinitely during NPM package installation (specifically while installing `prismjs`), causing the GitHub Actions workflow to exceed its 15-minute timeout.

## Key Observations from CI Logs

1. `dotnet build` completes successfully (~20 seconds)
2. `dotnet tool install --global lunet` succeeds (~3 seconds)
3. `lunet --stacktrace build` starts, downloads the `lunet-io/templates` theme
4. NPM installs begin: bootstrap, bootstrap-icons, tocbot, anchor-js, prismjs
5. The last log line shows `NPM installing prismjs` at 14:39:27
6. **Nothing is logged for ~15 minutes** until the job is cancelled
7. Cleanup kills orphan processes: `dotnet` (pid 2643) and `lunet` (pid 2763)

## Structure

```
repro/
  global.json               # .NET 10 SDK
  src/MyLib/                 # Minimal .NET class library with XML docs
    MyLib.csproj
    WidgetService.cs
  site/                      # Lunet site directory
    config.scriban           # Matches the original failing config
    index.md
  .github/workflows/
    docs-publish.yml         # Reproduces the CI workflow
```

## How to Reproduce

### Locally (if you have .NET 10 SDK)

```bash
cd repro
dotnet build src/MyLib -c Release
dotnet tool install --global lunet --version "1.*"
cd site
lunet --stacktrace build
```

### Via GitHub Actions

Push this `repro/` directory as the root of a repo and trigger the workflow.

## Likely Root Cause

The `extend "lunet-io/templates"` directive in `config.scriban` triggers NPM package
installs (bootstrap, bootstrap-icons, tocbot, anchor-js, prismjs). On GitHub Actions
runners, these NPM installs appear to hang indefinitely — no error, no output, just
silence. This may be related to:

- NPM registry connectivity issues in CI
- `prismjs` specifically having a problematic dependency tree
- Network sandbox restrictions on GitHub Actions runners
- The `lunet-io/templates` theme's NPM install process not having proper timeouts
