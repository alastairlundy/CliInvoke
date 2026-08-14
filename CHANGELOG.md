<!-- This Source Code Form is subject to the terms of the Mozilla Public
     License, v. 2.0. If a copy of the MPL was not distributed with this
     file, You can obtain one at https://mozilla.org/MPL/2.0/. -->
# Changelog

All notable changes to CliInvoke will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0] — Unreleased

### Removed

- **Hard break — sub-builder interfaces removed.** The four sub-builder
  interfaces and their concrete implementations have been removed:
  `IArgumentsBuilder` / `ArgumentsBuilder`,
  `IEnvironmentVariablesBuilder` / `EnvironmentVariablesBuilder`,
  `IProcessResourcePolicyBuilder` / `ProcessResourcePolicyBuilder`, and
  `IUserCredentialBuilder` / `UserCredentialBuilder`.

### Added

- **Configuration spec classes.** Sealed `XxxSpec` classes replace the
  removed sub-builder interfaces: `ArgumentsSpec`,
  `EnvironmentVariablesSpec`, `ProcessResourcePolicySpec`, and
  `UserCredentialSpec`. They live in `CliInvoke.Core.Configuration` and
  are configured through `IProcessConfigurationBuilder.ConfigureXxx`
  lambda methods.
- **Migration guide.** See
  [Migrating from Sub-Builder Interfaces](site/docs/guides/migrating-from-sub-builder-interfaces.md)
  for before/after examples and a full mapping of old types to new types.
