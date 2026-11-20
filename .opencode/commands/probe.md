---
description: Probe utility tokens against the example configuration — what matched, emission order, effective values
---

Probe a set of utility tokens in-process against `Examples/Enx.Atomic.Avalonia.Example.Config`'s
`MiniTheme` configuration, then read the report. `$ARGUMENTS` is passed straight to the CLI, so quote
the token list: `/probe "hover:ml-4 p-4" --control Button --pseudo :pointerover`.

```bash
dotnet run --project Examples/Enx.Atomic.Avalonia.Example.Config -- probe $ARGUMENTS
```

The positional argument is scanned with the configured extractors, so markup works too:
`probe 'Classes="hover:ml-4 p-4"'`. Add `--file <path>` (repeatable) to scan files, `--control <Type>`
(e.g. `Button`, `Border`, `TextBlock`) to apply the result to a real control, `--width <px>` to size its
window (default 800), `--pseudo <:pointerover>` (repeatable) to activate states, and `--json` for the
stable machine-readable document instead of the Spectre report.

How to read it:

- `variants:` — each prefix peeled off, outermost first, and the variant class that recognised it.
  `base:` is what remained and what rules were matched against.
- `rule:` — the winning rule (static rules win, then dynamic rules in registration order) with its
  declaration index and order rank.
- `style n/N` — one resolved `Style` per line. `n` is its 1-based position in `StyleOrdering.Sort`'s
  output for the whole probed set; the last one to be inserted wins at equal priority. `key:` shows the
  sort-key parts (specificity, container group/order, rule index, rank) that produced that position.
- `container:` — the container query the style is scoped to (`sm:`, `max-sm:`, ...), if any.
- `setters:` — property expression = value (CLR type). `DynamicResource(...)` means the value comes from
  the resource dictionary, listed under `resources:`.
- `hint:` — why an unmatched token most likely missed: a recognised variant with an unknown base, or an
  unknown prefix in front of a token a rule does understand.
- `control:` (with `--control`) — the effective value Avalonia ended up with for every property the
  styles touched, plus the recomposed `Margin`/`Padding`/`BorderThickness`/`CornerRadius` and the resource
  keys that supplied a `DynamicResource` value.

Use it before/after changing a rule, variant or theme scale (the delta in positions and setters is the
review), and to reproduce a reported bug *before* writing its regression test — then turn the probed
tokens into the test's source. Don't edit `GeneratedStyles/` from its output; fix the rule or the
emitter.
