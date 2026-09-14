# ADR 0004: StandardInput superseded by PipeSource

- Status: accepted
- Date: 2026-09-14
- Ledger: `docs/decisions/DECISIONS-CliInvoke-v4-improvements.md#D009`

## Context

`ProcessConfiguration.StandardInput` (`StreamWriter?`, default `StreamWriter.Null`) and `ProcessConfigurationBuilder.SetStandardInputPipe(StreamWriter)` are the only standard-input mechanisms today. The v4 plan (`#T004`) introduces `PipeSource` factories attached via configuration init properties, which can express exactly what the `StreamWriter` mechanism expresses — plus what it cannot (files, bytes, in-memory strings, delegates).

Shipping the `PipeSource` stdin property alongside `StandardInput` creates two competing knobs over the same stream with no precedence rule. Every documentation page, test, and middleware branch would have to reason about both. The v3 GA story (ADR not needed; `#D007`) shipped "one construction path" as a principle; two stdin mechanisms would contradict its spirit on the data side.

The v3 GA construction story shipped with no deprecation ceremony — but that constraint (`#D007`) was scoped to landing the init-conversion at GA, which has since shipped. The period after GA is a different situation: a *future 3.x release* may add obsolescence annotations ahead of a planned major removal, without reopening the already-shipped construction story.

## Decision

stdin is superseded by `PipeSource`:

1. v4 exposes standard input exclusively as a `PipeSource` init property on `ProcessConfiguration`.
2. In a future 3.x release (post-GA), `ProcessConfiguration.StandardInput` and `ProcessConfigurationBuilder.SetStandardInputPipe(StreamWriter)` are marked `[Obsolete]` with a migration message.
3. In the v4 breaking window, both members are removed outright.
4. Migrators replace a raw `StreamWriter` with `PipeSource.FromStream` (e.g., `StreamWriter.BaseStream`).

## Consequences

**Positive**

- One stdin mechanism enters v4; no precedence rules, no undocumented interaction states.
- The `PipeSource` taxonomy (files, bytes, strings, streams, delegates) covers every case the `StreamWriter` property could express, plus cases it could not.
- v3 users get a compile-time signal (`[Obsolete]`) well before the removal, unlike the v3 construction story (which broke at GA without ceremony).

**Negative**

- v4 ships a second breaking change alongside the pipe feature work, increasing migration burden for early adopters.
- An `[Obsolete]` ceremony now precedes removal — a deliberate post-GA deviation from the `#D007` no-ceremony precedent.

**Mitigations**

- Obsolete annotation names `PipeSource.FromStream` so the migration is a one-call engagement well before the v3 removal.
- Release notes and `site/docs` migration guidance pair the `[Obsolete]` window with the 4.0.0 alpha/beta train (`#D014`), so removal is dated, not ambiguous.

## Alternatives considered

- **Coexist with precedence** — keep the `StreamWriter` property beside a `PipeSource` property with a documented winner. Rejected: two knobs forever; every doc, test and middleware filter must handle both (`#D009` options table, option B).
- **Deprecate-only (defer removal to a later major)** — rejected: leaves the v4 contract half-broken and costs the one-construction-path goal that motivated the entire v3 GA story.
- **Immediate removal in v3 GA line** — rejected: the v3 GA construction story already shipped (`#T005`/`#D007`); breaking it right after GA would punish adopting the stable line.
