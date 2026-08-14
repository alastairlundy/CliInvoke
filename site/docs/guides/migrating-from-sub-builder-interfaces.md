<!-- This Source Code Form is subject to the terms of the Mozilla Public
     License, v. 2.0. If a copy of the MPL was not distributed with this
     file, You can obtain one at https://mozilla.org/MPL/2.0/. -->
---
title: "Migrating from Sub-Builder Interfaces"
layout: simple
---

# Migrating from Sub-Builder Interfaces

CliInvoke v3.0.0 replaces the four sub-builder interfaces with sealed
configuration spec classes. This guide shows what changed and how to
update your code.

## What changed

The following interfaces and their concrete builder implementations
have been removed:

| Removed interface | Removed implementation | Replacement |
|---|---|---|
| `IArgumentsBuilder` | `ArgumentsBuilder` | [`ArgumentsSpec`](#arguments--argumentsspec) |
| `IEnvironmentVariablesBuilder` | `EnvironmentVariablesBuilder` | [`EnvironmentVariablesSpec`](#environment-variables--environmentvariablesspec) |
| `IProcessResourcePolicyBuilder` | `ProcessResourcePolicyBuilder` | [`ProcessResourcePolicySpec`](#process-resource-policy--processresourcepolicyspec) |
| `IUserCredentialBuilder` | `UserCredentialBuilder` | [`UserCredentialSpec`](#user-credential--usercredentialspec) |

The new spec classes live in `CliInvoke.Core.Configuration` and are
configured through the `IProcessConfigurationBuilder.ConfigureXxx`
lambda methods introduced alongside them. They still expose a fluent
`Set*` / `Build()` surface, so the coding pattern is nearly identical
— the only change is the type name and namespace.

## Arguments → `ArgumentsSpec`

### Before (v2)

```csharp
using CliInvoke.Core.Builders;

IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("dotnet");

builder.ConfigureArguments(args =>
{
    args.Add("--version", escape: false);
    args.Add("--info", escape: true);
});
```

### After (v3)

```csharp
using CliInvoke.Core.Builders;
using CliInvoke.Core.Configuration;

IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("dotnet");

builder.ConfigureArguments(args =>
{
    args.Add("--version", escape: false);
    args.Add("--info", escape: true);
});
```

The lambda parameter changes from `IArgumentsBuilder` to
`ArgumentsSpec`. The `Add`, `AddEnumerable`, and `Build()` methods
remain the same. `EscapeCharacters` is no longer a public method —
escaping is handled internally based on the `escape` flag passed to
`Add`.

## Environment Variables → `EnvironmentVariablesSpec`

### Before (v2)

```csharp
using CliInvoke.Core.Builders;

builder.ConfigureEnvironmentVariables(env =>
{
    env.Set("PATH", "/usr/local/bin");
    env.SetEnumerable(new Dictionary<string, string>
    {
        ["HOME"] = "/home/user",
        ["LANG"] = "en_US.UTF-8"
    });
});
```

### After (v3)

```csharp
using CliInvoke.Core.Configuration;

builder.ConfigureEnvironmentVariables(env =>
{
    env.SetPair("PATH", "/usr/local/bin");
    env.SetEnumerable(new Dictionary<string, string>
    {
        ["HOME"] = "/home/user",
        ["LANG"] = "en_US.UTF-8"
    });
});
```

The lambda parameter changes from `IEnvironmentVariablesBuilder` to
`EnvironmentVariablesSpec`. Key differences:

- `Set(name, value)` is renamed to `SetPair(name, value)` for
  clarity.
- `SetEnumerable`, `SetDictionary`, and `SetReadOnlyDictionary`
  remain available.
- The spec constructor accepts an optional `StringComparer` and
  `throwExceptionIfDuplicateKeyFound` flag.

## Process Resource Policy → `ProcessResourcePolicySpec`

### Before (v2)

```csharp
using CliInvoke.Core.Builders;

builder.ConfigureResourcePolicy(policy =>
{
    policy.SetProcessorAffinity(0x01);
    policy.SetPriorityClass(ProcessPriorityClass.High);
    policy.ConfigurePriorityBoost(true);
});
```

### After (v3)

```csharp
using CliInvoke.Core.Configuration;

builder.ConfigureProcessResourcePolicy(policy =>
{
    policy.SetProcessorAffinity(0x01);
    policy.SetPriorityClass(ProcessPriorityClass.High);
    policy.ConfigurePriorityBoost(true);
});
```

The lambda parameter changes from `IProcessResourcePolicyBuilder` to
`ProcessResourcePolicySpec`. The builder method on
`IProcessConfigurationBuilder` is renamed from
`ConfigureResourcePolicy` to `ConfigureProcessResourcePolicy`.

Key differences in the spec API:

- `SetMinWorkingSet(nint?)` and `SetMaxWorkingSet(nint?)` are independent,
  nullable-aware setters so a policy can set only one working-set bound.
- `SetProcessorAffinity` accepts values ≥ 1 (the old builder
  required ≥ 1 as well, but the bounds were documented differently).
- `ConfigurePriorityBoost` and `SetPriorityClass` remain unchanged.

## User Credential → `UserCredentialSpec`

### Before (v2)

```csharp
using CliInvoke.Core.Builders;

builder.ConfigureUserCredential(cred =>
{
    cred.SetDomain("MYDOMAIN");
    cred.SetUsername("admin");
    cred.SetPassword(securePassword);
    cred.SetUserProfileLoading(true);
});
```

### After (v3)

```csharp
using CliInvoke.Core.Configuration;

builder.ConfigureUserCredential(cred =>
{
    cred.SetDomain("MYDOMAIN");
    cred.SetUsername("admin");
    cred.SetPassword(securePassword);
    cred.SetUserProfileLoading(true);
});
```

The lambda parameter changes from `IUserCredentialBuilder` to
`UserCredentialSpec`. The method names (`SetDomain`, `SetUsername`,
`SetPassword`, `SetUserProfileLoading`) are identical.

Key differences:

- `UserCredentialSpec` implements `IDisposable` and owns the
  `SecureString` lifetime. When used via the `ConfigureUserCredential`
  lambda on `IProcessConfigurationBuilder`, disposal is handled
  automatically by the builder's `Dispose` chain.
- `Build()` returns a `UserCredential` instance.

## Summary of API changes

| Concept | Old type (v2) | New type (v3) | Notable method changes |
|---|---|---|---|
| Arguments | `IArgumentsBuilder` | `ArgumentsSpec` | `EscapeCharacters` removed (internal) |
| Environment variables | `IEnvironmentVariablesBuilder` | `EnvironmentVariablesSpec` | `Set()` → `SetPair()` |
| Process resource policy | `IProcessResourcePolicyBuilder` | `ProcessResourcePolicySpec` | `SetMinWorkingSet(nint?)`/`SetMaxWorkingSet(nint?)` independent setters; `ConfigureResourcePolicy` → `ConfigureProcessResourcePolicy` |
| User credential | `IUserCredentialBuilder` | `UserCredentialSpec` | None — method names preserved |

## Further reading

- [Configuration](configuration.md) — full configuration model
  reference and builder documentation.
- [CHANGELOG](../../../CHANGELOG.md) — entry for the v3.0.0
  config-seam hard break.
