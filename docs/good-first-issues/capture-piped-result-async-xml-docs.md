# Document IExternalProcess.CapturePipedResultAsync

The `CapturePipedResultAsync` method on `IExternalProcess` (`src/CliInvoke.Core/Processes/IExternalProcess.cs:109-115`) has an empty `<summary>` and `<returns>` tag. The method right above it, `CaptureBufferedResultAsync` (lines 95-107), has complete docs you can follow.

## What to do

Write a `<summary>` and `<returns>` for `CapturePipedResultAsync` that match the style of `CaptureBufferedResultAsync`, adjusted for piped output.

## Acceptance criteria

- The method has a filled `<summary>` and `<returns>`.
- `dotnet build` succeeds with no warnings.
- No interface or behavior changes.

## Why this is a good start

The template sits twelve lines above. You learn how interface documentation reads and how piped and buffered results differ.
