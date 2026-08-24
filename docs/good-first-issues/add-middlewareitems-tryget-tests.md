# Add unit tests for MiddlewareItems.TryGet<T>

`MiddlewareItems` has a `TryGet<T>` method, but `tests/CliInvoke.Tests/Middleware/MiddlewareItemsTests.cs` tests only `Get<T>`. The `TryGet<T>` tests are missing.

This is the leftover from issue 409, which asked for `TryGet<T>` and is now implemented. The feature exists. What is missing is test coverage.

## What to do

Add tests that cover:

- A key that exists returns `true` and the correct typed value.
- A missing key returns `false` and `value` is `default`.
- A key whose stored type does not match `T` returns `false`.

Follow the existing `Get<T>` tests in the same file for setup and naming. The project uses TUnit.

## Acceptance criteria

- New tests pass with `dotnet test` from `tests/CliInvoke.Tests`.
- Tests cover the three cases above.
- No changes to `src/`.

## Why this is a good start

The test file already shows the pattern. You learn how tests are written and run here, and you cover a real method that has none.
