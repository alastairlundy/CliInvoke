---
name: package-installation-choice
description: Guides the selection and installation of the correct CliInvoke NuGet packages based on project type and requirements (Library vs App, Abstractions vs Implementation). USE FOR selecting the correct NuGet packages (Core, Implementation, Extensions, Specializations) based on project type (Library vs App). DO NOT USE FOR fixing NuGet restore errors.
---

# Package Installation Choice

## When to Use
- When starting a new CliInvoke-based project and deciding which NuGet packages to install.
- When you need to determine the correct package set for a Library author versus an application (Console/Desktop).
- When deciding whether to include `CliInvoke.Extensions` (DI helpers and middleware) or `CliInvoke.Specializations` (shell-specific features and platform middleware).
- When auditing an existing project's package references against the recommended installation matrix.

## When not to use
- When fixing NuGet restore errors, version conflicts, or feed/source issues — this skill does not address package resolution problems.
- When the question is about how to *use* a package after installation — load a skill specific to the API surface (e.g., `generate-process-configuration`, `select-execution-pattern`).
- When migrating between major versions of CliInvoke — load `cliinvoke-v1-to-v2-migration` instead.

## Installation Matrix

| Project type / Need | Packages to install | Notes |
| :--- | :--- | :--- |
| **Library author** (abstractions only) | `CliInvoke.Core` | Only the Core (abstractions) package — consumers can choose implementations. |
| **Library or app** (needs concrete builders/implementations) | `CliInvoke.Core`, `CliInvoke` | Implementation package plus Core for models/abstractions. |
| **Desktop or Console application** (common case — use DI & convenience helpers) | `CliInvoke.Core`, `CliInvoke`, `CliInvoke.Extensions` | Includes DI registration and convenience extensions for easy setup. |
| **Platform-specific/shell specializations** (optional) | `CliInvoke.Specializations` | Adds Cmd/PowerShell and other specializations; install in addition to the packages above as needed. |

## Key Installation Paths

### 1. Library Development (Abstractions Layer)
If you are creating a library that defines process interactions but doesn't want to force a concrete implementation on the consumer:
- **Package**: `CliInvoke.Core` (Recommended for libraries)
- **Reasoning**: This avoids introducing concrete dependencies into abstraction-only libraries.
- **Note**: Unlike other builder interfaces, `IProcessConfigurationBuilder` can be injected directly from the Core package to aid in configuration.

### 2. Application Development (Implementation Layer)
If you are building a final application (Console, Desktop, etc.):
- **Recommended**: Full Application Setup (`CliInvoke.Core`, `CliInvoke`, and `CliInvoke.Extensions`)
- **Required**: `CliInvoke.Core` and `CliInvoke`
- **Optional**: `CliInvoke.Specializations` (if you specifically need Windows CMD or PowerShell support, **including** the `UsePowerShell` / `UseCmd` middleware).

## Installation Commands

Depending on the choice above, use the following `dotnet add package` commands:

- **Abstractions only**:
  `dotnet add package CliInvoke.Core`

- **Concrete Implementation**:
  `dotnet add package CliInvoke.Core`
  `dotnet add package CliInvoke`

- **Full Application Setup (Recommended)**:
  `dotnet add package CliInvoke.Core`
  `dotnet add package CliInvoke`
  `dotnet add package CliInvoke.Extensions`

- **Adding Specializations** (includes platform middleware):
  `dotnet add package CliInvoke.Specializations`

> **Middleware note:** `CliInvoke.Extensions` provides the `UseLogging` and `UsePostExitValidation` middleware; `CliInvoke.Specializations` provides the `UsePowerShell` and `UseCmd` platform middleware. Both require the `ProcessInvoker` (from `CliInvoke`) — they are not available through `CliRun`.

This is a pure knowledge skill and does not invoke external tools.
