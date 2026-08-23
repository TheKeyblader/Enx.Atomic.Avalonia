# Architecture

Enx.Atomic.Avalonia is an atomic utility-class engine for Avalonia, inspired by
[UnoCSS](https://unocss.dev/) — the vocabulary (`Rule`, `Variant`, `Extractor`,
`Theme`) and the token-resolution logic (`hover:bg-red-500`, `sm:flex-row`, ...) are
lifted directly from it, adapted to Avalonia properties and selectors instead of CSS.

## Projects

| Project | Role |
|---|---|
| `Sources/Enx.Atomic.Avalonia` | Generic engine: resolves tokens into styles, independent of any preset. |
| `Sources/Enx.Atomic.Avalonia.Preset.Mini` | A concrete set of rules/variants/parts, inspired by `@unocss/preset-mini`. |
| `Sandbox` | Avalonia console app used as a manual test bed for the engine and the presets. |

## Engine concepts (`Sources/Enx.Atomic.Avalonia`)

- **`IRule`** — either `IStaticRule` (`Rule.Static`), which matches a token by exact
  name (`"hidden"`) and carries a fixed set of `StyleValue`, or `IDynamicRule<TTheme>`
  (`Rule.Dynamic<TTheme>` or a dedicated class), which matches by regex and computes
  its `StyleValue`s from the match and the theme (`RuleContext<TTheme>`).
- **`StyleValue`** — `Literal<TValue>` (value fixed at resolution time) or `Resource`
  (a runtime `DynamicResource` lookup, for values that must track theme/resource
  changes).
- **`VariantBase<TTheme>`** — matches a token prefix/suffix (`hover:`, `sm:`) and
  produces a `VariantHandlerBase` that transforms the resolved style's selector or
  container query. Variants are retried in a loop until none match
  (`AtomicGenerator.MatchVariants`), which is what makes chaining
  (`hover:focus:...`) work.
- **`SelectorExpression` / `StyleQueryExpression`** (`Compact/`) — a linked-list, data
  representation of an `Avalonia.Styling.Selector` / `StyleQuery`, built by rules and
  variants without depending directly on `System.Linq.Expressions`. Converted to a
  compiled `Expression<Func<Selector, Selector>>` only at the very end
  (`AtomicGenerator.ApplyVariants`).
- **`AtomicConfiguration<TTheme>`** — assembles `Rules`, `Variants`, `PreProcessors`,
  `Extractors` and the `TTheme` instance.
- **`AtomicGenerator<TTheme>`** — the engine itself: extracts tokens from source text
  (`Extractor`), resolves them into `StringifiedUtil` (a compiled selector + `Setter[]`
  + optional container query), cached by raw token. A static rule always wins over
  dynamic ones; among dynamic rules, the first one (in `Rules` declaration order)
  whose `Match` returns a non-empty result wins — this is what lets two rules share
  the same prefix (`border-2` as width vs `border-red-500` as color) without a single
  convoluted regex.

## `Enx.Atomic.Avalonia.Preset.Mini`

- **`Parts/`** — one interface per theme scale (`ISpacingPart.Spacing`,
  `IColorPart.Colors`, `IRadiusPart.Radii`, `ISizePart.Sizes`,
  `ILineWidthPart.LineWidths`, `IFontSizePart.FontSizes`,
  `IBreakpointPart.Breakpoints`, `IRemToPxPart.RemToPxFactor`). Each dynamic rule
  requires, via `where TTheme : ...`, only the parts it actually needs — a user theme
  can implement any subset of them.
- **`Static/`** — static rules, independent of any theme (`hidden`, `cursor-pointer`,
  `flex-row`, ...).
- **`Dynamic/`** — theme-driven rules. Directional families (margin/padding/gap,
  corner-radius) are *consolidated* into a single class per family via named capture
  groups in the regex (`side`, `axis`, `bound`, `neg`) instead of one class per
  direction — e.g. `MarginRule` handles `m-*`, `mx-*`, `mt-*`, ... in a single `Match`.
  - **`ThemeScale`** — resolves a theme key, falling back to a bare number:
    `TryResolve` treats that number as **rem** (converted to px via
    `RemToPxFactor`) — the behavior of most UnoCSS scales (`spacing`, `borderRadius`,
    ...). `TryResolvePx` treats it as a **raw px** value, no conversion — this is
    specifically UnoCSS's `lineWidth` scale behavior (border/ring/outline), which is
    deliberately smaller and already in pixels. Don't mix the two on a new rule
    without checking which UnoCSS scale it's meant to follow.
- **`Variants/`** — `PseudoClassVariant` (maps `hover:`, `disabled:`, ... onto real
  Avalonia pseudo-classes such as `:pointerover`) and `BreakpointVariant` (maps
  `sm:` / `max-sm:` onto container queries — `Width(GreaterThanOrEquals/
  LessThanOrEquals, ...)`; note that Avalonia's `WidthQuery.ToString()` only maps
  `LessThanOrEquals`/`GreaterThanOrEquals` to `"max-width"`/`"min-width"`, not
  `LessThan`/`GreaterThan`).
- **`MiniTheme`** — the default implementation aggregating every part, with empty
  dictionaries on construction.
- **`Extensions/`** — `DefaultTheme` holds the default scales (ported from
  `preset-mini/src/_theme/*.ts` in UnoCSS, converted rem→px at 16px/rem, full
  Tailwind palette). `ThemeBuilderExtensions` exposes one `Add*Rules(builder,
  configuration)` method per part: it seeds the theme's dictionary **and** registers
  the corresponding rules/variants into the `AtomicConfiguration`, with dedup (by
  rule type for dynamic rules/variants, by `Name` for static rules, since they all
  share the same CLR type `Rule.Static`). `AddMiniTheme` chains all of them, in an
  order that matters: rules with a more specific prefix (`BorderWidthRule`,
  `FontSizeRule`) must be registered before the ones sharing their generic prefix
  (`BorderColorRule`, `ForegroundColorRule`), to win the ambiguity.

## Current pipeline: runtime resolution

Today, `AtomicGenerator<TTheme>.Generate(...)` runs **at application runtime**: it
scans text, resolves tokens, compiles the `Expression<Func<Selector, Selector>>`
via `System.Linq.Expressions`, and produces `StringifiedUtil`s ready to become
`Avalonia.Styling.Style`s. `Sandbox/Program.cs` exercises this end to end.

## Build-time C# code generation (planned, not yet implemented)

The goal is to move this resolution to the **build** of the consuming application,
producing a static `.g.cs` file (a `Styles` class) instead of paying the
resolution/expression-compilation cost on every startup. Design so far:

1. **The user's configuration project becomes an executable.** It references
   `Enx.Atomic.Avalonia`(`.Preset.Mini`), builds its `AtomicConfiguration<TTheme>` in
   C#, and exposes a CLI entry point (`RunCli(string[] args)`) that: reads the source
   file paths (XAML + C#) passed as arguments, runs them through the existing
   `Extractor`s to pull out tokens, calls `Generate()`, then emits C# through a
   dedicated emitter.
2. **An MSBuild `.targets`** adds a `ProjectReference` from the consuming project to
   the configuration project (guarantees build order), then runs that CLI via
   `<Exec>` (no custom C# MSBuild task — a plain `Exec` is enough) with the consuming
   project's `@(Compile)` and `@(AvaloniaXaml)` items, and a user-facing property
   `EnxAtomicStylesOutputPath` (sensible default, e.g.
   `$(IntermediateOutputPath)GenStyles.g.cs`) as output — also used as the target's
   `Outputs` for incrementality. The generated file is added back as
   `<Compile Include="$(EnxAtomicStylesOutputPath)" />`.
3. **An emitter working directly off the data, not off compiled `Expression`s.**
   `SelectorExpression`/`StyleQueryExpression` are already data trees; the new
   emitter should walk them directly to produce C# text
   (`Selectors.Is<Button>(selector).Class("hover:bg-red-500")`), **without** going
   through `Expression.Compile()` and decompiling — see the History section below for
   why that approach was abandoned.

### Known constraint: custom components and the build cycle

If the configuration project references `AvaloniaProperty`s of custom controls
defined in the consuming project itself, that's a cycle: the consumer needs the
`.g.cs` generated by the config project to compile, and the config project needs the
consumer's types to write rules against them. MSBuild cannot resolve
`ProjectReference` cycles.

**Current decision: don't try to lift this constraint technically.** A user with
custom controls referenced by rules must define them in a separate `Components`
project, referenced by both `Config` and the consuming app — the same constraint
Roslyn source generators already impose ("a generator can never reference the
assembly it generates into"). A two-pass compile within a single project (the way
Uno.UI's XAML compiler does it) was considered and set aside for now: far more
complexity and fragility than the current need justifies.

## History: why the old code-generation architecture was dropped

Before the `Reset` commit, a C# generation architecture already existed
(`Emitter/StyleEmitter.cs`, `Sandbox/GenStyles.cs`). It had the right idea (bake
styles into static C#) but a fragile implementation, dropped at `Reset`:

- The selector was already built as a compilable `Expression<Func<Selector,
  Selector>>` — but the emitter converted it back to text via
  `FastExpressionCompiler`, a *debug/perf* library for decompiling `Expression`s, not
  a supported code-generation tool. A Data → Expression → text →
  (future) compile round trip, for a value that was already directly invocable.
- The rest (setters, values) was emitted by a separate, uncoordinated hand-rolled
  system (`ValueEmitter` + `AvantiPoint.CodeGenHelpers`) — two unrelated
  text-generation strategies for a single `Style`.
- A dependency on `Microsoft.CodeAnalysis` + `CodeGenHelpers` without using any of the
  actual incremental-source-generator machinery — just a `StringBuilder` in
  disguise, run by hand from a console app.
- `GenStyles.cs` was **committed**, regenerated by hand by re-running
  `Sandbox/Program.cs` against a hardcoded token string — no guarantee it stayed in
  sync with the actual configuration.
- Latent bugs: no variable-name dedup in `SolidColorBrushEmitter` (two tokens with
  the same color → a duplicate variable in the generated file → doesn't compile), and
  a duplicated reflection lookup for the static field backing an `AvaloniaProperty`
  (once in `SelectorExpression.PropertyEquals`, once in
  `StyleEmitter.GetAvaloniaPropertyName`).

The new generation architecture (section above) keeps the goal but fixes these:
direct emission from `SelectorExpression`/`StyleQueryExpression` nodes (never
decompiling a compiled `Expression`), an isolated CLI process instead of an in-process
assembly load (`AssemblyLoadContext`) to avoid version conflicts, and output
regenerated on every build via MSBuild instead of hand-committed.

---

## Colophon

This document was drafted by Claude (Anthropic), via Claude Code, from the design
discussion and codebase archaeology done in that session with
[@TheKeyblader](https://github.com/TheKeyblader). The decisions it records — the
runtime engine's design, the planned build-time generation approach, the choice not
to lift the custom-component build cycle technically — are his; the wording is a
draft he is responsible for the accuracy of, per this repository's
[AI use policy](https://github.com/TheKeyblader/.github/blob/main/CONTRIBUTING.md#ai-use).
