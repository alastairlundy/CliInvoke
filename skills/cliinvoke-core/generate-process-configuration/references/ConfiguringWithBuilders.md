# Configuring with Builder Interfaces

This reference shows how to use `IProcessConfigurationBuilder` in conjunction with other builder interfaces.

## Interacting with Other Builders

The `ProcessConfigurationBuilder` provides access to specialized builders via `Configure*` methods.

### Configuring Arguments
Use `ConfigureArguments` to access the `IArgumentsBuilder` for complex argument construction.

```csharp
var builder = new ProcessConfigurationBuilder("dotnet");

builder.ConfigureArguments(args => 
{
    args.Add("run");
    args.AddEnumerable(["--project", "MyProject.csproj"]);
    args.Add(123, CultureInfo.InvariantCulture);
});

var config = builder.Build();
```

### Configuring Environment Variables
Use `ConfigureEnvironmentVariables` to access the `IEnvironmentVariablesBuilder`.

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
Use `ConfigureProcessResourcePolicy` to access the `IProcessResourcePolicyBuilder`.

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
Use `ConfigureUserCredential` to access the `IUserCredentialBuilder`.

```csharp
var builder = new ProcessConfigurationBuilder("cmd");

builder.ConfigureUserCredential(cred => 
{
    cred.SetUsername("admin");
    cred.SetPassword(securePassword);
});

var config = builder.Build();
```
