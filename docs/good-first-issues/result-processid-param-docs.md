# Document the processId parameter on result types

Three result constructors take a `processId` parameter with an empty description:

- `src/CliInvoke.Core/Primitives/Results/ProcessResult.cs:28`
- `src/CliInvoke.Core/Primitives/Results/BufferedProcessResult.cs:29`
- `src/CliInvoke.Core/Primitives/Results/PipedProcessResult.cs:27`

## What to do

Add a one line `<param>` description to each, such as "The operating system assigned process identifier." The three can share the same wording.

## Acceptance criteria

- All three `processId` parameters have a non-empty description.
- `dotnet build` succeeds with no warnings.
- No logic changes.

## Why this is a good start

It is the same small fix repeated three times. Good for learning the edit, build, and verify loop without any risk.
