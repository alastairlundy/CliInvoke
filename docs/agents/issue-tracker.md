# Issue Tracker Configuration

This repository uses GitHub Issues for tracking work.

## Workflow

- Issues are created and managed via GitHub Issues
- The `gh` CLI is used for issue operations from the command line
- Standard GitHub issue labels and workflows apply

## Integration with Skills

Skills like `to-issues`, `triage`, `to-prd`, and `qa` interact with this issue tracker using:
- `gh issue create` for creating new issues
- `gh issue list` with filters for querying issues
- `gh issue edit` and `gh issue comment` for updates

This configuration assumes standard GitHub Issues functionality. If you use GitHub Projects or other GitHub-native features, they will work alongside this setup.