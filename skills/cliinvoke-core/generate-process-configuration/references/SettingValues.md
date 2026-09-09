# Setting Values Directly

This reference shows the default and recommended way to configure a process: direct init construction of `ProcessConfiguration`. The builder path (`ProcessConfigurationBuilder`) is reserved for advanced scenarios — see `ConfiguringWithBuilders.md`.

## Init Construction (Default)

In v3, `ProcessConfiguration` is directly constructible via an object initializer with init-only properties and a required `TargetFilePath`. A convenience constructor marked `[SetsRequiredMembers]` covers the common case.

### Convenience Constructor

The simplest path — pass the target file path and optional arguments:

```csharp
using CliInvoke.Core;

// Convenience constructor — TargetFilePath is required, Arguments default to ""
ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
```

```csharp
using CliInvoke.Core;

// With arguments
ProcessConfiguration config = new ProcessConfiguration("dotnet", "build --configuration Release");
```

```csharp
using CliInvoke.Core;

// With output redirection disabled
ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version", outputRedirection: false);
```

### Object Initializer

For full control, use the object initializer. `TargetFilePath` is required and must be set — the init setter throws `ArgumentException` on null or empty:

```csharp
using CliInvoke.Core;

ProcessConfiguration config = new ProcessConfiguration
{
    TargetFilePath = "dotnet",
    Arguments = "--version"
};
```

```csharp
using CliInvoke.Core;

// Multiple properties via init setters
ProcessConfiguration config = new ProcessConfiguration
{
    TargetFilePath = "git",
    Arguments = "status",
    WorkingDirectoryPath = @"C:\Users\Dev\Project",
    OutputRedirection = true,
    RequiresAdministrator = false,
    WindowCreation = false
};
```

### ArgumentList (v3 init-only property)

The v2 `ArgumentsList` property was merged into the init-only `ArgumentList` in v3. Use it for pre-tokenised argument lists — each entry is passed to the child process unmodified via `ProcessStartInfo.ArgumentList`:

```csharp
using CliInvoke.Core;

ProcessConfiguration config = new ProcessConfiguration("dotnet")
{
    ArgumentList = ["run", "--project", "MyProject.csproj", "--verbosity", "minimal"]
};
```

> `ArgumentList` is a snapshot: any list you supply is copied at construction time. Later mutations to the original collection are not reflected in the configuration.

### Environment Variables

```csharp
using CliInvoke.Core;

ProcessConfiguration config = new ProcessConfiguration("dotnet")
{
    EnvironmentVariables = new Dictionary<string, string>
    {
        ["ASPNETCORE_ENVIRONMENT"] = "Development",
        ["MY_CUSTOM_VARIABLE"] = "Value"
    }
};
```

> The dictionary is captured as an immutable ordinal-sorted snapshot at construction time.

### Working Directory

`WorkingDirectoryPath` defaults to `Directory.GetCurrentDirectory()`. Setting it to a non-existent directory throws `DirectoryNotFoundException` at construction time:

```csharp
using CliInvoke.Core;

ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version")
{
    WorkingDirectoryPath = @"C:\Users\Dev\Project"
};
```

### Requiring Administrator Privileges

```csharp
using CliInvoke.Core;

ProcessConfiguration config = new ProcessConfiguration("cmd", "/c echo Hello")
{
    RequiresAdministrator = true
};
```

### Shell Execution

```csharp
using CliInvoke.Core;

ProcessConfiguration config = new ProcessConfiguration("cmd", "/c echo Hello")
{
    UseShellExecution = true
};
```

> **Warning:** Using shell execution whilst also redirecting standard input will throw an exception. This is a known limitation of `System.Diagnostics.Process`.

## No-Mutation Contract

`ProcessConfiguration` properties are all init-only. Once constructed, the configuration cannot be mutated. The resolved file path (after `FilePathResolver` processing) surfaces on the result as `ProcessResult.ExecutedFilePath`, not by mutating the configuration.

## When to Use the Builder Instead

The `ProcessConfigurationBuilder` is reserved for scenarios where init construction cannot express what you need:

- **Argument escaping** — per-argument escaping control via `ConfigureArguments`
- **User credential staging** — `SecureString` password setup via `ConfigureUserCredential` (Windows only)
- **Resource policy callbacks** — processor affinity and priority class via `ConfigureProcessResourcePolicy`

For everything else, init construction is the v3 default. See `ConfiguringWithBuilders.md` for the advanced builder path.
