---
name: package-installation-choice
description: Guides the selection and installation of the correct CliInvoke NuGet packages based on project type and requirements (Library vs App, Abstractions vs Implementation). USE FOR selecting the correct NuGet packages (Core, Implementation, Extensions, Specializations) based on project type (Library vs App). DO NOT USE FOR fixing NuGet restore errors.
---

# Package Installation Choice

This SKILL guides the selection of CliInvoke NuGet packages based on the project type and specific needs.

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
- **Optional**: `CliInvoke.Specializations` (if you specifically need Windows CMD or PowerShell support).

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

- **Adding Specializations**:
  `dotnet add package CliInvoke.Specializations`

This is a pure knowledge skill and does not invoke external tools.
