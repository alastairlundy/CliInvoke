# CliInvoke AI Agent Skills

> **API version note:** These skills describe the **CliInvoke 3.0 configuration API** — the `*Spec` configuration seams (e.g. `ArgumentsSpec`, `EnvironmentVariablesSpec`, `ProcessResourcePolicySpec`, `UserCredentialSpec`) and the `ConfigureXxx(Action<XxxSpec>)` builder methods. The 3.0 API is available in pre-release packages (`3.0.0-beta.2`). On the older 2.x release the equivalent surface is the builder types (`IArgumentsBuilder`, `UserCredentialBuilder`, etc.). Agents should match their guidance to the installed package version.

This directory contains skills that guide AI agents in correctly using the CliInvoke library.

## Organization

Skills are organized into directories by the general area they cover:

- `cliinvoke/` — invocation patterns, configuration, and lifecycle.
- `cliinvoke-migration/` — v2 to v3 migration guidance.
- `evals/` — YAML files for testing skill effectiveness. Used for skill development and testing only.

Each skill is contained in its own directory and consists of a `SKILL.md` file documenting the specialized knowledge and workflows.

## Installation

These skills are distributed as a standard agent-skills directory and can be installed into any supported agent using the [skills](https://github.com/vercel-labs/skills) CLI.

### Using `npx skills` (no install required)

The quickest way to try a skill without installing anything:

```bash
npx skills use alastairlundy/CliInvoke@main --skill select-execution-pattern --agent opencode
```

This fetches the skill, writes the files to a temp directory, and starts your agent with the skill loaded.

### Installing skills into your project

Install all CliInvoke skills into your project's agent configuration:

```bash
npx skills add alastairlundy/CliInvoke@main
```

Or install a specific skill by name:

```bash
npx skills add alastairlundy/CliInvoke@main --skill select-execution-pattern
```

Install globally (user-level, not project-specific):

```bash
npx skills add alastairlundy/CliInvoke@main -g
```

Target a specific agent:

```bash
npx skills add alastairlundy/CliInvoke@main -a opencode
```

### Available skills

| Skill | Directory | Description |
|-------|-----------|-------------|
| `select-execution-pattern` | `cliinvoke/` | Choosing between CliRun, IProcessInvoker, and IExternalProcess |
| `package-installation-choice` | `cliinvoke/` | Selecting the right NuGet packages for your project type |
| `generate-process-configuration` | `cliinvoke/` | Building ProcessConfiguration via IProcessConfigurationBuilder |
| `implement-resource-lifecycle` | `cliinvoke/` | Managing disposal of disposable types to prevent leaks |
| `v2-to-v3-migration` | `cliinvoke-migration/` | Migrating v2-style code to v3 patterns |

### Supported agents

The `skills` CLI supports OpenCode, Claude Code, Codex, Cursor, and [many more](https://github.com/vercel-labs/skills#supported-agents). Skills work across agents without modification since each `SKILL.md` follows the standard format.
