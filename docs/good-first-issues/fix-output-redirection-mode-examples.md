# Fix code examples that use a non-existent OutputRedirectionMode

Nine documentation files show a `ProcessConfiguration` constructor call that passes `OutputRedirectionMode.Buffer` as the third argument. That type does not exist anywhere in the codebase. The constructor takes a `bool` for output redirection, not an enum.

Files to fix:

- `PATTERNS.md:70`, `PATTERNS.md:100`, `PATTERNS.md:114`
- `site/docs/guides/choosing-invocation-pattern.md:124`, `:173`, `:196`, `:237`
- `site/docs/guides/architecture.md:263`, `:296`

## What to do

Open each file and change the third constructor argument from `OutputRedirectionMode.Buffer` to a `bool`. Read the paragraph around the example to pick the right value. Examples that talk about capturing output usually want `true`. If the example is about not capturing, use `false`.

## Acceptance criteria

- No file references `OutputRedirectionMode`.
- Each example compiles against the real `ProcessConfiguration` constructor signature.
- No prose changes beyond the code block.

## Why this is a good start

You read real API usage and fix docs to match it. The work spans several files but each edit is the same mechanical change, and you cannot break the library by editing markdown.
