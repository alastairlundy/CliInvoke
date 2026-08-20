# ProcessConfiguration Disposal

`ProcessConfiguration` manages unmanaged resources, including the `StandardInput` stream and potentially a `SecureString` credential.

### Recommended Pattern: `using` statement

```csharp
using var config = new ProcessConfiguration("dotnet", "--version");
await invoker.ExecuteAsync(config);
// config is disposed here
```
