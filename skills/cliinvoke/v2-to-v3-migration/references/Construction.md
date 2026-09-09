# Construction Migration Reference

This reference maps v2-style construction patterns to v3 replacements.
It mirrors the [Shared Construction walkthrough](../../../site/docs/migration-guides/3.0.0.md#shared-construction-v3-default) in the migration guide.

## Default: init construction

In v3, `ProcessConfiguration` is directly constructible via init setters
with a required `TargetFilePath`. The builder is reserved for advanced
scenarios.

### Before (v2-style)

```csharp
// v2: ProcessConfigurationFactory or mutable TargetFilePath
ProcessConfiguration config = new ProcessConfiguration("dotnet")
{
    TargetFilePath = "dotnet",
    Arguments = "--version"
};
```

### After (v3)

```csharp
// v3: convenience constructor — TargetFilePath is required
ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");

// v3: object initializer — TargetFilePath is required via init setter
ProcessConfiguration config = new ProcessConfiguration
{
    TargetFilePath = "dotnet",
    Arguments = "--version"
};
```

## ArgumentList replaces ArgumentsList

### Before (v2-style)

```csharp
// v2: ArgumentsList property
ProcessConfiguration config = new ProcessConfiguration("dotnet")
{
    ArgumentsList = new List<string> { "--list-sdks", "--verbosity", "minimal" }
};
```

### After (v3)

```csharp
// v3: ArgumentList replaces the old ArgumentsList property
ProcessConfiguration config = new ProcessConfiguration("dotnet")
{
    ArgumentList = ["--list-sdks", "--verbosity", "minimal"]
};
```

## Working-directory validation at construction

In v3, setting `WorkingDirectoryPath` to a non-existent directory throws
`DirectoryNotFoundException` at construction time.

```csharp
// v3: working-directory validation throws at construction time
ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version")
{
    WorkingDirectoryPath = "/nonexistent" // throws DirectoryNotFoundException
};
```

## Builder — advanced only

The builder is not the default. Use it only when you need:

- **Argument escaping** — `ConfigureArguments(Action<ArgumentsSpec>)`
- **UserCredentialSpec** — `ConfigureUserCredential(Action<UserCredentialSpec>)`
- **Resource-policy callbacks** — `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)`

```csharp
// Advanced: builder for escaping/credentials/policy
using CliInvoke.Builders;

IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("dotnet")
    .SetArguments(["--info"])
    .SetOutputRedirection(true);

ProcessConfiguration config = builder.Build();
```

## Builder-methods-to-init-properties mapping

| Builder method | Init property | Notes |
|----------------|---------------|-------|
| `SetTargetFilePath(string)` | `TargetFilePath` | Required in both paths |
| `SetArguments(string)` | `Arguments` | Verbatim string |
| `SetArguments(IEnumerable<string>)` | `ArgumentList` | Init-only |
| `ConfigureArguments(Action<ArgumentsSpec>)` | *(no direct init)* | Escaping, validation |
| `SetOutputRedirection(bool)` | `OutputRedirection` | Default differs: `true` (init) vs `false` (builder) |
| `SetWorkingDirectory(string)` | `WorkingDirectoryPath` | Builder validates existence |
| `SetWindowCreation(bool)` | `WindowCreation` | |
| `ConfigureShellExecution()` | `UseShellExecution` | |
| `ConfigureEnvironmentVariables(Action<EnvironmentVariablesSpec>)` | `EnvironmentVariables` | |
| `ConfigureUserCredential(Action<UserCredentialSpec>)` | `Credential` | Advanced: SecureString staging |
| `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)` | `ResourcePolicy` | Advanced: pairing logic |

## Removed concepts

| Removed | Replacement |
|---------|-------------|
| `ProcessConfigurationFactory` | Construct `ProcessConfiguration` directly |
| `ArgumentsList` property | `ArgumentList` init property |
| `new ProcessConfigurationBuilder()` as default | Init construction (convenience constructor or object initializer) |

## Cross-references

- [Configuration guide](../../../site/docs/guides/configuration.md) — full configuration reference
- [Migration guide — Shared Construction](../../../site/docs/migration-guides/3.0.0.md#shared-construction-v3-default)
