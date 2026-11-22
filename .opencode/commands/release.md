---
description: Prepare a release vX.Y.Z — ship the PublicAPI files, date the CHANGELOG, verify. Never tags or pushes.
---

Prepare the release: $ARGUMENTS

The argument is the version, `X.Y.Z` (without the `v`). If it is missing, propose one from the
`[Unreleased]` section and the last tag (pre-1.0: a breaking change or a new feature bumps the
patch digit like every release so far, unless the maintainer says otherwise), then ask before
going on.

Current state:

!`git describe --tags --abbrev=0 2>/dev/null || echo "(no tag yet)"`
!`git status --short --branch`
!`git log --oneline $(git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)..HEAD`

Run `git fetch origin` first (read-only). Stop and report if the current branch
(`git branch --show-current`) isn't `main`, if the working tree isn't clean, if `main` is ahead of
or behind `origin/main`, or if `[Unreleased]` in `CHANGELOG.md` is empty.

1. **Ship the public API.** For each `Sources/*/PublicAPI.Unshipped.txt`:
   - each `*REMOVED*<entry>` line: delete the matching `<entry>` line from `PublicAPI.Shipped.txt`
     (exact match), then drop the `*REMOVED*` line. If the entry isn't in Shipped, stop and report.
   - every other entry: append it to `PublicAPI.Shipped.txt`.
   - keep `#nullable enable` as the first line of both files and sort the rest of Shipped
     (ordinal), so the diff stays readable. Unshipped ends up holding only `#nullable enable`.
   - drop duplicate entries (RS0025), keep LF line endings and the final newline.
   Do this with a line-based script, never by hand: some entries are very long single lines
   (const values with escaped `\n`, e.g. `GridDefinitions.EmbeddableSource`) that read/edit tools
   may truncate.
2. **CHANGELOG.** Rename `## [Unreleased]` to `## [X.Y.Z] - <today, YYYY-MM-DD>` and add a fresh,
   empty `## [Unreleased]` above it. Don't rewrite the entries. In the link footer at the bottom,
   point `[Unreleased]` at `compare/vX.Y.Z...HEAD` and insert
   `[X.Y.Z]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v<previous>...vX.Y.Z`
   right below it.
3. **Verify** as `/verify` does: `dotnet csharpier check .`, `dotnet build Enx.Atomic.Avalonia.slnx
   -nologo` with 0 warnings and 0 errors (RS0016/RS0017 prove step 1 is right), and `dotnet test
   Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj` green.
4. **Show** the diff summary (entries shipped/removed per project, the CHANGELOG heading) and the
   commands that would finish the release:

   ```bash
   git commit -am "chore: release vX.Y.Z"
   git push
   git tag vX.Y.Z && git push origin vX.Y.Z   # triggers .github/workflows/release.yml → NuGet
   ```

Don't run any of them. Commits, pushes and tags need the maintainer's explicit go-ahead in that
turn, and the tag publishes to nuget.org, which can't be undone.
