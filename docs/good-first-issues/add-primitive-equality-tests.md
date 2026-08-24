# Add equality and hash code tests for primitive types

Several primitive types implement `Equals` and `GetHashCode` but have no tests. There is no test file for them under `tests/CliInvoke.Tests/Primitives/`. `ProcessResultEqualityTests.cs` in that same folder shows the pattern to follow.

Types missing tests:

- `ProcessExitConfiguration`
- `UserCredential`
- `ShellInformation`
- `ProcessResourcePolicy`
- `BufferedProcessResult.ToString` and `ProcessResult.ToString`

## What to do

For each type, add a test file that checks:

- Two equal instances report equal and have matching hash codes.
- Two different instances report not equal.
- The `==` and `!=` operators behave the same as `Equals`.

Copy the structure of `ProcessResultEqualityTests.cs` and swap in each type's constructor arguments.

## Acceptance criteria

- New tests pass with `dotnet test` from `tests/CliInvoke.Tests`.
- Each listed type has at least basic equality and hash code coverage.
- No changes to `src/`.

## Why this is a good start

The template is in the same directory. You learn the testing layout and get comfortable constructing these types, which show up across the library.
