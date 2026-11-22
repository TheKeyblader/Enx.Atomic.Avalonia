---
description: Rewrite HANDOFF.md at the end of a session so the next one can pick up — state, what shipped, decisions and why, lessons, next steps
---

Update `HANDOFF.md` for the session that is ending. $ARGUMENTS

`HANDOFF.md` is a local note (excluded through `.git/info/exclude`, never committed) that the next
session reads after `AGENTS.md` and `ARCHITECTURE.md`. It is written **in French**, like the
conversation. Code identifiers, commands and commit hashes stay as they are.

Current state:

!`git status --short --branch`
!`git describe --tags --abbrev=0 2>/dev/null || echo "(no tag yet)"`
!`git log --oneline -15`
!`gh pr list --state all --limit 5 2>/dev/null`

## Structure

Keep this order. Put the new session at the top and keep earlier sessions below it, but shorten
them (see Rules).

```markdown
# Handoff — reprise de session

Note locale (non versionnée, exclue via `.git/info/exclude`) ... Lire d'abord `AGENTS.md` et `ARCHITECTURE.md`.

## Reprise au <YYYY-MM-DD> — <n>ᵉ session : <sujet en quelques mots>

### État
`main` = `origin/main` = `<hash>` (« <message> »), dernier tag `<vX.Y.Z>`, `[Unreleased]` <vide / contenu en une ligne>,
branches ou PR ouvertes, worktrees, modifications non commitées (et à qui elles appartiennent).

### Livré
Une sous-section par PR ou lot : numéro, merge commit, ce que ça change, vérification (tests, CI).

### Décisions et pourquoi
Choix validés par le mainteneur, options écartées et la raison. C'est ce qui évite de relitiguer.

### Leçons
Ce qui a coûté du temps et comment l'éviter. Une leçon qui doit s'appliquer à chaque session va
plutôt dans `AGENTS.md` ou un skill : propose-le au lieu de l'écrire seulement ici.

### Prochaines pistes
Ce qui reste, par priorité, en séparant ce que le mainteneur a demandé de ce qui n'est qu'une idée.

## <sessions précédentes, condensées>

## Conventions rappel
```

## Rules

- **Verify every claim** against `git log`, `gh pr view` and the code. Don't copy a previous
  section's claims forward without checking them: earlier handoffs have contained false claims.
- **Condense, don't accumulate.** A previous session shrinks to its still-useful facts: decisions,
  open items, lessons. Drop what is now obsolete, i.e. done, superseded, or already in `AGENTS.md`/
  `ARCHITECTURE.md`. Merge items that are still open into "Prochaines pistes" instead of leaving them
  scattered in old sections. Aim to keep the file readable in one pass (roughly 300 lines max).
- Remove items that are no longer true (rules the maintainer dropped, branches since deleted).
- No secrets, tokens, personal data or screenshots.
- Edit only `HANDOFF.md`. Don't commit, since the file is untracked by design. Finish by showing a
  short summary of what changed in it.
