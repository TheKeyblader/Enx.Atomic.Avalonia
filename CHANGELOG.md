# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versioning follows
[Semantic Versioning](https://semver.org/). Versions are git tags (`vX.Y.Z`) — see
[ARCHITECTURE.md](ARCHITECTURE.md#release-pipeline) for how a tag becomes a release.

## [Unreleased]

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

[Unreleased]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.4...HEAD
[0.0.4]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.3...v0.0.4
[0.0.3]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.2...v0.0.3
[0.0.2]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/releases/tag/v0.0.1
