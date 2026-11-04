# ADR 0005: Arguments is a preformatted string; ArgumentList keeps values separate

- Status: accepted
- Date: 2026-11-04

## Context

`ProcessConfiguration` accepts command-line arguments in two ways: `Arguments` (a string,
carried over from the pre-v3 API) and `ArgumentList` (a list of discrete entries). Shell
composition reads both. When `ArgumentList` is non-empty, each entry is quoted or escaped for
the target shell and composed individually. Otherwise the whole `Arguments` string is escaped
and composed as a single line.

No written contract stated what `Arguments` guarantees about the values inside it. The POSIX
composition passes the escaped string to the shell as written, so a space inside it splits the
text. A value such as `safe.txt --overwrite` therefore reaches the target as two arguments.

Reviewers and agents have repeatedly read this splitting as a defect and proposed "fixing" it
by escaping spaces — without a record stating whether the behaviour is specified.

## Decision

1. `Arguments` is a preformatted command-line string. CliInvoke delivers it as written; it
   does not keep the values inside it separate, and any delimiter the consuming shell
   recognises (a space, on the POSIX path) splits the text. Quoting and escaping inside this
   string are the caller's responsibility.
2. `ArgumentList` keeps each value separate: one entry holds one value, and each entry is
   delivered to the target as exactly one argument, quoted or escaped for the target shell by
   the library.
3. When `ArgumentList` is non-empty it takes precedence over `Arguments` (existing behaviour;
   now stated as contract).
4. `EscapeForPosixShell` deliberately does not escape spaces. Escaping them would silently
   change the semantics of every existing caller that relies on the string carrying multiple
   values, and would not make the string a single-value channel for other delimiters.
5. The contract is published in the XML documentation of both members, in the README, and in
   `site/docs`, with a pointer entry in GLOSSARY. Regression tests assert the splitting
   behaviour of the string form as specified behaviour, so it cannot drift unnoticed.

## Consequences

**Positive**

- One written answer to a question that has been re-litigated; the string/list asymmetry reads
  as intent rather than oversight.
- Docs, tests, and XML comments agree on both members, so agents stop proposing space-escaping
  "fixes".
- No runtime behaviour changes: this ADR records, it does not alter.

**Negative**

- Callers who assumed per-value preservation inside `Arguments` must migrate to `ArgumentList`
  to get it; for them this is a behaviour they were relying on but never had.
- The word "safe" in existing escaper documentation is now visibly scoped to the string's own
  delivery, not to per-value guarantees.

**Mitigations**

- The README and `site/docs` sections pair the contract with the migration one-liner (move a
  single value to `ArgumentList` as one entry) so the correction is actionable, not just a
  rule.
- The XML documentation update for both members follows this record in the same documentation
  pass, so IntelliSense and prose cannot disagree.

## Alternatives considered

- **Escape spaces in `EscapeForPosixShell`** — rejected: silently changes multi-value legacy
  callers into single-value callers, leaves every other delimiter unaddressed, and converts a
  documented contract into an undocumented one.
- **Make `Arguments` preserve boundaries (auto-quote)** — rejected: changes composition for all
  existing string callers at once; a breaking behaviour change in a patch line.
- **Deprecate `Arguments`** — rejected for now: the preformatted-line form is a legitimate use
  and removing it is a major-version question, not a contract question.
