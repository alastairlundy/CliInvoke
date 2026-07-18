# Decision Ledger — CliInvoke Docs Site Fixes

Topic: address documentation site issues so the site is helpful to users and reads as a polished site, not a placeholder.

Scope: decisions in this ledger affect the documentation site at `docs/` and the rendered site output. The two specific items named in the kickoff are the Guides section structure and the landing page placeholder content.

Cross-references:
- Site source: `docs/`
- Build guidance: `docs/docs/building-cliinvoke.md`
- CI workflow: `.github/workflows/test.yml`
- Domain glossary: `CONTEXT.md` at the repo root.

---

### [D001] — session goal

- **Driver**: the user wants the documentation site to actually help users learn and use the library, not just exist as a half-built placeholder.
- **Resolved Answer**: "To fix the documentation site so that it is helpful to users and is a polished site."
- **Normalized Requirement**: The documentation site shall be helpful (users can find answers to their questions about CliInvoke) and polished (consistent design, no placeholder content, no half-finished sections).
- **Constraints**: The user named two specific items at kickoff: the Guides section (currently just a list of links, not a real guides section) and the landing page (placeholder content).

### [D002] — Guides section target

- **Driver**: the user wants the Guides section to be intuitive (readers can find what they need) and aesthetically pleasing (designed, not assembled).
- **Resolved Answer**: "Option 2 — Curated section."
- **Normalized Requirement**: The Guides section landing shall be a curated orientation covering audience, reading order, and summaries of each guide, and each guide shall be a substantial page with prose, examples, and links; the section shall feel cohesive, not assembled.
- **Constraints**: None.

### [D003] — landing page target

- **Driver**: the user wants the landing page to introduce what CliInvoke is, briefly explain what it offers and why to use it, and guide users to different parts of the docs by their need.
- **Resolved Answer**: "Option 2 — Three-block landing."
- **Normalized Requirement**: The landing page shall have three explicit blocks — "What CliInvoke is", "What it offers", and "Where to go next" — with the third block routing readers by need into the appropriate docs section; the page shall read as a story, not a checklist.
- **Constraints**: None.

<!-- next-id: D004 -->
