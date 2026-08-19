# Issue tracker: GitHub

Issues and specifications live in GitHub Issues. Use `gh` with argument arrays or body files; do not interpolate untrusted issue content into shell commands.

## Common operations

- Read: `gh issue view <number> --comments`
- Create: write the body to a temporary Markdown file, then run `gh issue create --title <title> --body-file <file>`
- Comment: `gh issue comment <number> --body-file <file>`
- Label ready work: apply `ready-for-agent`; if the label is absent, request approval before creating it once.
- Close only when the active workflow calls for resolution.

## Wayfinding operations

- Create one map issue labelled `wayfinder:map`.
- Create child issues first. Resolve a child's numeric database id with `gh api repos/{owner}/{repo}/issues/<child-number> --jq .id`, then attach it with `gh api --method POST repos/{owner}/{repo}/issues/<map-number>/sub_issues -F sub_issue_id=<database-id>`. Use a linked task list plus `Part of #<map>` only when sub-issues are unavailable.
- Prefer GitHub's native blocked-by relationship. Resolve the blocker's numeric database id the same way, then run `gh api --method POST repos/{owner}/{repo}/issues/<blocked-number>/dependencies/blocked_by -F issue_id=<blocker-database-id>`. The endpoint does not accept the visible issue number or GraphQL node ID in that field.
- Fall back to a `Blocked by: #<n>` line when native dependencies are unavailable.
- The frontier is every open child with no open blocker and no assignee.
- Claim before work with `gh issue edit <number> --add-assignee @me`.
- Resolve by posting the answer, closing the child, and appending a one-line linked gist to the map's Decisions-so-far.
