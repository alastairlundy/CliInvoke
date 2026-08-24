# Fix a wrong ProcessResult.StandardOutput reference in PATTERNS.md

`PATTERNS.md:39` reads `result.StandardOutput` on a `ProcessResult`. That property only exists on `BufferedProcessResult`, not on the base `ProcessResult`. The example is therefore misleading.

## What to do

Pick the fix that keeps the example's point intact:

- Change the variable type to `BufferedProcessResult` so `StandardOutput` is valid, or
- Remove the `StandardOutput` line if the example does not need it.

## Acceptance criteria

- The example no longer calls `StandardOutput` on a `ProcessResult`.
- The surrounding explanation still makes sense.
- No code in `src/` changes.

## Why this is a good start

It teaches a real distinction in the API: `ProcessResult` versus `BufferedProcessResult`. Small, contained, and you learn something useful about the library.
