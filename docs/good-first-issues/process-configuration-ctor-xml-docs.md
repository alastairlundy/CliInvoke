# Document the ProcessConfiguration constructor

The public constructor on `ProcessConfiguration` (`src/CliInvoke.Core/Primitives/ProcessConfiguration.cs:22-39`) has an empty `<summary>` and three empty `<param>` tags (`targetFilePath`, `arguments`, `outputRedirection`). The protected internal constructor just below it (lines 41-115) already has full docs, so you can mirror that wording.

## What to do

Copy the style and detail from the constructor at line 41 into the one at line 22. Describe each parameter: the executable path (`targetFilePath`), the arguments (`arguments`), and the output-redirection flag (`outputRedirection`) — a `bool` that controls whether standard output and error are redirected, **not** a working directory.

## Acceptance criteria

- The constructor at line 22 (signature at line 35) has a `<summary>` and three filled `<param>` tags.
- `dotnet build` succeeds with no warnings.
- No logic changes.

## Why this is a good start

The wording you need already exists a few lines down. You practice reading an existing pattern and extending it, which is most of what contributing here feels like.
