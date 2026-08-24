# Document the ProcessExitConfiguration TimeSpan constructor

The `ProcessExitConfiguration(TimeSpan)` constructor (`src/CliInvoke.Core/Primitives/ProcessExitConfiguration.cs:30-33`) has an empty `<summary>` and an empty `<param name="timeoutTimeSpan">`. The sibling constructor at lines 42-59 already documents the same idea.

## What to do

Mirror the wording from the constructor at line 42 for the one at line 30. Describe the timeout and what the parameter sets.

## Acceptance criteria

- The constructor at line 30 has a `<summary>` and a filled `<param>`.
- `dotnet build` succeeds with no warnings.
- No logic changes.

## Why this is a good start

You copy a proven pattern from the same file. Low risk, and you see how constructors overload each other in the public API.
