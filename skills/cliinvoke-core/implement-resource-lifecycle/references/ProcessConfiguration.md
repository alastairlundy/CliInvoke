# ProcessConfiguration Lifecycle

`ProcessConfiguration` is a plain immutable value object. It does **not** implement `IDisposable`.

## Construction

`ProcessConfiguration` is directly constructible via an object initializer with init-only properties. There is no factory — `ProcessConfigurationFactory` was removed in v3.

### Convenience Constructor

The `[SetsRequiredMembers]` convenience constructor covers the common case:

```csharp
using CliInvoke.Core;

ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version");
```

```csharp
using CliInvoke.Core;

// With output redirection disabled
ProcessConfiguration config = new ProcessConfiguration("dotnet", "--version", outputRedirection: false);
```

### Object Initializer

`TargetFilePath` is required and must be set via the init setter — it throws `ArgumentException` on null or empty:

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
    OutputRedirection = true
};
```

## No-Mutation Contract

All properties are init-only. Once constructed, the configuration cannot be mutated. The resolved file path (after `FilePathResolver` processing) surfaces on the result as `ProcessResult.ExecutedFilePath`, not by mutating the configuration.

## ProcessConfiguration Is NOT Disposable

`ProcessConfiguration` does **not** implement `IDisposable`. The invocation pipeline never adopts ownership of the disposable objects it references. Two of its properties are therefore **your** responsibility:

- **`StandardInput`** — a `StreamWriter` you supplied (or the default `StreamWriter.Null`). If you provide a real stream, you must dispose it after the final invocation that referenced it has completed.
- **`Credential`** — a `UserCredential` you supplied (or the default `UserCredential.Null`). If you provide a real `UserCredential`, you must dispose it.

CliInvoke will not close your `StandardInput` stream or wipe your `SecureString` for you. Hold these resources in their own `using` declarations:

```csharp
using var stdin = new StreamWriter(new MemoryStream());
using var credential = new UserCredential("domain", "user", password, false);

ProcessConfiguration config = new ProcessConfiguration("cmd")
{
    ArgumentList = ["/c", "echo hello"],
    StandardInput = stdin,
    Credential = credential
};

BufferedProcessResult result = await CliRun.RunBufferedAsync(config);
// config falls out of scope without disposal; stdin and credential are
// disposed by their own `using` declarations.
```

## Ownership Rules

1. `ProcessConfiguration` is a value object — no disposal required.
2. The caller that supplies `StandardInput` or `UserCredential` owns those objects and must dispose them.
3. Reusing a single configuration across multiple invocations is allowed — dispose `StandardInput` and `UserCredential` only after the final invocation.
4. The `UserCredentialSpec` staged through `ProcessConfigurationBuilder.ConfigureUserCredential` is owned and disposed by the builder; do not dispose it yourself. The `UserCredential` it produces has an independent lifetime and must be disposed separately.
