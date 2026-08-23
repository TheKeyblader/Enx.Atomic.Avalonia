# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versioning follows
[Semantic Versioning](https://semver.org/). Versions are git tags (`vX.Y.Z`) — see
[ARCHITECTURE.md](ARCHITECTURE.md#release-pipeline) for how a tag becomes a release.

## [Unreleased]

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

[Unreleased]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.2...HEAD
[0.0.2]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/TheKeyblader/Enx.Atomic.Avalonia/releases/tag/v0.0.1
