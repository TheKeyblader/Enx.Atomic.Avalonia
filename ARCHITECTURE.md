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
- **`ISourceTransformer<TTheme>`** (`Transformers/`) — sees a source file's *full
  text* before extraction, ported from UnoCSS's `SourceCodeTransformer`. Unlike
  `IPreProcessor`, which only ever sees one already-isolated token, a transformer sees
  everything at once. Its `Transform` returns the (possibly rewritten) text — the tool
  for genuine text rewrites, e.g. a future port of UnoCSS's variant-group expansion
  (`hover:(bg-red-500 text-white)` → `hover:bg-red-500 hover:text-white`) — but it
  doesn't have to rewrite anything: [ghost-property
  combining](#source-transformers-and-ghost-properties) below returns its input
  unchanged and instead calls `AtomicGenerator.AddUtil` as a side effect, for a style
  that isn't derived from any single matched token. Transformers are grouped by
  `Enforce` (`Pre` → `Default` → `Post`) and run in that order, declaration order
  within a group, each seeing the previous one's
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
`GhostPropertyCombiner`, and the per-side/corner branches of `MarginRule`,
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

**The combiner (`GhostPropertyCombiner<TTheme>`, a `Post`-stage
`ISourceTransformer<TTheme>`, auto-registered by `AddMiniTheme`).** Its `Transform`
always returns the source text **unchanged** — it only ever *reads* it, never
rewrites it. Two things are easy to conflate here and worth being precise about:
transforming the in-memory string a transformer receives is completely fine and
doesn't touch anything on disk (nothing in this engine ever writes a file); what
actually matters is that the *selector* the combiner emits must only ever require
classes that genuinely exist in that real, untouched source — never a name invented
along the way. An earlier draft got this backwards: it rewrote the in-memory text to
inject a synthesized class name and built the combined style around *that* name —
which would only ever match a live control if that synthesized name were also
somehow present on it, i.e. only if the real file got rewritten too. The fix isn't
"don't transform text" (transforming text in memory is exactly what
`ISourceTransformer` is for) — it's "build the selector from the real class names
that were actually there."

Concretely: for each source line, `Transform` resolves every candidate token on that
line (`generator.ParseToken`, reusing `SplitExtractor`'s tokenization) and collects
the ones whose resolved `Setter`s target a registered ghost property. Grouped by
target real property, it assembles the combined value and calls
`AtomicGenerator<TTheme>.AddUtil` with an extra `StringifiedUtil` carrying a
**compound selector** requiring every contributing class — `.Class("ml-1")
.Class("mr-2")`, i.e. `:is(Layoutable).ml-1.mr-2` — and the real
`Setter(MarginProperty, ...)`. `AddUtil` exists precisely because `Transform`'s
return type is `string`, with no way to also hand back extra resolved styles; a
transformer that needs to (this one does, since one compound style doesn't
correspond to any single matched token) registers them as a side effect instead,
and `Generate(string, Options)` folds them into its own result automatically — the
caller never needs to know this happened. `ml-1`/`mr-2` still individually resolve
to a `SpecialProperties`-scoped style on their own, dropped at `Generate`'s emission
boundary same as always (see below) — only the compound one reaches output. A single
ghost token with no sibling on the same line still yields a style — a "group" of
one, compound with just that one already-existing class — which is what makes a
ghost property usable on its own at all, other slots defaulting to 0, same as before
ghost properties existed. Same-line co-occurrence is a deliberately cheap heuristic,
not a real XAML/AST-aware analysis — good enough for the common
`Classes="ml-1 mr-2"` case, not meant to be exhaustive. A style whose owner type is
`SpecialProperties` can never match a real element (no real control derives from
it) — resolving one doesn't throw (`ParseToken` still returns it, which is what lets
the combiner see it in the first place), but
`AtomicGenerator.Generate(ISet<string>, Options)` drops it at the final emission
boundary, so an *uncombined* ghost token never reaches that call's output on its own.

This is why the whole mechanism has no runtime-vs-build-time caveat: a compound
selector only ever depends on classes the real source already has, so it works the
same whether resolved at app runtime or by the [build-time codegen
emitter](#build-time-c-code-generation) below — nothing needs to be written back to
any file, ever.

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

**Implemented: the CLI + MSBuild wiring (`Sources/Enx.Atomic.Avalonia.CodeGen/AtomicCli.cs`,
`Sources/Enx.Atomic.Avalonia.CodeGen/build/Enx.Atomic.Avalonia.CodeGen.targets`).**

1. **The user's configuration project is an executable** that references
   `Enx.Atomic.Avalonia`(`.Preset.Mini`)`.CodeGen`, builds its
   `AtomicConfiguration<TTheme>` in C#, and calls `AtomicCli.Run(args, configuration)`
   from `Main`. `AtomicCli` defines its `--output`/`--namespace`/`--class`/`--container`/
   `<SOURCES>` contract as a Spectre.Console.Cli `Command<AtomicCliSettings>`
   (`GenerateCommand<TTheme>`) rather than hand-rolled parsing — real `--help`, option
   validation (`--output` is required), and error messages for free. Since
   `CommandApp<TDefaultCommand>` builds the command via a plain parameterless
   `Activator.CreateInstance` (no constructor injection without a custom
   `ITypeRegistrar`, which isn't worth the complexity for a single generic dependency),
   the `AtomicConfiguration<TTheme>` is handed to `GenerateCommand<TTheme>` through a
   static property set immediately before `app.Run(args)` — safe because this is always
   a single, synchronous, single-shot invocation. `Execute` runs each source file
   through `AtomicGenerator<TTheme>.Generate(content, options)` to extract and resolve
   tokens, then calls `StyleEmitter.Emit` and writes the result. See
   `Examples/Enx.Atomic.Avalonia.Example.Config` for a minimal config project — it
   bootstraps Avalonia's headless platform before touching `AddMiniTheme` because some
   static rules (e.g. `Cursors`) construct real Avalonia types at static-init time and
   need `AvaloniaLocator` populated.
2. **The MSBuild `.targets`** does *not* use a `ProjectReference` (that would force a
   plain build-order dependency the consuming project doesn't otherwise need); instead
   it invokes `<MSBuild Projects="$(EnxAtomicConfigProject)" Targets="Build">` directly
   inside the generation target to build the configuration project and resolve its
   output assembly via the `TargetOutputs` output parameter, then runs it with
   `<Exec Command="dotnet exec ...">` against the consuming project's `@(Compile)` and
   `@(AvaloniaXaml)` items. Output path defaults to
   `$(MSBuildProjectDirectory)/GeneratedStyles/GenStyles.g.cs` — deliberately next to the
   project file rather than under `obj/`, so the emitted code is easy for a user to find
   and read, not just compiled invisibly — overridable via `EnxAtomicStylesOutputPath`.
   `GeneratedStyles/` is gitignored (build output, regenerated every build). The target
   runs `BeforeTargets="CoreCompile"`, with `Inputs`/`Outputs` for incrementality, and
   adds the generated file back via `<Compile Include="..." />` inside the same target
   so it's picked up for this same compilation pass regardless of whether the target
   itself was skipped as up to date (a plain `ItemGroup` that's a direct child of a
   `Target`, as opposed to a task's captured output, runs every time the target is
   *evaluated* even when its tasks are skipped — this is what makes the `Compile
   Include` reliable across incremental builds without needing the target's `Exec` to
   actually run). Because the output now lives under the project directory instead of
   `obj/`, the SDK's own default `Compile` glob would otherwise pick it up too once it
   exists — as a *static*, top-level `<ItemGroup><Compile Remove="$(EnxAtomicStylesOutputPath)" /></ItemGroup>`
   (outside the target) excludes it; a `Remove` attempted *inside* the target instead
   only matches an item's `Identity` as a literal string, not the equivalent
   relative/absolute form the default glob used, so it silently failed to match there,
   while the static, project-load-time form does full path normalization and works.
   Left unexcluded this was a real correctness bug, not just noise: the generated file
   got fed back into `EnxAtomicGenerateStyles`'s own list of source files to scan,
   re-extracting its own emitted `.Class("...")` calls as utility tokens and doubling
   the output on every subsequent build (10 styles → 20), on top of a `CS2002` duplicate
   source warning from `csc`. All of `EnxAtomicStylesOutputPath`/`EnxAtomicNamespace`/
   `EnxAtomicClassName`/`EnxAtomicContainerName`'s defaults are computed in a plain
   top-level `PropertyGroup` — safe here since, unlike `IntermediateOutputPath` (see
   git history for an earlier version of this file that depended on it — its
   TargetFramework-specific suffix isn't appended until targets start executing),
   `MSBuildProjectDirectory` is fully resolved at plain project-load time. A consuming
   project opts in by setting `EnxAtomicConfigProject` to the config project's `.csproj`
   path and importing the `.targets` file explicitly (not auto-imported — not
   packaged/shipped via NuGet, so there's no `build/{PackageId}.targets` convention to
   rely on). See
   `Examples/Enx.Atomic.Avalonia.Example.App` for a working end-to-end example.

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
