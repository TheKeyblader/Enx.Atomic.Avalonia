# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versioning follows
[Semantic Versioning](https://semver.org/). Versions are git tags (`vX.Y.Z`) — see
[ARCHITECTURE.md](ARCHITECTURE.md#release-pipeline) for how a tag becomes a release.

## [Unreleased]

### Added

- `ClassesAttributeExtractor`, now in the default `AtomicConfiguration.Extractors` alongside
  `SplitExtractor`: a conditional class, `Classes.line-through="{Binding IsDone}"`, now gets its
  style generated. Before, `SplitExtractor` saw `Classes.line-through=` as one unmatchable
  candidate and the class silently had no style. The class name is an XML attribute name, so it
  can't carry a variant, an arbitrary `[...]` value or a `/` (and a name containing `.` isn't
  matched) — toggle those from code-behind (`Classes.Set("dark:line-through", v)`), whose quoted
  string `SplitExtractor` already picks up. A configuration that replaces `Extractors` must add it
  back to get this.
- `Examples/Enx.Atomic.Avalonia.Example.TodoApp`: a small todo app (add, toggle, delete, filter,
  clear completed) styled almost entirely with utility classes — `dark:`, `hover:`, `focus:`,
  `checked:` and `disabled:` variants, and conditional `Classes.line-through`/`Classes.opacity-45`
  for completed items. It reuses the existing `Example.Config` project.

## [0.0.6] - 2026-11-20

### Added

- `size-*`, `size-min-*` and `size-max-*`: set `Width` + `Height` (or the `Min*`/`Max*` pair) from
  the existing `Sizes` scale, with the same bare-number and arbitrary-px resolution as `w-*`/`h-*`.
  Registered before `w-*`/`h-*` — upstream's order — so `size-4 w-8` keeps the axis token's width and
  `size-4`'s height; `size-full`/`size-auto` are the two-axis forms of `w-full`/`w-auto`.
- `border-x-*` and `border-y-*`: left+right / top+bottom border widths through the existing
  `BorderThicknessSides` composite host. `BorderWidthRule` now ranks uniform (0) < axis (1) < side
  (2), so `border-2 border-x-4 border-t-8` composes to 4/8/4/2 whatever the source order.
- `opacity-*` and `op-*`: set `Visual.OpacityProperty`. A bare token is a percentage
  (`opacity-50` = 0.5), while an arbitrary value is taken as written (`opacity-[0.35]` fraction,
  `opacity-[50%]` percentage). No theme scale.
- `z-*` and `z{n}`: set `Visual.ZIndexProperty` (`int`); a fractional value is a miss rather than a
  rounded one. Upstream's `z-auto` is not ported — an `int` cannot express "as if unset".
- `leading-*`, `lh-*` and `line-height-*` (**partial**): set `TextBlock.LineHeightProperty` from an
  arbitrary px value (`leading-[24px]`) or a bare number. Upstream's named scale is unitless
  multipliers with no absolute Avalonia equivalent, so `leading-tight`/... do not resolve.
- `tracking-*` (**partial**): sets `TextElement.LetterSpacingProperty` from an arbitrary px value or
  a bare number. Upstream's scale is in `em` (relative to the element's font size), which
  Avalonia's pixel `LetterSpacing` cannot express, so `tracking-wide`/... do not resolve.
- `font-sans`, `font-serif` and `font-mono`: set the inherited `TextElement.FontFamilyProperty` from
  a new `IFontFamilyPart.FontFamilies` scale, plus a public `FontFamilyValueEmitter` that emits
  `new FontFamily("...")` (or `FontFamily.Default`) — covered by the Roslyn compile check. The static
  `font-bold`-style weight tokens still win the shared `font-` prefix.
- `inset-*`, `inset-x-*`, `inset-y-*`, `inset-{t,r,b,l}-*`, `top-*`/`right-*`/`bottom-*`/`left-*`
  (**partial**): set `Canvas`'s attached placement properties from the spacing scale, with a leading
  `-` negating the value; ranked uniform < axis < side. Only meaningful on a child of a `Canvas`;
  `start`/`end` and upstream's `auto`/fraction forms are not ported.
- `w-full`/`h-full` and `w-auto`/`h-auto`. Avalonia has no percentage lengths, so
  `w-full`/`h-full` clear `Width`/`Height` to `double.NaN` and set the matching
  alignment to `Stretch` (filling the space the parent offers), while
  `w-auto`/`h-auto` only clear the length. `SizeRule` ranks `auto` below a concrete
  size and `full` above it, so the outcome is deterministic whatever the source order.
- `shadow`, `shadow-{size}` (`sm`, `md`, `lg`, `xl`, `2xl`), `shadow-inner` and
  `shadow-none`: UnoCSS's `boxShadow` scale ported to Avalonia `BoxShadows`
  (multi-layer, inset for `inner`, empty for `none`) and applied to
  `Border.BoxShadowProperty` and `ContentPresenter.BoxShadowProperty`. Shadow colors,
  the `shadow-inset` modifier, `shadow-opacity-*` and arbitrary values are not ported.
  New `IBoxShadowPart.BoxShadows` theme part and public `BoxShadowsValueEmitter`.
- `CompositeProperty.Register(descriptor, sides)`: the public runtime entry point for a
  third-party composite host. It validates the descriptor's real property against
  `CompositeKind` (`StyledProperty<Thickness>`/`StyledProperty<CornerRadius>`), picks
  the compose function and registers the class handlers — `CompositePropertyHandler`
  itself stays internal. The four shipped hosts now use it.

### Fixed

- Grid utilities (`grid-cols-*`, `grid-rows-*`, `col-*`/`row-*`, `col-span-*`/
  `row-span-*`) now work through runtime resolution: `GridDefinitions` has an explicit
  static constructor that registers the class handlers cloning its ghost definitions
  into `Grid.ColumnDefinitions`/`RowDefinitions`, mirroring the codegen copy.
- A cyclic `MultiPass` variant looped forever instead of throwing:
  `AtomicGenerator.MatchVariants`' 500-handler guard read a count captured before its
  matching loop and never tripped.
- An ambiguous (forking) variant dropped every variant an earlier pass had matched:
  `a:` was lost from each branch of `a:b:x`. Preset.Mini's variants are never
  ambiguous, so this only affected custom variants.
- **Breaking (visual):** a bare number that isn't a scale key is now read in quarter-rems,
  as UnoCSS's `rem` handler does (`n` → `n / 4` rem), instead of whole rems. `w-4`/`h-4`
  are 16px (were 64px), `w-64` is 256px (was 1024px), and the same applies to every
  `TryResolve` family's unmatched numbers (`size-*`, `min-/max-w/h-*`, `rounded-*`,
  `text-*` sizes, `m-*`/`p-*`/`gap-*` beyond the spacing scale, `leading-*`,
  `tracking-*`). Spacing keys already in the scale (`m-4` = 16px) are unchanged.

### Changed

- **Breaking:** theme scales are `double` instead of `float` —
  `ISpacingPart.Spacing`, `ISizePart.Sizes`, `IRadiusPart.Radii`,
  `ILineWidthPart.LineWidths`, `IRemToPxPart.RemToPxFactor`, `MiniTheme`,
  `DefaultTheme` and `ThemeScale.TryResolve`/`TryResolvePx` all changed type. Numeric
  values are now the same type as Avalonia's length properties, removing the widening
  step (and float precision artifacts) at every rule. Custom themes implementing these
  parts need to update their scale types.
- CI builds and tests on `windows-latest` as well as `ubuntu-latest`. Verify snapshots
  are checked out with LF endings on every OS (`.gitattributes`).
- The release workflow uses `actions/checkout`/`actions/setup-dotnet` v5 and has job
  and test-step timeouts.

## [0.0.5] - 2026-11-20

### Added

- Resource-based theming: `bg-*`/`text-*`/`border-*` now emit a `DynamicResource`
  lookup instead of an inlined `SolidColorBrush`, backed by a generated
  `ResourceDictionary` (`GenResources.g.cs`, merged into `Application.Resources` once
  at startup) instead of literal brushes baked into every generated `Style`.
- `Themed<T>`, a theme scale entry that may differ between light and dark (e.g.
  `IColorPart.Colors : Dictionary<string, Themed<IBrush>>`) — implicitly convertible
  from a plain value, so an entry only actually splits across
  `ResourceDictionary.ThemeDictionaries[Light]`/`[Dark]` when a theme author opts in
  via `new Themed<T>(light, dark)`.
- `AtomicCli`'s `--resources-output`/`--resources-class` options (optional — omitting
  `--resources-output` skips resource generation), and the matching
  `EnxAtomicResourcesOutputPath`/`EnxAtomicResourcesClassName` MSBuild properties.
- Arbitrary values: UnoCSS/Tailwind's bracket syntax (`bg-[#ff0000]`, `w-[123px]`,
  `m-[10px]`, `rounded-[6px]`, `border-[3px]`, `text-[18px]`) bypasses a token's theme
  scale entirely — a literal color or a literal pixel value (no rem conversion), never
  a `DynamicResource`. Covers every color rule (`bg-*`/`text-*`/`border-*`) and every
  px-based scale (Spacing, Sizes, Radii, LineWidths, FontSizes).
- `StyleOrdering` (`Sort`/`Distinct`) — the canonical emission order (specificity →
  container order → rule index → rule rank → variant sort → selector text) shared by
  the runtime generator and `AtomicCli`, plus structural deduplication for
  transformer-registered styles.
- Composite side properties: `MarginSides`, `PaddingSides`, `BorderThicknessSides` and
  `CornerRadiusSides` — four `AttachedProperty<double>` each, recomposed into the real
  `Thickness`/`CornerRadius` by a class handler at `BindingPriority.Style` — described
  by a public `CompositePropertyDescriptor`/`CompositeKind`, with a generated,
  self-contained copy of each used host embedded by the code generator.
- `StringifiedUtil.Rank`/`Sort` and `IOrderRankedDynamicRule<TTheme>` — the two
  rule-level ordering signals: `Rank` lets a consolidated directional rule rank its own
  matches (`m-*` < `mx-*` < `ml-*`) the way UnoCSS's separate rule entries used to, and
  `Sort` is a variant-provided hint (still unused by the Mini preset).
- `AtomicGenerator.PendingUtils`, exposing the styles a source transformer registered
  through `AddUtil` during the last `ApplyTransformers` call.
- `AtomicCli probe`, a diagnostic command that explains how a token set resolves:
  - per token: the variants peeled off, the winning rule (with its index and rank), and
    a hint when nothing matched;
  - per style: its position in the emission order and the sort key behind it, its
    selector, container query and setters;
  - with `--control <Type>` (plus `--width`/`--pseudo`): the effective values on a real
    headless control, composite properties included;
  - `--json` prints a stable machine-readable document.

  The existing behaviour stays the default `generate` command, so the MSBuild
  invocation is unchanged.
- `AtomicCli.Run(args, configuration, IAnsiConsole? console = null)`: an optional
  console, so a test or an in-process host can capture the CLI's output.
- `ProbeSettings`, the `probe` command's options. Its XML doc describes the `--json`
  schema.
- `StyleOrdering.SortKey`/`StyleOrdering.GetSortKey`, which expose the ordering key
  `StyleOrdering.Sort` uses.
- `TextTrimmingValueEmitter`, the value emitter for `TextBlock.TextTrimming`.
- `Preset.Mini/PARITY.md`, a family-by-family matrix of `@unocss/preset-mini` against
  this port, kept honest by `ParityMatrixTests`: every example token of a ported or
  partial row must resolve, and the summary counts must match the tables.
- Verify snapshot tests of the emitted styles and resource dictionary.

### Changed

- A resource's key is always derived automatically from the theme-scale expression a
  rule read to produce it (e.g. `t => t.Colors[value]` → `"Colors[red-500]"`), never
  passed in by hand — guarantees two rules reading the same theme entry always agree
  on one key, and two rules reading different entries can never collide.
- `m-*`/`mx-*`/`my-*`/`mt-*`/... and `p-*`/`border-*` (width)/`rounded-*` now write
  only their host's side properties, the uniform token included, and target `Control`
  explicitly — so `p-*`, `border-*` and `rounded-*` also reach `TemplatedControl`
  subclasses such as `Button`.
- `AtomicCli` runs transformers and extractors per file but resolves the union of every
  source's tokens once, deduplicates transformer output structurally, then orders the
  whole set together.
- The emitter embeds a generated copy of each composite host a stylesheet targets
  (from its `CompositePropertyDescriptor`) plus one shared recomposition helper,
  alongside the existing hand-written `EmbeddableSource` path `GridDefinitions` still
  uses.
- `AtomicGenerator.MatchVariants` throws `InvalidOperationException` instead of a bare
  `Exception` when a token accumulates more than 500 variant handlers (a cyclic
  variant). Every engine exception that had no message now says what failed: the
  offending rule type, the ambiguous `MultiPass` variant and its token, or the
  Avalonia member a reflection lookup couldn't find.
- Package versions are managed centrally (`Directory.Packages.props`). The published
  packages' dependency versions are unchanged.

### Removed

- `GhostPropertyCombiner<TTheme>`, `SpecialProperties`, `GhostProperties`/`Map`, and
  `ThemeBuilderExtensions.AddGhostPropertyCombiner` — replaced by composite side
  properties. `ISourceTransformer`, `AddUtil` and `ApplyTransformers` stay.

### Fixed

- A variant written before its base (`hover:w-8 w-4`) silently lost its variant state:
  equal-priority styles resolve last-inserted-wins, and emission followed source order.
  Styles are now emitted in a specificity-aware order, so source order never matters.
- Per-side utilities losing their variant pseudo-class, container query, split-line
  combination or overlapping-utility behaviour — the old build-time combiner rebuilt
  the selector from class names alone.
- `p-*`, `border-*` (width) and `rounded-*` not applying to `TemplatedControl`
  subclasses such as `Button`.
- A token used in two source files being emitted twice by `AtomicCli`.
- `truncate`, `text-clip` and `text-ellipsis` broke code generation ("No value emitter
  registered" for `TextTrimming`).
- CI hanging indefinitely in `dotnet test`: the MSBuild integration tests' nested
  `dotnet build` could leave build servers holding its redirected output open. Nested
  builds now disable build servers, drain stdout/stderr concurrently and time out. CI
  also has job/step timeouts and `--blame-hang-timeout` diagnostics.

## [0.0.4] - 2026-08-25

### Added

- `dark:` variant, matching Avalonia's native `ThemeVariant` (Light/Dark).
- Per-side border-width utilities: `border-t-*`, `border-r-*`, `border-b-*`, `border-l-*`.
- Grid utilities: `grid-cols-*`/`grid-rows-*` (equal-column/row `Grid` definitions) and
  `col-*`/`row-*`/`col-span-*`/`row-span-*` (`Grid.Column`/`Row`/`ColumnSpan`/`RowSpan`).
- `bg-*` now also targets `Panel.BackgroundProperty` (`StackPanel`, `Grid`, `DockPanel`, ...),
  alongside `Border`/`TemplatedControl`.
- A utility-class reference README for `Enx.Atomic.Avalonia.Preset.Mini`, and a Roadmap
  section in the root README.

### Fixed

- Building the consuming app project standalone (not via the whole solution) could fail
  the first time with `NETSDK1004`, since nothing restored the configuration project on
  its own.
- Editing a rule/theme in the configuration project while leaving the app's own sources
  untouched could leave the generated styles silently stale.
- A `Setter`'s default selector target for an attached property (e.g.
  `Grid.ColumnSpanProperty`) now targets `StyledElement` instead of the property's
  declaring type — fixes `col-span-*`/`row-span-*`, and incidentally the same
  pre-existing bug in `scroll-x-*`/`scroll-y-*`, only ever matching the wrong element.

### Changed

- Bumped Avalonia to 12.1.1, Spectre.Console.Cli to 0.55.0, Roslynator.Analyzers to 5.0.0,
  and the test project's `coverlet.msbuild`/`Microsoft.NET.Test.Sdk`/`Microsoft.CodeAnalysis.CSharp`.
- Removed the unused `Dunet` dependency.

## [0.0.3] - 2026-08-23

### Fixed

- `bg-red-500` (and other properties shared via `AddOwner`, e.g. `TemplatedControl.BackgroundProperty`)
  not applying to `Button`/`ComboBox`/etc., since `AvaloniaProperty.OwnerType` always reports the
  type that originally registered a property, not the type it was actually reached through.
- A code-gen boxing bug: a whole-number `double` value (e.g. for `MaxWidthProperty`) was emitted
  as a bare numeric literal, inferred as `int` by the compiler and crashing at runtime when
  Avalonia applied the boxed `Setter` value.
- A latent reflection bug in `SelectorExpression`'s `PropertyEquals` node (`BindingFlags.Static`
  alone always returns zero fields) and a `CS0252` reference-equality warning.

## [0.0.2] - 2026-08-23

### Removed

- Unused dependencies from `Enx.Atomic.Avalonia`: `AvantiPoint.CodeGenHelpers`,
  `ExpressionToCodeLib`, `FastExpressionCompiler`, `Microsoft.Extensions.FileSystemGlobbing`,
  and `Testably.Abstractions` — leftovers from an earlier, since-dropped
  code-generation approach that nothing in the current codebase references anymore.
  This also removes a NuGet warning about a prerelease dependency
  (`AvantiPoint.CodeGenHelpers`) inside a stable package.

## [0.0.1] - 2026-08-23

Initial release.

### Added

- The core engine (`Enx.Atomic.Avalonia`): rules, variants, source transformers, and
  `AtomicGenerator<TTheme>`, resolving UnoCSS/Tailwind-style utility tokens
  (`hover:bg-red-500`, `sm:flex-row`, `ml-4`, ...) into real Avalonia `Style`s.
- Ghost properties and `GhostPropertyCombiner`, so per-side utilities on the same
  element (`ml-1 mr-2`) combine into a single style instead of overwriting each
  other's struct-valued property.
- `Enx.Atomic.Avalonia.Preset.Mini`, a concrete rule/variant set inspired by
  `@unocss/preset-mini`.
- `Enx.Atomic.Avalonia.CodeGen`: build-time C# code generation — an emitter that
  turns resolved styles into a compiled `Styles` class, a CLI
  (`AtomicCli`, built on Spectre.Console.Cli) for a consumer's configuration
  project to call, and MSBuild wiring (`Enx.Atomic.Avalonia.CodeGen.targets`) that
  runs it automatically as part of a consuming project's build.
- `Examples/` — a minimal end-to-end example of the build-time code generation
  pipeline (a configuration project plus a consuming Avalonia app).
- The automated test suite (xUnit v3, `Avalonia.Headless.XUnit`), CI (build, test,
  coverage via Codecov), and the release pipeline itself: package versioning via
  MinVer and publishing to nuget.org via NuGet Trusted Publishing (OIDC), triggered
  by pushing a `vX.Y.Z` tag.
- MIT license.

[Unreleased]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.6...HEAD
[0.0.6]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.5...v0.0.6
[0.0.5]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.4...v0.0.5
[0.0.4]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.3...v0.0.4
[0.0.3]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.2...v0.0.3
[0.0.2]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/releases/tag/v0.0.1
