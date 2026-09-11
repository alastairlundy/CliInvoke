# CliInvoke AI Agent Skills

> **API version note:** These skills describe the **CliInvoke 3.0 configuration API** — the `*Spec` configuration seams (e.g. `ArgumentsSpec`, `EnvironmentVariablesSpec`, `ProcessResourcePolicySpec`, `UserCredentialSpec`) and the `ConfigureXxx(Action<XxxSpec>)` builder methods. The 3.0 API is available in pre-release packages (`3.0.0-beta.2`). On the older 2.x release the equivalent surface is the builder types (`IArgumentsBuilder`, `UserCredentialBuilder`, etc.). Agents should match their guidance to the installed package version.

This directory contains SKILLs designed to guide AI agents in correctly using the CliInvoke library.

## Organization

Skills are organized by the package they primarily relate to:

- `CliInvoke.Core`: Skills related to core abstractions, models, and configuration.
- `CliInvoke`: Skills related to the main implementation and high-level patterns.

- `Evals`: YAML files for testing the effectiveness of the SKILLs. - These are for SKILLs development and testing purposes.

Each skill is contained in its own directory and consists of a `SKILL.md` file documenting the specialized knowledge and workflows.
