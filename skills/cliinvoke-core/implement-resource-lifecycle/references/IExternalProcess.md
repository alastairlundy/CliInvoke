# IExternalProcess Disposal

`IExternalProcess` wraps a `System.Diagnostics.Process` and must be disposed to release OS handles.

### Recommended Pattern: `await using`

```csharp
await using var process = invoker.StartAsync(config);
var result = await process.CaptureBufferedResultAsync(CancellationToken.None);
// process is disposed here
```
