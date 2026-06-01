# Domain Documentation Layout

This repository follows a single-context layout for domain documentation.

## Structure

- `CONTEXT.md` at the repository root contains the core domain language and concepts
- `docs/adr/` directory at the repository root contains Architectural Decision Records

## How Skills Use This

Skills that read domain documentation expect this layout:
- `improve-codebase-architecture` reads `CONTEXT.md` to understand domain terminology
- `diagnose` consults `CONTEXT.md` for context during troubleshooting
- Skills that work with architectural decisions look in `docs/adr/` for ADRs

## File Contents

### CONTEXT.md
Should contain:
- Domain glossary (key terms and their meanings)
- Core business concepts
- Important domain rules and constraints
- Shared vocabulary for team communication

### docs/adr/
Should contain ADR files (typically Markdown) following a standard format:
- Title: Clear, descriptive title
- Status: Proposed, Accepted, Superseded, etc.
- Context: The problem or situation driving the decision
- Decision: What was decided
- Consequences: Outcomes, both positive and negative

## Multi-Context Alternative

If this were a multi-context repo (monorepo), there would be:
- `CONTEXT-MAP.md` at the root mapping context names to paths
- Per-context `CONTEXT.md` files in subdirectories
- Per-context `docs/adir/` directories

But this repository uses the simpler single-context approach.