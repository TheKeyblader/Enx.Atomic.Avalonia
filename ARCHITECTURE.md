# Architecture

Enx.Atomic.Avalonia is an atomic utility-class engine for Avalonia, inspired by
[UnoCSS](https://unocss.dev/) — the vocabulary (`Rule`, `Variant`, `Extractor`,
`Theme`) and the token-resolution logic (`hover:bg-red-500`, `sm:flex-row`, ...) are
adapted from it to Avalonia properties and selectors instead of CSS.

## Projects

| Project | Role |
|---|---|
| `Sources/Enx.Atomic.Avalonia` | The generic engine: resolves tokens into styles, independent of any preset. |
| `Sources/Enx.Atomic.Avalonia.Preset.Mini` | A concrete rule/variant set, inspired by `@unocss/preset-mini`. |
| `Sources/Enx.Atomic.Avalonia.CodeGen` | Build-time C# generation: emitter, CLI, and MSBuild wiring. |
| `Sandbox` | An Avalonia console app used as a manual test bed for the engine and the presets. |
| `Examples/Enx.Atomic.Avalonia.Example.Config` | A minimal configuration project — the pattern a consumer's own config project follows. |
| `Examples/Enx.Atomic.Avalonia.Example.App` | A minimal Avalonia app wired to build-time codegen via the Config project above. |
| `Examples/Enx.Atomic.Avalonia.Example.TodoApp` | A small but complete app (a todo list) styled almost entirely with utility classes, conditional `Classes.name="{Binding ...}"` classes included — same wiring, same Config project. |
| `Tests/Enx.Atomic.Avalonia.Tests` | The automated test suite (xUnit v3). |

## How a token becomes a style

A string like `hover:bg-red-500` goes through four steps:

1. **Extraction** — an `Extractor` scans source text and pulls out candidate tokens
   (e.g. every word inside `Classes="..."`, or the `line-through` of a conditional
   `Classes.line-through="{Binding IsDone}"`).
2. **Variant matching** — a `VariantBase<TTheme>` strips a recognized prefix/suffix
   (`hover:`) and remembers how it should transform the final selector or container
   query. This repeats until no variant matches, which is what makes chaining
   (`hover:focus:...`) work.
   If what's left is a **shortcut** (`btn` → `bg-blue-500 px-4`), it is expanded here,
   before any rule is tried, and each expanded token goes through steps 2–3 itself —
   see `IShortcut<TTheme>` below.
3. **Rule matching** — an `IRule` matches what's left of the token (`bg-red-500`)
   against either a fixed name (`IStaticRule`) or a regex (`IDynamicRule<TTheme>`),
   and produces one or more `StyleValue`s (a `Setter` value, fixed or resolved from
   the theme).
4. **Emission** — the resolved selector, container query, and `StyleValue`s become a
   `StringifiedUtil`, which either becomes a real `Avalonia.Styling.Style` at runtime,
   or gets handed to the code generator to emit as C# (see below). Either way the whole
   set is put in a canonical order first — see "Emission order" below.

## Core engine (`Sources/Enx.Atomic.Avalonia`)

- **`IRule`** — `IStaticRule` (`Rule.Static`) matches a token by exact name and
  carries a fixed set of `StyleValue`s. `IDynamicRule<TTheme>` matches by regex and
  computes its `StyleValue`s from the match and the theme (`RuleContext<TTheme>`).
  When several rules could match, a static rule always wins over dynamic ones; among
  dynamic rules, the first match in `Rules` declaration order wins — this is how two
  rules can share a prefix (`border-2` as width vs `border-red-500` as color) without
  one convoluted regex.
  `IOrderRankedDynamicRule<TTheme>` is the optional companion for a rule that
  consolidates several of those entries into one regex (`m-*`/`mx-*`/`ml-*`): its
  `GetOrderRank` reports the relative order of its own matches, which `StyleOrdering`
  consumes right after the rule index (see "Emission order" below).
- **`StyleValue`** — `Literal<TValue>` (value fixed at resolution time) or `Resource`
  (a `DynamicResource` lookup under `Key`, so the value tracks a theme/resource change
  at runtime instead of being baked in). A rule builds one from `ThemeAccess`, the
  theme-scale expression it read to produce it (e.g. `t => t.Colors[value]`, via
  `AvaloniaExtensions.ToResource`) — kept as an `Expression` tree, never
  compiled+decompiled, so downstream code can compile and invoke it once against a
  real `TTheme` to learn the actual value (see `ResourceDictionaryEmitter` below).
  `Key` itself is never passed in — it's derived automatically from `ThemeAccess` by
  `ThemeResourceKey.From` (walking member/indexer access nodes as data, evaluating an
  indexer's argument), so two rules reading the same theme entry (e.g. `bg-red-500`'s
  `Border`/`TemplatedControl`/`Panel` triple) always agree on one key, and two rules
  reading different entries can never collide on a hand-typed one.
- **`Themed<T>`/`IThemedValue`** — a theme scale entry (e.g.
  `IColorPart.Colors : Dictionary<string, Themed<IBrush>>`) that may differ between
  `ThemeVariant.Light`/`Dark`. Implicitly convertible from a plain `T`, so seeding a
  scale with one value per key (`theme.Colors[key] = someBrush`) still compiles — only
  a value built via the two-argument constructor (`new Themed<T>(light, dark)`) is
  `IsThemed`. That flag is what decides, for a resolved `Resource`, whether its value
  lands in the plain resource dictionary or gets split across
  `ResourceDictionary.ThemeDictionaries[Light]`/`[Dark]`.
- **`VariantBase<TTheme>`** — matches a token prefix/suffix and produces a
  `VariantHandlerBase` that transforms the resolved style's selector or container
  query.
- **`SelectorExpression` / `StyleQueryExpression`** (`Compact/`) — a plain data
  representation of an `Avalonia.Styling.Selector` / `StyleQuery`, built by rules and
  variants without depending on `System.Linq.Expressions`. Only converted to a
  compiled `Expression<Func<Selector, Selector>>` at the very end
  (`AtomicGenerator.ApplyVariants`) — this same data representation is also what the
  build-time emitter reads directly, instead of ever decompiling a compiled
  expression.
- **`ISourceTransformer<TTheme>`** (`Transformers/`) — sees a source file's *full
  text* before extraction (unlike `IPreProcessor`, which only sees one already
  isolated token). Its `Transform` returns the text unchanged or rewritten. Grouped by
  `Enforce` (`Pre` → `Default` → `Post`) and run in that order
  (`AtomicGenerator.ApplyTransformers`), always before extraction. A transformer that
  needs to emit a style no single token corresponds to registers it through
  `AtomicGenerator.AddUtil` as a side effect; `PendingUtils` exposes what the most
  recent `ApplyTransformers` call registered, and `Generate` folds those utilities into
  its own output.
- **`Extractor`** (`Extractors/`) — pulls candidate tokens out of a source's text; a
  candidate no rule matches is simply dropped downstream, so extractors over-collect
  rather than parse. Two run by default. `SplitExtractor` splits on whitespace, quotes,
  `;` and `{}`, which covers `Classes="..."` and any quoted string in code-behind
  (`Classes.Set("dark:underline", v)`). It deliberately doesn't split on `.` or `=`
  (`p-0.5` would break), so Avalonia's conditional class syntax,
  `Classes.line-through="{Binding IsDone}"`, reaches it as one unmatchable candidate —
  `ClassesAttributeExtractor` covers that form, capturing the name after `Classes.`,
  which is exactly the class the XAML compiler toggles (`Classes.Set(name, value)`).
  That name is an XML attribute name, so it can't carry a variant (`:` is a namespace
  prefix), an arbitrary `[...]` value or a `/`; a name containing `.` isn't matched.
  Those cases go through code-behind instead. A configuration replacing `Extractors`
  should keep both.
- **`IShortcut<TTheme>`** (`Shortcuts/`) — UnoCSS's shortcuts: a token that expands to
  other utility tokens. `Shortcut.Static("done", "line-through opacity-45")` matches by
  exact name; `Shortcut.Dynamic<TTheme>(regex, (match, context) => ...)` computes the
  expansion from a regex match and the theme (returning `null`/nothing is a miss, so
  the next shortcut is tried). Expansions are whitespace-split, trimmed and
  deduplicated. `IShortcut<in TTheme>` is contravariant and `IStaticShortcut` is an
  `IShortcut<object>`, which is what keeps `Shortcut.Static` non-generic while
  `Shortcuts` stays a `List<IShortcut<TTheme>>`. Resolution, in `ParseToken`:
  - **Shortcuts are tried before rules**, in list order, on the variant-stripped token,
    so a shortcut named like a rule wins.
  - Each expanded token gets its own variant matching; one that is itself a shortcut is
    expanded recursively (inner handlers combined with the enclosing ones, so a shortcut
    may contain `hover:otherShortcut`), up to 5 nested levels — beyond that it is taken
    for a cycle and throws `InvalidOperationException` naming the chain.
  - Every resulting style selects on the **token the user wrote**:
    `Is(TargetType).Class("hover:btn")`, never the inner `bg-blue-500`, with the inner
    and outer variant handlers both applied. The inner tokens are neither cached nor
    emitted under their own names. Leaves are deduplicated on their rewritten form
    (`hover:opacity-50`), their matched base token and their handler pipeline, not on the
    bare base token: two leaves differing only by variant both survive, and so do the
    branches a variant forks for the same token (two handlers for one `v:left`). Variant
    matches are cached per token for the expansion, so a token really repeated
    (`v:left v:left`, or reached again through a nested shortcut) still collapses.
  - **No setter merging**, unlike UnoCSS, which folds a shortcut into one CSS rule: each
    inner utility stays its own `StringifiedUtil` with its rule's index and rank. That
    keeps the per-rule order (`p-2 px-4` inside a shortcut still composes uniform before
    axis) and never puts two setters for the same property in one `Style`.
  - Every shortcut style carries `StyleLayer.Shortcuts` (see "Emission order" below),
    so a plain utility on the same element overrides it.
  - An expanded token nothing matches is a configuration typo, not a fatal error: it is
    recorded in `AtomicGenerator.Diagnostics` and skipped, and the rest of the shortcut
    still applies. `GetShortcutExpansion(raw)` returns the leaf tokens a shortcut
    resolved to, for `probe`.

  Because the expansion happens inside the engine, a shortcut works everywhere a token
  does — conditional `Classes.done="{Binding IsDone}"` included, which is the way to get
  a variant (`dark:`, `hover:`) under a binding-toggled class, since the XML attribute
  name itself can't carry one.
- **`AtomicConfiguration<TTheme>`** — assembles `Transformers`, `PreProcessors`,
  `Rules`, `Shortcuts`, `Variants`, `Extractors`, and the `TTheme` instance.
- **`AtomicGenerator<TTheme>`** — the engine itself. `Generate(...)` runs the full
  pipeline above and returns `StringifiedUtil[]`, cached by raw token, in
  `StyleOrdering`'s canonical emission order (see "Emission order" below). Also
  accumulates every distinct `StyleValue.Resource` encountered into
  `ResolvedResources` (keyed by `Key`), which `ResourceDictionaryEmitter` reads to
  build the actual resource dictionary those resources' `DynamicResource` lookups
  resolve against.

## `Enx.Atomic.Avalonia.Preset.Mini`

- **`Parts/`** — one interface per theme scale (`ISpacingPart.Spacing`,
  `IColorPart.Colors`, `IRadiusPart.Radii`, `ISizePart.Sizes`,
  `ILineWidthPart.LineWidths`, `IFontSizePart.FontSizes`, `IFontFamilyPart.FontFamilies`,
  `IBoxShadowPart.BoxShadows`, `IBreakpointPart.Breakpoints`, `IRemToPxPart.RemToPxFactor`).
  Numeric scales and the rem factor are `double` — the CLR type of every Avalonia length
  property — so a resolved value can be boxed into a `Setter` without a widening step.
  Each dynamic rule requires, via `where TTheme : ...`, only the parts it actually needs
  — a user theme can implement any subset of them.
- **`Static/`** — static rules, independent of any theme (`hidden`, `cursor-pointer`,
  `flex-row`, ...).
- **`Dynamic/`** — theme-driven rules. Directional families (margin/padding/gap,
  corner-radius) are consolidated into a single class per family via named capture
  groups (`side`, `axis`, `bound`, `neg`) instead of one class per direction — e.g.
  `MarginRule` handles `m-*`, `mx-*`, `mt-*`, ... in a single `Match`. Because one
  consolidated rule has a single declaration index, these rules implement
  `IOrderRankedDynamicRule<TTheme>` and rank their matches (`uniform < axis/edge <
  side/corner`) instead of relying on UnoCSS's separate rule entries to encode that
  order; `StringifiedUtil.Rank` carries it to `StyleOrdering`.
  - **`ThemeScale`** — resolves a theme key, falling back, in order, to an
    `ArbitraryValue` then a bare number. `TryResolve` treats the bare-number fallback
    as **quarter-rems**, like UnoCSS's `rem` handler (`n` → `n / 4` rem, so `w-4` =
    1rem = 16px, then px via `RemToPxFactor`) — the behavior of most UnoCSS scales
    (`spacing`, `borderRadius`, ...), and consistent with the spacing scale's own
    numeric keys. `TryResolvePx` treats it as a **raw px**
    value, no conversion — UnoCSS's `lineWidth` scale behavior (border/ring/outline).
    `TryResolveUnscaled` keeps only the arbitrary-px and bare-number fallbacks, for a family
    whose upstream scale has no portable entries at all (`leading-*`'s unitless
    multipliers, `tracking-*`'s `em` values — both absolute pixels in Avalonia).
  - **`ArbitraryValue`** — UnoCSS/Tailwind's `{prefix}-[{value}]` bracket syntax,
    bypassing the theme scale entirely (`w-[123px]`, `bg-[#ff0000]`). `TryUnwrap`
    strips the brackets (underscore→space, since a class token can't contain a literal
    space); every `ThemeScale` entry point checks it *first*, before the scale
    dictionary or the bare-number fallback, and — when matched — parses the content
    directly as pixels (an optional `px` suffix, any other unit rejected), never
    rem-converted. Colors don't go through `ThemeScale`, so `ColorRules.cs` has its own
    `ArbitraryColor` helper (`Color.TryParse` on the unwrapped content) shared by
    `bg-*`/`text-*`/`border-*`. Either way the result is always a `StyleValue.Literal`,
    never a `StyleValue.Resource` — an arbitrary value has no named theme member for a
    `ThemeAccess` expression to point at.
- **`Variants/`** — `PseudoClassVariant` (maps `hover:`, `disabled:`, ... onto real
  Avalonia pseudo-classes such as `:pointerover`) and `BreakpointVariant` (maps
  `sm:` / `max-sm:` onto container queries).
- **`MiniTheme`** — the default theme implementation aggregating every part.
- **`Extensions/`** — `DefaultTheme` holds the default scales (ported from
  `preset-mini/src/_theme/*.ts`, rem→px at 16px/rem, full Tailwind palette).
  `ThemeBuilderExtensions` exposes one `Add*Rules(builder, configuration)` method per
  part, seeding the theme's dictionary and registering the matching rules/variants.
  `AddMiniTheme` chains all of them in the order that matters — rules with a more
  specific prefix (`BorderWidthRule`, `FontSizeRule`) are registered before rules
  sharing their generic prefix (`BorderColorRule`, `ForegroundColorRule`).

## Emission order: the cascade Avalonia doesn't have

Avalonia has no CSS specificity. The value that survives is decided by priority first,
frame order second: a style whose selector carries an *activator* is framed at
`BindingPriority.StyleTrigger` (`StyleInstance.GetPriority`), and a plain class selector
**is** an activator. So `.w-4` and `.hover:w-8:pointerover` — or `.w-4` and a
`min-width:640` container query holding `.sm:w-8` — always tie, and `ValueStore`'s frame
insertion plus `ReevaluateEffectiveValues` resolve the tie in favour of the **last
inserted** frame. Emitted in source-token order, `hover:w-8 w-4` would therefore
silently lose its hover state: the base utility, resolved later, wins at equal
priority.

`StyleOrdering.Sort` (`StyleOrdering.cs`) is the single place that decides that order,
applied by `AtomicGenerator.Generate(...)` for the runtime path and by `AtomicCli` over
the combined set of every source file for the build-time path, so both produce the
same cascade. Its key, ascending:

1. **Specificity** — class-like nodes (`Class`, including Avalonia pseudo-classes whose
   name starts with `:`, and `PropertyEquals`, e.g. `dark:`) as CSS's
   class/attribute/pseudo-class column, then type nodes (`Is`/`OfType`) as its lower
   type column. `.hover\:bg-red-500:pointerover` (2) therefore beats
   `.sm\:bg-red-500` (1) regardless of where its container query falls, exactly as CSS
   compares specificity before the at-rule's position.
2. **Layer** (`StringifiedUtil.Layer`) — styles expanded from a shortcut
   (`StyleLayer.Shortcuts`, 0) before plain utilities (`StyleLayer.Default`, 1, also the
   default for a util built outside token resolution), the equivalent of UnoCSS emitting
   its `shortcuts` layer before `default`. Placed after specificity and before container
   order, as a CSS block emitted earlier would be: `class="btn bg-red-500"` is red even
   when `btn` contains `sm:bg-blue-500`, while `hover:btn` (two class-like nodes) still
   beats a plain `bg-red-500`.
3. **Container order** — unconditional utilities first, then `max-*` upper bounds
   (narrowest last, since the narrowest is the most specific), then the
   `sm:`/`md:`/... lower bounds ascending by breakpoint, mirroring ascending `@media`
   blocks where the later, wider block wins. An unrecognized query node sorts after
   unconditional utilities, so a future variant can't silently stop overriding its
   base.
4. **Rule index** (`StringifiedUtil.Index`) — the originating rule's position in this
   generator's `Configuration.Rules`. It is looked up per generator, never stored on the
   rule: Preset.Mini shares rule instances across configurations, so an index written on
   the instance would be overwritten by the next generator built from it.
5. **Rule rank** (`StringifiedUtil.Rank`) — an `IOrderRankedDynamicRule<TTheme>`'s
   ordering of its own matches.
6. **Variant sort hint** (`StringifiedUtil.Sort`) — UnoCSS's per-rule `sort`; no variant
   in the Mini preset sets it today.
7. **Selector text** — a culture-independent description, so otherwise identical styles
   still come out deterministic.

The practical consequences: source order never matters, so a variant may be written
before its base; a wider breakpoint wins over a narrower one when both match; a more
specific selector outranks any container query; and a plain utility overrides an
equally specific shortcut.

`StyleOrdering.GetSortKey` exposes that key (`StyleOrdering.SortKey`, one field per
step above) for diagnostics — `AtomicCli probe` prints it next to each style so a
surprising position can be explained without re-deriving the comparison by hand.

`StyleOrdering.Distinct` is the companion used on the build-time path only: transformer
output registered through `AddUtil` has no token to deduplicate by, so it is compared
structurally (selector data, container query, setter property instance and value) and
collapsed. Two distinct resources are never merged on a guess, since reference-typed
values compare equal only when they are the same instance. The layer is part of that
identity: a shortcut's copy of a style sorts differently from a plain utility's.

## Composite side properties: per-side utilities

Avalonia's `Margin`, `Padding`, `BorderThickness`, and `CornerRadius` are each a single
struct-valued property, not four independent ones. Two utilities on the same element
(`ml-1 mr-2`) would normally resolve to two separate `Style`s each setting the *whole*
struct — and, both framed at `StyleTrigger`, the later one would win outright rather
than merging them.

The engine decomposes each of those properties into four independent `double` sides:

- **The hosts (`Composites/`)** — `MarginSides`, `PaddingSides`,
  `BorderThicknessSides` and `CornerRadiusSides` each declare four
  `AttachedProperty<double>` fields on `Control` (`NaN` meaning "this side is unset"), a
  `CompositePropertyDescriptor` describing them, and a static constructor that
  registers the class handler through `CompositeProperty.Register`. Targeting `Control`
  is deliberate: it is the widest honest target, since `Padding`/`BorderThickness`/
  `CornerRadius` are `AddOwner`'ed by
  `TemplatedControl` (and `ContentPresenter`; `TextBlock` too for `Padding`), so a
  `Button` receives them — while a control such as `Canvas` never registers `Padding` at
  all. The explicit static constructor is load-bearing: it makes the type not
  `beforefieldinit`, so the handlers are always registered before the first side
  property can be read.
- **`CompositePropertyHandler<TValue>`** (internal) — the shared runtime implementation
  behind every host. When any side changes it recomposes the real
  `Thickness`/`CornerRadius` and writes it at `BindingPriority.Style`. `Style`, not
  `StyleTrigger`, because the composed value is a *derived* value: at `StyleTrigger` it
  would outrank the class-selected styles that feed it, whereas at `Style` it loses to a
  local value and to a user style whose selector carries an activator (the expected
  outcome) while still beating the theme/template default. The write is set-then-dispose:
  `SetValue` with a non-local priority returns the inserted frame's `IDisposable`, and
  the previous frame is disposed *after* the new one is inserted, so the composed value
  never transiently falls back to the theme. When all four sides are unset the
  contribution is withdrawn entirely, so the real property returns to its theme/default
  value. A control that doesn't register the real property
  (`AvaloniaPropertyRegistry.IsRegistered`) is skipped.
- **Rules** — `MarginRule`, `PaddingRule`, `BorderWidthRule` and `RoundedRule` write
  only side properties, the uniform token included (`m-4` writes all four sides), and
  pass `typeof(Control)` explicitly. Because the sides are ordinary attached properties,
  variants (each stays its own `Style`), container queries, class lists split across
  lines, and overlapping utilities all resolve through Avalonia's own cascade instead of
  a build-time merge. UnoCSS's direction order (uniform < axis/edge < side/corner) is
  preserved by `IOrderRankedDynamicRule<TTheme>` + `StyleOrdering` rather than by
  separate rule entries.
- **Codegen** — the consuming project may not reference the preset assembly at all, so
  `StyleEmitter` embeds a `file`-local copy of each host a stylesheet actually targets.
  `CompositeHostSourceEmitter` generates that copy from the host's
  `CompositePropertyDescriptor` (four fields plus the class handler), and one shared
  `CompositeSideRecomposition` helper is emitted once per file. Because the runtime
  hosts and the embedded copies are maintained separately, a shared scenario suite
  (`CompositeScenarioTests`) runs every scenario against both, including the
  Roslyn-compiled copy.

Known limitation: **an unset side composes as `0`**, not "leave it unchanged". A lone
`px-4` on a control whose theme gives `Padding` a non-zero vertical value therefore
zeroes the vertical sides, because the composed struct is built from the four sides
alone. Keeping the underlying value per slot was tried and doesn't work. It would mean
reading the value the contribution replaces when the contribution starts. In the normal
case, though, the class is already present when the control is styled, and Avalonia
then applies the theme and the utility styles in **one** styling pass:
`ValueStore.ReevaluateEffectiveValues` walks frames from the highest priority down and
raises each change immediately, so the side's change (a `StyleTrigger` class style)
fires before the theme's or a plain style's `Padding` has been applied, and the value
read is the default. With that approach, only a class added after styling saw the theme value.
Deferring a re-read (`Dispatcher.Post`) or moving to a coercion-based design are the
remaining options, both judged not worth it. `CompositeScenarioTests` scenario 12 pins
the current behavior, and detaching/recycling a control is covered by scenario 13 (its
styles unapply, the sides go back to `NaN`, the contribution is withdrawn).

`CompositePropertyDescriptor` and `CompositeKind` are public for the same reason as the
hosts: a preset or app can declare its own four-sided host — a public static class with
four `AttachedProperty<double>` fields in the kind's slot order (`Thickness` or
`CornerRadius` today), a `Descriptor`, a call to `CompositeProperty.Register` from its
explicit static constructor, and `[EmittableGhostPropertyHost]` — and have CodeGen embed
it. `CompositeProperty.Register(descriptor, sides)` is the single public runtime entry
point: it validates that `descriptor.RealProperty` is a `StyledProperty<Thickness>` /
`StyledProperty<CornerRadius>` matching the kind (otherwise `ArgumentException`), picks
the matching compose function, and registers the class handlers. The shared
`CompositePropertyHandler<TValue>` stays internal — a third-party host only supplies the
declaration and the wiring.

## Runtime resolution

`AtomicGenerator<TTheme>.Generate(...)` can run at application startup: it
transforms the source text, extracts tokens, resolves them, orders them
(`StyleOrdering`), compiles the `Expression<Func<Selector, Selector>>`, and produces
`StringifiedUtil`s ready to become `Avalonia.Styling.Style`s. `Sandbox/Program.cs`
demonstrates this. The composite side utilities (`m-*`, `p-*`, `border-*` width,
`rounded-*`) and the grid utilities (`grid-cols-*`, `grid-rows-*`, `col-*`/`row-*`,
`col-span-*`/`row-span-*`) work on this path too, because everything they need is
ordinary compiled code in this assembly: the side properties are real
`AvaloniaProperty`s, and both the composite hosts and `GridDefinitions` register their
class handlers from an explicit static constructor.

Resource-based theming (`bg-*`, `text-*`, `border-*` colors) is the one feature that
only works through `Enx.Atomic.Avalonia.CodeGen` (see below): the style resolves to a
`DynamicResource` lookup, but nothing builds the `ResourceDictionary`
`ResourceDictionaryEmitter` emits, so at runtime the lookup finds no entry. See the root
README's Roadmap, v3, for the runtime connector that would cover it.

## Build-time C# code generation (`Sources/Enx.Atomic.Avalonia.CodeGen`)

Resolving tokens at every app startup has a cost. `Enx.Atomic.Avalonia.CodeGen` moves
that resolution to **build time** instead, producing a static `.g.cs` file (a
`Styles` subclass) that gets compiled straight into the consuming app.

**The emitter.** `StyleEmitter.Emit(utils, namespaceName, className, containerName)`
turns a `StringifiedUtil[]` into a full `.cs` file — a `Styles` subclass constructing
each `Style` and its `Setter`s directly. It works entirely off data:

- **`StringifiedUtil.SelectorData`/`ContainerQueryData`** — the `SelectorExpression`/
  `StyleQueryExpression` tree described above. `SelectorEmitter`/`StyleQueryEmitter`
  pattern-match on the node types (`Is`, `Class`, `OfType`, `PropertyEquals`, `Width`,
  `Height`, `Or`, `And`) and recurse over `.Previous`, producing text like
  `selector.Is<Button>().Class("hover:bg-red-500")`. `Or`/`And` are static factory
  calls in Avalonia (`StyleQueries.Or(params StyleQuery[])`), not fluent extensions
  like the others.
- **`Values/IValueEmitter`** — one emitter per value shape a `Setter` can carry:
  `PrimitiveValueEmitter` (bool/numeric/string), `EnumValueEmitter` (generic over any
  enum type), `Thickness`/`CornerRadiusValueEmitter`, `BrushValueEmitter` (any
  `ISolidColorBrush`), `FontFamilyValueEmitter` (`new FontFamily("Georgia, Cambria")`
  from the family-name list, or `FontFamily.Default`; throws for a family loaded from a
  font asset, whose source URI isn't recoverable from its names), `CursorValueEmitter`
  (reconstructs
  `new Cursor(StandardCursorType.X)`), `TextDecorationsValueEmitter` (the three named
  `TextDecorations.*` constants), `TextTrimmingValueEmitter` (the named
  `TextTrimming.*` instances `truncate`/`text-clip`/`text-ellipsis` set — `TextTrimming`
  is a class, not an enum, so `EnumValueEmitter` can't handle it),
  `ThemeVariantValueEmitter`, `BoxShadowsValueEmitter` (explicit `BoxShadow` layers and
  `Color.FromArgb`, never a culture-sensitive `BoxShadows.Parse` string),
  `GridDefinitionsValueEmitter` (see grid utilities), and `DynamicResourceValueEmitter` — a
  `StyleValue.Resource`'s
  `UntypedValue` is a `DynamicResourceExtension`; this emitter only needs its `Key`
  text (`new DynamicResourceExtension("Colors[red-500]")`), never the value the key
  resolves to — that's `ResourceDictionaryEmitter`'s job, described below.
- **`AvaloniaPropertyNaming`** — resolves an `AvaloniaProperty` back to its declaring
  static field's name (`"Button.IsPressedProperty"`) by reflection.
- **Embedded host classes** — a `Setter` targeting a "ghost" property (one that cannot
  be resolved to a pre-existing Avalonia property) needs that property's declaring class
  to exist in the consuming project, which may not reference the preset assembly at all.
  A class marked `[EmittableGhostPropertyHost]` is copied into the generated file once
  per used host: a composite host is generated by `CompositeHostSourceEmitter` from its
  `Descriptor` (plus one shared `CompositeSideRecomposition` helper per file), while any
  other host supplies a hand-maintained `EmbeddableSource` const — today only
  `GridDefinitions`, whose real static constructor registers the same class handlers so
  the grid utilities also work on the runtime path. `AvaloniaPropertyNaming` then
  rewrites the property reference to the embedded type's own field name.

Verified by `Tests/.../CodeGenTests.cs`, which actually **compiles** the emitted text
with Roslyn (`Microsoft.CodeAnalysis.CSharp`) and asserts no errors, and by
`Tests/.../Snapshots/EmittedCodeSnapshotTests.cs`: [Verify](https://github.com/VerifyTests/Verify)
snapshots of the styles and resource dictionary emitted for one token set covering
every rule family and variant, so any byte change in the output shows up as a
reviewable diff. Each snapshot test also asserts the emitter is byte-for-byte
repeatable, so an ordering regression can't hide behind re-accepting the snapshot.
The snapshot files are excluded from the test project's compilation.

**Resource-based theming (`ResourceDictionaryEmitter.cs`).** A color value (`bg-*`,
`text-*`, `border-*`) is a `StyleValue.Resource`, so `StyleEmitter` emits a
`DynamicResourceExtension` key rather than a `SolidColorBrush` baked straight into the
`Style`. Something still has to build the actual `ResourceDictionary` those keys
resolve against — that's `ResourceDictionaryEmitter`:
for every distinct `AtomicGenerator<TTheme>.ResolvedResources` entry (deduplicated by
`Key` — `bg-red-500`'s `Border`/`TemplatedControl`/`Panel` triple all share one), it
compiles and invokes the resource's `ThemeAccess` once against a real `TTheme`
instance to get the value, then emits it via the same `ValueEmitterRegistry` used for
`Setter` values. A non-`Themed<T>` result (or one built via the implicit conversion,
`IsThemed == false`) becomes a plain dictionary entry; an explicitly `Themed<T>` one
splits across `ResourceDictionary.ThemeDictionaries[ThemeVariant.Light]`/`[Dark]`.
Output is a second static class, e.g.:

```csharp
public static class AtomicResources
{
    public static ResourceDictionary Build() { /* ... */ }
}
```

which the consuming app merges once at startup — see
`Examples/Enx.Atomic.Avalonia.Example.App/App.axaml.cs`
(`Resources.MergedDictionaries.Add(AtomicResources.Build())`), before adding
`AtomicStyles`.

**The CLI (`AtomicCli.cs`).** A user's configuration project is a small executable
that references `Enx.Atomic.Avalonia`(`.Preset.Mini`)`.CodeGen`, builds its
`AtomicConfiguration<TTheme>`, and calls `AtomicCli.Run(args, configuration)` from
`Main`. `AtomicCli` is a [Spectre.Console.Cli](https://spectreconsole.net/cli/) app
with two commands: `generate`, the default — so the `.targets` invocation, which
passes no command name, is routed to it unchanged — and `probe` (below). `generate`
takes `--output`/`--namespace`/`--class`/`--container` options plus a list of
source files. It runs transformers and extractors per file (with the file path as the
transformers' `id`, so an `IdFilter` can scope one to a single source) but accumulates
extraction into **one shared token set**, then resolves that union once, merges in the
`PendingUtils` transformers registered through `AddUtil` — deduplicated with
`StyleOrdering.Distinct`, since they have no token to deduplicate by — orders the whole
set with `StyleOrdering.Sort`, and only then calls `StyleEmitter.Emit`. Resolving per
file and concatenating used to emit a token used by two files twice, and ordering per
file let a variant declared in one file sort before its base declared in another.
Each entry of `AtomicGenerator.Diagnostics` (a shortcut expanding to a token nothing
matches) is printed as a yellow warning, like a missing source file; the build goes on.
`--resources-output`/`--resources-class` (optional — omitting `--resources-output` skips
resource generation entirely) do the same for `ResourceDictionaryEmitter`. See
`Examples/Enx.Atomic.Avalonia.Example.Config` for a minimal config project — it
bootstraps Avalonia's headless platform before touching `AddMiniTheme`, because some
static rules (e.g. `Cursors`) construct real Avalonia types at static-init time and
need `AvaloniaLocator` populated.

Spectre instantiates commands through `Activator.CreateInstance`, so `Run` hands the
configuration — and an optional `IAnsiConsole`, which lets a test or an in-process
host capture the output — to them through a static, single-shot holder rather than
constructor injection: one `Run` per process.

**`probe` — the diagnostic loop.** `AtomicCli probe "hover:ml-4 p-4"` (or
`--file <path>`, repeatable; the text and files go through the configured
extractors, so markup like `Classes="..."` works too) explains a token set instead of
writing files. Per token: each variant peeled off and the class that recognised it,
the base token, the leaf tokens it expanded to when it is a shortcut, the winning rule
with its declaration index and rank, and a hint when
nothing matched (a known variant over an unknown base, or an unknown prefix over a
known utility). Per resolved style: its 1-based position in `StyleOrdering.Sort`'s
order for the whole probed set together with the `SortKey` parts that produced it,
the selector and container query rendered from the data trees, and every setter
(with its resource key, for a `DynamicResource`). `--control <Type>` (with `--width`,
`--pseudo :pointerover`, ...) goes further: it shows a real control on the headless
platform the config project already bootstraps, inside a `top-level` width container,
applies the styles in emitted order plus a resource dictionary built the way
`ResourceDictionaryEmitter` builds one, and reports the values Avalonia actually ended
up with — composite properties included, with the sides they were recomposed from.
`--json` prints one stable document (`tokens`/`resources`/`control`, documented on
`ProbeSettings`) for agents and scripts. It deliberately goes through the same public
API the runtime and build-time paths use, so what it reports is what `generate`
emits. The repo's `/probe` OpenCode command runs it against
`Examples/Enx.Atomic.Avalonia.Example.Config`.

**The MSBuild wiring (`build/Enx.Atomic.Avalonia.CodeGen.targets`).** A consuming
project opts in by setting `EnxAtomicConfigProject` to the configuration project's
`.csproj` path:

```xml
<PropertyGroup>
    <EnxAtomicConfigProject>..\MyApp.Config\MyApp.Config.csproj</EnxAtomicConfigProject>
</PropertyGroup>
```

Referencing `Enx.Atomic.Avalonia.CodeGen` as a `PackageReference` auto-imports the
`.targets` file (it's packed at `build/Enx.Atomic.Avalonia.CodeGen.targets`, the
standard NuGet convention). During the consuming project's build, the target:

1. Builds the configuration project via `<MSBuild Targets="Restore;Build">` and
   resolves its output assembly.
2. Runs it with `dotnet exec` against the consuming project's `@(Compile)` and
   `@(AvaloniaXaml)` items.
3. Writes the result to `GeneratedStyles/GenStyles.g.cs` and
   `GeneratedStyles/GenResources.g.cs`, next to the project file (not hidden under
   `obj/`, so they're easy to read) — overridable via `EnxAtomicStylesOutputPath`/
   `EnxAtomicResourcesOutputPath`. `GeneratedStyles/` is gitignored: it's regenerated
   on every build.
4. Compiles both files into the project.

The target runs `BeforeTargets="CoreCompile"` on every build, deliberately without
`Inputs`/`Outputs` — `AtomicCli` itself writes each file write-if-different instead
(see its `Execute` method), which is what actually lets the consuming project's own
incremental compile skip recompiling either generated file when nothing changed. See
`Examples/Enx.Atomic.Avalonia.Example.App` for a working end-to-end example.

### Known constraint: custom components and the build cycle

If the configuration project references `AvaloniaProperty`s of custom controls
defined in the consuming project itself, that's a cycle: the consumer needs the
`.g.cs` generated by the config project to compile, and the config project needs the
consumer's types to write rules against them. MSBuild cannot resolve
`ProjectReference` cycles.

A user with custom controls referenced by rules must define them in a separate
`Components` project, referenced by both the config project and the consuming app —
the same constraint Roslyn source generators already impose (a generator can never
reference the assembly it generates into).

## Release pipeline

`Enx.Atomic.Avalonia`, `Enx.Atomic.Avalonia.Preset.Mini`, and
`Enx.Atomic.Avalonia.CodeGen` are published to nuget.org by
`.github/workflows/release.yml`, triggered by pushing a `vX.Y.Z` tag.

- **Versioning**: [MinVer](https://github.com/adamralph/minver), referenced once in
  the root `Directory.Build.props` (`MinVerTagPrefix` = `v`), derives every project's
  `Version` from the nearest git tag — no project hand-maintains a version number.
- **Dependencies**: Central Package Management — every `PackageReference` is
  versionless and the root `Directory.Packages.props` holds the single version of
  each package (MinVer included), so the shipped packages, examples and tests can't
  drift onto different Avalonia versions.
- **Publishing**: [NuGet Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
  — the workflow requests a GitHub OIDC token (`permissions: id-token: write`) and
  exchanges it for a short-lived nuget.org API key via the `NuGet/login` action,
  rather than storing a long-lived API key secret. Requires a Trusted Publishing
  policy configured on nuget.org (repository owner/name + the workflow filename,
  `release.yml`) and a `NUGET_USER` repository secret holding the nuget.org profile
  name used to log in.
- Package metadata (`Authors`, `PackageProjectUrl`, `PackageLicenseExpression`,
  symbol packages, source link) is centralized in `Directory.Build.props`; each
  packable project only adds its own `Description` and `PackageTags`.

---

## Colophon

This document was drafted by Claude (Anthropic), via Claude Code, from the design
work done with [@TheKeyblader](https://github.com/TheKeyblader). The decisions it
records are his; the wording is a draft he is responsible for the accuracy of, per
this repository's
[AI use policy](https://github.com/TheKeyblader/.github/blob/main/CONTRIBUTING.md#ai-use).
