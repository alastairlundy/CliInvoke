# Configuring with Spec Interfaces

This reference shows how to use `IProcessConfigurationBuilder` in conjunction with the `*Spec` configuration seams.

## Interacting with Spec Types

The `ProcessConfigurationBuilder` provides access to specialized configuration seams via `Configure*` methods. Each `Configure*` method accepts an `Action<*Spec>` callback where you configure the spec directly.

### Configuring Arguments
Use `ConfigureArguments` to access the `ArgumentsSpec` for complex argument construction.

```csharp
var builder = new ProcessConfigurationBuilder("dotnet");

builder.ConfigureArguments(args => 
{
    args.Add("run", escape: false);
    args.AddEnumerable(["--project", "MyProject.csproj"], escape: false);
    args.Add(123, escape: false);
});

var config = builder.Build();
```

### Configuring Environment Variables
Use `ConfigureEnvironmentVariables` to access the `EnvironmentVariablesSpec`.

```csharp
var builder = new ProcessConfigurationBuilder("dotnet");

builder.ConfigureEnvironmentVariables(env => 
{
    env.SetPair("ASPNETCORE_ENVIRONMENT", "Development");
    env.SetPair("MY_CUSTOM_VARIABLE", "Value");
});

var config = builder.Build();
```

### Configuring Resource Policy
Use `ConfigureProcessResourcePolicy` to access the `ProcessResourcePolicySpec`.

```csharp
var builder = new ProcessConfigurationBuilder("dotnet");

builder.ConfigureProcessResourcePolicy(policy => 
{
    policy.SetPriorityClass(ProcessPriorityClass.BelowNormal);
    policy.SetProcessorAffinity(0x01);
});

var config = builder.Build();
```

### Configuring User Credentials
Use `ConfigureUserCredential` to access the `UserCredentialSpec`.

```csharp
var builder = new ProcessConfigurationBuilder("cmd");

builder.ConfigureUserCredential(cred => 
{
    cred.SetUsername("admin");
    cred.SetPassword(securePassword);
});

var config = builder.Build();
```
