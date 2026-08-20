# Setting Values Directly

This reference shows the simplest way to configure a process using only the direct methods of `ProcessConfigurationBuilder`.

For simple use cases, utilizing `IProcessConfigurationBuilder` may be verbose or add unnecessary complexity; in such instances, the `ProcessConfigurationFactory` static factory may be a more efficient alternative. For the most basic scenarios, such as specifying only a file path and arguments, you can instantiate `ProcessConfiguration` directly.

## Basic Value Configuration

The most common way to set up a configuration is by chaining the `Set*` methods. While not strictly required, chaining is preferred for improved readability.

```csharp
using CliInvoke;
using CliInvoke.Builders;
using CliInvoke.Core;

// Basic configuration with only value settings
IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("git")
    .SetArguments(["--version"])
    .SetWorkingDirectory(@"C:\Users\Dev\Project")
    .SetOutputRedirection(true)
    .EnableWindowCreation(false);

ProcessConfiguration config = builder.Build();
```

## Common Value Combinations

### Buffered Execution Setup
To support `ExecuteBufferedAsync`, you must set output redirection.

```csharp
using CliInvoke;
using CliInvoke.Builders;
using CliInvoke.Core;

IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("dotnet")
    .SetArguments(["--info"])
    .SetOutputRedirection(true); // Required for buffered output

ProcessConfiguration config = builder.Build();
```

### Administrator Setup
To run a process with elevated privileges.

```csharp
using CliInvoke;
using CliInvoke.Builders;
using CliInvoke.Core;

IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder("cmd")
    .SetArguments(["/c", "echo Hello Admin"])
    .RequireAdministratorPrivileges();

ProcessConfiguration config = builder.Build();
```

### Custom Target Path
When the executable is not in the system PATH.

```csharp
using CliInvoke;
using CliInvoke.Builders;
using CliInvoke.Core;

IProcessConfigurationBuilder builder = new ProcessConfigurationBuilder()
    .SetTargetFilePath(@"C:\CustomTools\my-tool.exe")
    .SetArguments(["--help"]);

ProcessConfiguration config = builder.Build();
```
