---
description: Explore the design of a feature before any code — read upstream UnoCSS, Avalonia and this repo, then propose 2-3 API shapes with a recommendation. Writes no code.
---

Design discussion for: $ARGUMENTS

The maintainer settles a roadmap-sized feature's API in conversation, not in a written plan doc. This
command prepares that discussion. **Do not write or edit any code, test, doc or config file**, and
don't create a branch. The output is a message in the conversation, in French (code identifiers
stay as they are).

## 1. Research (read the real source, not memory)

- **This repo**: the parts of `ARCHITECTURE.md` it touches, the existing mechanism closest to it, and the
  public API it would extend (`PublicAPI.Shipped.txt`, `roslyn` MCP `get_public_api` /
  `find_references`). Check `Sources/Enx.Atomic.Avalonia.Preset.Mini/README.md`, `PARITY.md` and the
  root README's Roadmap for prior decisions, and `HANDOFF.md` (if present: it's local) for discussions already held. Don't
  reopen a decision recorded there without saying so.
- **UnoCSS** (`unocss` reference): how upstream does it (`packages-presets/preset-mini/src`,
  `packages/core` for engine-level features), including the user-facing config shape. Cite files.
- **Avalonia 12.1.3** (`avalonia` and `avalonia-docs` references): the real constraints. That means
  selectors, `StyleQuery`, priorities, `AddOwner`, resources, and what codegen can emit. Cite files.

If the feature is small enough not to need a design (a single token, an obvious port), say so and
suggest the matching skill instead.

## 2. Constraints to state up front

Say which repo invariants the feature touches. Common ones:

- codegen-first, with the runtime path as a sandbox
- `CodeGen` never references `Preset.Mini`
- emission order through `StyleOrdering`
- composite side properties
- `internal` by default, and public API declared in `PublicAPI.Unshipped.txt`
- breaking changes are acceptable pre-1.0 if justified

## 3. Options (2 or 3)

For each option:

- **Shape**: a short sketch of the API as the user would write it (config or C# usage), plus
  the main types. Keep it to a sketch, not an implementation.
- **How it fits**: which pipeline stage it lives in (extraction, transformers, shortcuts, rules,
  variants, ordering, emission, MSBuild) and what it changes there.
- **Trade-offs**: parity with UnoCSS, runtime/codegen behaviour, public API cost, test cost, and what
  it makes harder later (Roadmap items).

## 4. Recommendation

Pick one and say why in a few sentences. List the open questions the maintainer has to answer
before implementation (naming, defaults, scope cuts), each with your suggested answer.

Stop there and wait for the maintainer. Once a design is agreed, the implementation goes to the
`implementer` subagent with the full agreed design, then to `reviewer`.
