# Configuring with Spec Interfaces

This reference shows how to use `ProcessConfigurationBuilder` and the `*Spec` configuration seams for advanced scenarios. For the default path, see `SettingValues.md` — direct init construction is the recommended approach for most use cases.

## When to Use the Builder

`ProcessConfigurationBuilder` is an **advanced** tool. Use it only when you need one of these features that init construction cannot provide:

- **Argument escaping** — `ConfigureArguments(Action<ArgumentsSpec>)` lets you add arguments with per-argument escaping control.
- **User credential configuration** — `ConfigureUserCredential(Action<UserCredentialSpec>)` stages a `SecureString` password through the `UserCredentialSpec` seam (Windows only).
- **Resource policy configuration** — `ConfigureProcessResourcePolicy(Action<ProcessResourcePolicySpec>)` configures processor affinity, priority class, and other OS resource settings via a callback.

For all other cases, prefer direct init construction of `ProcessConfiguration`. The builder is sealed and cannot be subclassed.

## Interacting with Spec Types

The `ProcessConfigurationBuilder` provides access to specialized configuration seams via `Configure*` methods. Each `Configure*` method accepts an `Action<*Spec>` callback where you configure the spec directly.

### Configuring Arguments (Escaping)

Use `ConfigureArguments` to access the `ArgumentsSpec` for complex argument construction with per-argument escaping.

```csharp
using CliInvoke.Builders;

var builder = new ProcessConfigurationBuilder("dotnet");

builder.ConfigureArguments(args =>
{
    args.Add("run", escape: false);
    args.AddEnumerable(["--project", "MyProject.csproj"], escape: false);
    args.Add(123, escape: false);
});

var config = builder.Build();
```

> **Init alternative:** For simple argument lists without escaping, use the `ArgumentList` init-only property directly:
>
> ```csharp
> ProcessConfiguration config = new ProcessConfiguration("dotnet")
> {
>     ArgumentList = ["run", "--project", "MyProject.csproj"]
> };
> ```
>
> The `ArgumentList` property is an init-only snapshot — any list you supply is captured at construction time and later mutations to the original collection are not reflected. The v2 `ArgumentsList` property was merged into this init-only `ArgumentList` in v3.

### Configuring User Credentials

Use `ConfigureUserCredential` to access the `UserCredentialSpec`. This is the only supported way to stage a `SecureString` password for process execution (Windows only).

```csharp
using CliInvoke.Builders;

var builder = new ProcessConfigurationBuilder("cmd");

builder.ConfigureUserCredential(cred =>
{
    cred.SetUsername("admin");
    cred.SetPassword(securePassword);
});

var config = builder.Build();
```

The `UserCredentialSpec` implements `IDisposable` — when used through the builder, the builder owns and disposes it. If you create a `UserCredentialSpec` directly, you must dispose it yourself. The `UserCredential` produced by `Build()` has an independent lifetime and must also be disposed separately.

### Configuring Resource Policy

Use `ConfigureProcessResourcePolicy` to access the `ProcessResourcePolicySpec` for processor affinity, priority class, and other OS resource settings.

```csharp
using CliInvoke.Builders;

var builder = new ProcessConfigurationBuilder("dotnet");

builder.ConfigureProcessResourcePolicy(policy =>
{
    policy.SetPriorityClass(ProcessPriorityClass.BelowNormal);
    policy.SetProcessorAffinity(0x01);
});

var config = builder.Build();
```

### Configuring Environment Variables

Use `ConfigureEnvironmentVariables` to access the `EnvironmentVariablesSpec`.

```csharp
using CliInvoke.Builders;

var builder = new ProcessConfigurationBuilder("dotnet");

builder.ConfigureEnvironmentVariables(env =>
{
    env.SetPair("ASPNETCORE_ENVIRONMENT", "Development");
    env.SetPair("MY_CUSTOM_VARIABLE", "Value");
});

var config = builder.Build();
```

> **Init alternative:** For simple key-value pairs, use the `EnvironmentVariables` init-only property directly:
>
> ```csharp
> ProcessConfiguration config = new ProcessConfiguration("dotnet")
> {
>     EnvironmentVariables = new Dictionary<string, string>
>     {
>         ["ASPNETCORE_ENVIRONMENT"] = "Development",
>         ["MY_CUSTOM_VARIABLE"] = "Value"
>     }
> };
> ```
>
> The dictionary is captured as an immutable ordinal-sorted snapshot at construction time.

## Builder Lifecycle

`ProcessConfigurationBuilder` implements `IDisposable`. It owns the internal `UserCredentialSpec` and `StandardInput` `StreamWriter` it creates. Always dispose the builder when you are done with it — or better, use it in a `using` declaration:

```csharp
using CliInvoke.Builders;

using var builder = new ProcessConfigurationBuilder("cmd");
builder.ConfigureUserCredential(cred =>
{
    cred.SetUsername("admin");
    cred.SetPassword(securePassword);
});
ProcessConfiguration config = builder.Build();
```
