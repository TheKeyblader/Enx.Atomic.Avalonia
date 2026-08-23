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
| `Sources/Enx.Atomic.Avalonia.CodeGen` | The build-time C# emitter — see below. |
| `Sandbox` | Avalonia console app used as a manual test bed for the engine and the presets. |
| `Tests/Enx.Atomic.Avalonia.Tests` | The automated test suite (xUnit v3). |

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
- **`ISourceTransformer<TTheme>`** (`Transformers/`) — rewrites a source file's *full
  text* before extraction, ported from UnoCSS's `SourceCodeTransformer`. Unlike
  `IPreProcessor`, which only ever sees one already-isolated token, a transformer sees
  everything at once — the tool for anything that needs several co-occurring tokens
  together (see [Source transformers and ghost properties](#source-transformers-and-ghost-properties)
  below). Transformers are grouped by `Enforce` (`Pre` → `Default` → `Post`) and run
  in that order, declaration order within a group, each seeing the previous one's
  output (`AtomicGenerator.ApplyTransformers`) — always before `ApplyExtractors`.
- **`AtomicConfiguration<TTheme>`** — assembles `Transformers`, `PreProcessors`,
  `Rules`, `Variants`, `Extractors` and the `TTheme` instance.
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
transforms the source text (`ApplyTransformers`), scans it for tokens
(`ApplyExtractors`), resolves them, compiles the `Expression<Func<Selector,
Selector>>` via `System.Linq.Expressions`, and produces `StringifiedUtil`s ready to
become `Avalonia.Styling.Style`s. `Sandbox/Program.cs` exercises this end to end.

## Source transformers and ghost properties

Implemented: `ISourceTransformer<TTheme>`, `SpecialProperties`, `GhostProperties`,
`GhostPropertyCombiner<TTheme>`, and the per-side/corner branches of `MarginRule`,
`PaddingRule` and `RoundedRule` targeting ghost properties. Verified end to end in
`Sandbox/Program.cs`.

**The problem.** Avalonia's `Margin`, `Padding`, `BorderThickness` and `CornerRadius`
are each a single struct-valued property, not four independent ones. A per-side rule
(`ml-4`) has to set the *whole* struct, zeroing the sides it isn't targeting. Two
separate utilities on the same element (`ml-1 mr-2`) therefore don't combine — they
resolve to two independent `Style`s, each setting the whole `Margin`, and Avalonia's
styling system lets the later one win outright rather than merging them.

**Ghost properties (`SpecialProperties.cs`).** A per-side rule can instead target a
*ghost* `AvaloniaProperty` — a real, registered `AvaloniaProperty<TValue>` (so it
still fits `StyleValue.Literal<TValue>` unchanged), but owned by a type
(`SpecialProperties : StyledElement`; it has to derive from `StyledElement` rather
than plain `AvaloniaObject` purely because `Selectors.Is<T>()` requires it — no
instance of the type is ever created) that lives only in this generator project and
is never referenced by the consuming/output project. `SpecialProperties` currently
covers `Margin*`, `Padding*`, `BorderThickness*` (registered but not yet consumed by
any rule — there is no per-side border-width utility today, only the uniform
`border-*`) and `CornerRadius*` (note its slot order is TopLeft/TopRight/
BottomRight/BottomLeft, following `CornerRadius`'s own constructor, unlike the
Left/Top/Right/Bottom order the `Thickness`-valued ones use). `MarginRule`,
`PaddingRule` and `RoundedRule`'s non-uniform branches target these instead of
building a zeroed struct on the real composite property directly; each rule's
uniform branch (`m-4`, `p-4`, `rounded-*` with no side) keeps targeting the real
property, since there's nothing to combine there. `GhostProperties.Map` is the
(hardcoded, not user-extensible) registry saying which real property, which of its
four slots, and how to assemble a full group of slots into that property's
`StyleValue` — the `Build` delegate is what lets the combiner below stay generic over
both `Thickness`- and `CornerRadius`-valued composites.

**The combiner (`GhostPropertyCombiner<TTheme>`).** A `Post`-stage
`ISourceTransformer<TTheme>`: for each source line, it resolves every candidate token
on that line (`generator.ParseToken`, reusing `SplitExtractor`'s tokenization) and
collects the ones whose resolved `Setter`s target a registered ghost property.
Grouped by target real property, it assembles the combined value and appends a
**synthesized token** to the line (e.g. `ml-1 mr-2` → `ml-1 mr-2 __ghost_ml-1_mr-2__`)
together with a matching `Rule.Static` registered on the fly into
`AtomicConfiguration.Rules`, carrying the real `Setter(MarginProperty, ...)` —
alongside, not instead of, the original tokens. A single ghost token with no sibling
on the same line still gets its own synthesized token (a "group" of one), so it falls
back to its own style with the other slots at 0, same as before ghost properties
existed. Same-line co-occurrence is a deliberately cheap heuristic, not a real
XAML/AST-aware analysis — good enough for the common `Classes="ml-1 mr-2"` case, not
meant to be exhaustive. A style whose owner type is `SpecialProperties` can never
match a real element (no real control derives from it) — resolving one doesn't throw
(`ParseToken` still returns it, which is what lets the combiner see it in the first
place), but `AtomicGenerator.Generate(ISet<string>, Options)` drops it at the final
emission boundary, so an *uncombined* ghost token never reaches the output on its
own.

Note this ended up simpler than first drafted: no actual compound multi-class
selector (`:is(Layoutable).ml-1.mr-2`) was needed — a single synthesized class name,
added only when its ghost members actually co-occur, achieves the same effect within
the existing "one token → one selector" resolution model.

**Runtime vs. build-time implication.** For a combined style to ever match a live
element, the element's actual `Classes` must contain the synthesized token — so this
only works if the *transformed* text becomes the real source of truth (the on-disk
`.axaml` the app compiles), not a throwaway scratch copy used only for token
scanning. That fits the [planned build-time codegen CLI](#build-time-c-code-generation)
below, which reads and is meant to rewrite real files; it does not fit bolting this
onto an already-compiled, already-loaded XAML tree at pure runtime.

## Build-time C# code generation

The goal is to move this resolution to the **build** of the consuming application,
producing a static `.g.cs` file (a `Styles` class) instead of paying the
resolution/expression-compilation cost on every startup.

**Implemented: the emitter (`Sources/Enx.Atomic.Avalonia.CodeGen`).** `StyleEmitter.Emit(
utils, namespaceName, className, containerName)` turns a `StringifiedUtil[]` (e.g.
straight from `AtomicGenerator<TTheme>.Generate(...)`) into a full `.cs` file text — a
`Styles` subclass constructing each `Style` and its `Setter`s directly. It works
entirely off data:
- **`StringifiedUtil.SelectorData`/`ContainerQueryData`** — added alongside the
  existing compiled `Selector`/`ContainerQuery` `Expression`s specifically for this.
  `SelectorEmitter`/`StyleQueryEmitter` pattern-match on the actual
  `SelectorExpression`/`StyleQueryExpression` record types (`Is`, `Class`, `OfType`,
  `PropertyEquals`, `Width`, `Height`, `Or`, `And`) and recurse over `.Previous`,
  producing text like `selector.Is<Button>().Class("hover:bg-red-500")` — **never**
  compiling an `Expression` and decompiling the result (see History below for why that
  distinction matters). `Or`/`And` are real static factory calls in Avalonia
  (`StyleQueries.Or(params StyleQuery[])`), not fluent extensions like the others —
  worth remembering if extending this, since it's easy to assume otherwise.
- **`Values/IValueEmitter`** — one emitter per value shape a `Setter` or
  `PropertyEquals` can carry: `PrimitiveValueEmitter` (bool/numeric/string),
  `EnumValueEmitter` (generic over any enum type — not one per enum), `Thickness`/
  `CornerRadiusValueEmitter`, `BrushValueEmitter` (any `ISolidColorBrush`),
  `CursorValueEmitter` (reconstructs `new Cursor(StandardCursorType.X)` from
  `Cursor.ToString()`, since `Cursor` doesn't expose which `StandardCursorType` it was
  built from — only supports that constructor, not bitmap cursors), and
  `TextDecorationsValueEmitter` (only the three named `TextDecorations.*` constants,
  matched by reference equality). No intermediate `var brushXxx = ...;` declarations
  (unlike the old architecture) — every value is inlined as an expression directly in
  the `Setter` call, so there's no variable-name collision to avoid in the first place.
- **`AvaloniaPropertyNaming`** — resolves an `AvaloniaProperty` back to its declaring
  static field's name (`"Button.IsPressedProperty"`) by reflection over
  `OwnerType.GetFields(Public | Static)`, fixing the old architecture's missing
  `Public` flag (a real bug there, not just noise).

Verified by `Tests/.../CodeGenTests.cs`, which is the strongest check available for an
emitter: it actually **compiles** the emitted text with Roslyn
(`Microsoft.CodeAnalysis.CSharp`, test-only dependency) and asserts no errors.
`Sandbox/Program.cs` prints a real example.

**Not yet implemented — the CLI + MSBuild wiring:**

1. **The user's configuration project becomes an executable.** It references
   `Enx.Atomic.Avalonia`(`.Preset.Mini`)`.CodeGen`, builds its
   `AtomicConfiguration<TTheme>` in C#, and exposes a CLI entry point
   (`RunCli(string[] args)`) that: reads the source file paths (XAML + C#) passed as
   arguments, runs them through the existing `Extractor`s to pull out tokens, calls
   `Generate()`, then calls `StyleEmitter.Emit`.
2. **An MSBuild `.targets`** adds a `ProjectReference` from the consuming project to
   the configuration project (guarantees build order), then runs that CLI via
   `<Exec>` (no custom C# MSBuild task — a plain `Exec` is enough) with the consuming
   project's `@(Compile)` and `@(AvaloniaXaml)` items, and a user-facing property
   `EnxAtomicStylesOutputPath` (sensible default, e.g.
   `$(IntermediateOutputPath)GenStyles.g.cs`) as output — also used as the target's
   `Outputs` for incrementality. The generated file is added back as
   `<Compile Include="$(EnxAtomicStylesOutputPath)" />`.

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
