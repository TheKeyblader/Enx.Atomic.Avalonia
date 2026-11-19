---
description: Update ARCHITECTURE.md, README.md, CHANGELOG.md and the Preset.Mini README together for recent changes
---

Update the project documentation for: $ARGUMENTS

If no scope was given, use the changes since the last tag:

!`git describe --tags --abbrev=0 2>/dev/null || echo "(no tag yet)"`
!`git log --oneline $(git describe --tags --abbrev=0 2>/dev/null || git rev-list --max-parents=0 HEAD)..HEAD`
!`git status --short`

Update **all four** together — they must not drift:

1. `ARCHITECTURE.md` — the source of truth for *how* it works: new mechanisms, data flow, the
   constraints and gotchas behind design decisions. Prose explaining *why*, matching the existing
   sections; don't restate code.
2. `README.md` — user-facing overview. Keep the **Roadmap** in sync: remove a bullet once it has
   shipped (don't leave "done" items), and adjust the milestone intro paragraph if a milestone
   becomes empty.
3. `CHANGELOG.md` — add entries under `## [Unreleased]` only (Keep a Changelog style: Added /
   Changed / Fixed / Removed). Never invent a version number or date; releases are tag-driven.
4. `Sources/Enx.Atomic.Avalonia.Preset.Mini/README.md` — the token reference: every new/changed
   token, variant or theme scale in its tables, with the Avalonia property it sets.

Rules: docs are in English (the conversation may be in French); verify every claim against the
code (use the `roslyn` MCP tools or read the source) rather than from memory; don't touch the
Colophon sections. Show a summary of what changed in each file when done. Do not commit.
