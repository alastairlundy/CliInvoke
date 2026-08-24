# Add XML doc comments to InvocationMode enum values

The `InvocationMode` enum in `src/CliInvoke.Core/Primitives/Invocation/InvocationMode.cs` has four values: `Raw`, `Buffered`, `Piped`, and `FireAndForget`. Each one has an empty `<summary>` tag (lines 12-32). The public API reference builds from these comments, so right now those values show up blank in the docs.

## What to do

Write a one or two sentence `<summary>` for each enum value that says what it does. For example, `Buffered` keeps standard output and error in memory so you can read them after the process exits.

Look at how other enums in the same folder document their values if you want a style reference.

## Acceptance criteria

- All four values have a non-empty `<summary>`.
- `dotnet build` succeeds with no warnings.
- No public API or behavior changes.

## Why this is a good start

The change stays in one file. You learn where XML doc comments live and how they feed the public API docs, without touching any logic.
