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
| `Tests/Enx.Atomic.Avalonia.Tests` | The automated test suite (xUnit v3). |

## How a token becomes a style

A string like `hover:bg-red-500` goes through four steps:

1. **Extraction** — an `Extractor` scans source text and pulls out candidate tokens
   (e.g. every word inside `Classes="..."`).
2. **Variant matching** — a `VariantBase<TTheme>` strips a recognized prefix/suffix
   (`hover:`) and remembers how it should transform the final selector or container
   query. This repeats until no variant matches, which is what makes chaining
   (`hover:focus:...`) work.
3. **Rule matching** — an `IRule` matches what's left of the token (`bg-red-500`)
   against either a fixed name (`IStaticRule`) or a regex (`IDynamicRule<TTheme>`),
   and produces one or more `StyleValue`s (a `Setter` value, fixed or resolved from
   the theme).
4. **Emission** — the resolved selector, container query, and `StyleValue`s become a
   `StringifiedUtil`, which either becomes a real `Avalonia.Styling.Style` at runtime,
   or gets handed to the code generator to emit as C# (see below).

## Core engine (`Sources/Enx.Atomic.Avalonia`)

- **`IRule`** — `IStaticRule` (`Rule.Static`) matches a token by exact name and
  carries a fixed set of `StyleValue`s. `IDynamicRule<TTheme>` matches by regex and
  computes its `StyleValue`s from the match and the theme (`RuleContext<TTheme>`).
  When several rules could match, a static rule always wins over dynamic ones; among
  dynamic rules, the first match in `Rules` declaration order wins — this is how two
  rules can share a prefix (`border-2` as width vs `border-red-500` as color) without
  one convoluted regex.
- **`StyleValue`** — `Literal<TValue>` (value fixed at resolution time) or `Resource`
  (a runtime `DynamicResource` lookup, for values that must track theme/resource
  changes).
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
  (`AtomicGenerator.ApplyTransformers`), always before extraction.
- **`AtomicConfiguration<TTheme>`** — assembles `Transformers`, `PreProcessors`,
  `Rules`, `Variants`, `Extractors`, and the `TTheme` instance.
- **`AtomicGenerator<TTheme>`** — the engine itself. `Generate(...)` runs the full
  pipeline above and returns `StringifiedUtil[]`, cached by raw token.

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
  corner-radius) are consolidated into a single class per family via named capture
  groups (`side`, `axis`, `bound`, `neg`) instead of one class per direction — e.g.
  `MarginRule` handles `m-*`, `mx-*`, `mt-*`, ... in a single `Match`.
  - **`ThemeScale`** — resolves a theme key, falling back to a bare number.
    `TryResolve` treats that number as **rem** (converted to px via
    `RemToPxFactor`) — the behavior of most UnoCSS scales (`spacing`, `borderRadius`,
    ...). `TryResolvePx` treats it as a **raw px** value, no conversion — UnoCSS's
    `lineWidth` scale behavior (border/ring/outline).
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

## Ghost properties: combining per-side utilities

Avalonia's `Margin`, `Padding`, `BorderThickness`, and `CornerRadius` are each a
single struct-valued property, not four independent ones. Two utilities on the same
element (`ml-1 mr-2`) would normally resolve to two separate `Style`s each setting
the *whole* struct — and Avalonia lets the later one win outright rather than merging
them.

To fix this:

- **Ghost properties (`SpecialProperties.cs`)** — a per-side rule (`ml-1`) targets a
  *ghost* `AvaloniaProperty`, owned by a type (`SpecialProperties`) that's never
  referenced by the consuming project. `GhostProperties.Map` records, for each real
  composite property, which ghost property backs which of its four slots.
- **`GhostPropertyCombiner<TTheme>`** — a `Post`-stage `ISourceTransformer<TTheme>`,
  auto-registered by `AddMiniTheme`. For each source line, it resolves every token on
  that line, groups the ones targeting the same real property, and calls
  `AtomicGenerator<TTheme>.AddUtil` with a combined style: a **compound selector**
  requiring every contributing class (`.Class("ml-1").Class("mr-2")`) and one
  `Setter` carrying the assembled `Thickness`/`CornerRadius`. It never rewrites the
  source text — the selector it builds only ever references classes that are
  genuinely present in the real source. A style whose owner type is
  `SpecialProperties` never matches a real element on its own; it's dropped at
  `AtomicGenerator.Generate`'s final emission boundary, so only the combined style
  reaches output.

Because the combined selector only depends on classes already in the source, this
works identically whether resolved at runtime or by the build-time emitter below.

## Runtime resolution

`AtomicGenerator<TTheme>.Generate(...)` can run at application startup: it
transforms the source text, extracts tokens, resolves them, compiles the
`Expression<Func<Selector, Selector>>`, and produces `StringifiedUtil`s ready to
become `Avalonia.Styling.Style`s. `Sandbox/Program.cs` demonstrates this.

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
  `ISolidColorBrush`), `CursorValueEmitter` (reconstructs
  `new Cursor(StandardCursorType.X)`), and `TextDecorationsValueEmitter` (the three
  named `TextDecorations.*` constants).
- **`AvaloniaPropertyNaming`** — resolves an `AvaloniaProperty` back to its declaring
  static field's name (`"Button.IsPressedProperty"`) by reflection.

Verified by `Tests/.../CodeGenTests.cs`, which actually **compiles** the emitted text
with Roslyn (`Microsoft.CodeAnalysis.CSharp`) and asserts no errors.

**The CLI (`AtomicCli.cs`).** A user's configuration project is a small executable
that references `Enx.Atomic.Avalonia`(`.Preset.Mini`)`.CodeGen`, builds its
`AtomicConfiguration<TTheme>`, and calls `AtomicCli.Run(args, configuration)` from
`Main`. `AtomicCli` is a [Spectre.Console.Cli](https://spectreconsole.net/cli/)
command with `--output`/`--namespace`/`--class`/`--container` options plus a list of
source files: it runs each file through `AtomicGenerator<TTheme>.Generate(...)`, then
`StyleEmitter.Emit`, and writes the result. See
`Examples/Enx.Atomic.Avalonia.Example.Config` for a minimal config project — it
bootstraps Avalonia's headless platform before touching `AddMiniTheme`, because some
static rules (e.g. `Cursors`) construct real Avalonia types at static-init time and
need `AvaloniaLocator` populated.

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

1. Builds the configuration project via `<MSBuild Targets="Build">` and resolves its
   output assembly.
2. Runs it with `dotnet exec` against the consuming project's `@(Compile)` and
   `@(AvaloniaXaml)` items.
3. Writes the result to `GeneratedStyles/GenStyles.g.cs`, next to the project file
   (not hidden under `obj/`, so it's easy to read) — overridable via
   `EnxAtomicStylesOutputPath`. `GeneratedStyles/` is gitignored: it's regenerated on
   every build.
4. Compiles that file into the project.

The target runs `BeforeTargets="CoreCompile"` with `Inputs`/`Outputs` for
incrementality. See `Examples/Enx.Atomic.Avalonia.Example.App` for a working
end-to-end example.

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
