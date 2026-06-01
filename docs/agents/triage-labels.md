# Triage Label Mappings

This file defines how the triage skill maps internal state names to actual labels in your issue tracker.

## Default Mappings (1:1)

The triage skill uses these label names directly:
- `needs-triage` → `needs-triage`
- `needs-info` → `needs-info`
- `ready-for-agent` → `ready-for-agent`
- `ready-for-human` → `ready-for-human`
- `wontfix` → `wontfix`

## Customization

If your issue tracker uses different label names for these states, you would modify this file to create the appropriate mappings.

For example, if your tracker uses:
- `triage` instead of `needs-triage`
- `info-needed` instead of `needs-info`

You would change the mappings accordingly.

## Usage

The triage skill reads this file to determine which labels to apply when moving issues through its state machine.
