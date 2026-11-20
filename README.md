# Enx.Atomic.Avalonia

[![CI](https://github.com/TheKeyblader/Enx.Atomic.Avalonia/actions/workflows/ci.yml/badge.svg)](https://github.com/TheKeyblader/Enx.Atomic.Avalonia/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/TheKeyblader/Enx.Atomic.Avalonia/graph/badge.svg)](https://codecov.io/gh/TheKeyblader/Enx.Atomic.Avalonia)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Enx.Atomic.Avalonia on NuGet](https://img.shields.io/nuget/v/Enx.Atomic.Avalonia?label=Enx.Atomic.Avalonia&logo=nuget)](https://www.nuget.org/packages/Enx.Atomic.Avalonia)
[![Enx.Atomic.Avalonia.Preset.Mini on NuGet](https://img.shields.io/nuget/v/Enx.Atomic.Avalonia.Preset.Mini?label=Preset.Mini&logo=nuget)](https://www.nuget.org/packages/Enx.Atomic.Avalonia.Preset.Mini)
[![Enx.Atomic.Avalonia.CodeGen on NuGet](https://img.shields.io/nuget/v/Enx.Atomic.Avalonia.CodeGen?label=CodeGen&logo=nuget)](https://www.nuget.org/packages/Enx.Atomic.Avalonia.CodeGen)

An atomic utility-class engine for [Avalonia](https://avaloniaui.net/), inspired by
[UnoCSS](https://unocss.dev/): resolve Tailwind/UnoCSS-style tokens
(`hover:bg-red-500`, `sm:flex-row`, `ml-4`, ...) into real Avalonia styles instead of
hand-written XAML `Style`s.

```xml
<Button Classes="flex-row p-4 rounded-md bg-blue-500 hover:bg-blue-600" Content="Click me" />

<!-- A class toggled by a binding gets its style too -->
<TextBlock Classes="truncate" Classes.line-through="{Binding IsDone}" Text="{Binding Title}" />
```

Several utilities can be grouped under one name with a **shortcut**, declared in the
configuration — variants included, which a binding-toggled class can't carry otherwise:

```csharp
configuration.Shortcuts.Add(Shortcut.Static("done", "line-through opacity-45 dark:text-gray-400"));
configuration.Shortcuts.Add(Shortcut.Dynamic<MiniTheme>(
    new Regex("^btn-(.+)$"), (match, _) => $"bg-{match.Groups[1].Value}-500 hover:bg-{match.Groups[1].Value}-600 px-4 py-2 rounded"));
```

```xml
<TextBlock Classes.done="{Binding IsDone}" Text="{Binding Title}" />
<Button Classes="btn-blue" Content="Save" />
```

A plain utility written next to a shortcut overrides it (`btn-blue bg-red-500` is red).

See [ARCHITECTURE.md](ARCHITECTURE.md) for how the engine actually works — rules,
variants, source transformers, the Mini preset, and build-time code generation.

## Projects

| Project | Role |
|---|---|
| `Sources/Enx.Atomic.Avalonia` | The generic engine: resolves tokens into styles, independent of any preset. |
| `Sources/Enx.Atomic.Avalonia.Preset.Mini` | A concrete rule/variant set, inspired by `@unocss/preset-mini` — see its [README](Sources/Enx.Atomic.Avalonia.Preset.Mini/README.md) for the full utility-class reference. |
| `Sources/Enx.Atomic.Avalonia.CodeGen` | Build-time C# code generation: emitter, CLI, and MSBuild wiring. |
| `Sandbox` | An Avalonia console app used as a manual test bed for the engine and the presets. |
| `Examples/` | End-to-end examples of the build-time code generation pipeline: a shared config project, a minimal app, and a small todo app styled almost entirely with utility classes. |
| `Tests/Enx.Atomic.Avalonia.Tests` | The automated test suite (xUnit v3). |

## Getting started

Requires a .NET SDK compatible with `net10.0` (a `net10.0`-targeting preview SDK
works too).

```bash
dotnet build Enx.Atomic.Avalonia.slnx
```

There are two ways to resolve utility tokens into styles — **build time is the
supported, complete path**; runtime is a lighter-weight sandbox/demo path with a
real gap (see below):

- **At build time** — set up a small configuration project and wire it into your
  app's build via `Enx.Atomic.Avalonia.CodeGen`, so tokens are resolved once at
  compile time instead of on every startup. See `Examples/` and
  [ARCHITECTURE.md](ARCHITECTURE.md#build-time-c-code-generation) for the full setup.
- **At runtime** — call `AtomicGenerator<TTheme>.Generate(...)` yourself and add the
  resulting styles to your `Application.Styles`. See `Sandbox/Program.cs`. Grid
  utilities (`grid-cols-*`, `col-span-*`, ...) work here too; only resource-based theming
  (`bg-*`/`text-*`/`border-*` colors) is **codegen-only today** — at runtime the style's
  `DynamicResource` lookup finds no entry, since only the build-time pipeline builds the
  `ResourceDictionary`. Closing that last gap would need a connector watching for
  newly-used classes across loaded components and resolving them live — see the
  Roadmap's v3.

## Testing

```bash
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj
```

With code coverage (Cobertura report under `coverage/`):

```bash
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj \
  /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../coverage/
```

The emitted code is snapshot-tested with [Verify](https://github.com/VerifyTests/Verify)
(`Tests/Enx.Atomic.Avalonia.Tests/Snapshots/`): an intended change to the generated
output shows up as a failing test plus a `.received.cs` file. Review the diff, then
accept it by replacing the matching `.verified.cs` file with it.

To see how a set of tokens resolves (variants and rule matched, emission order,
setters, and, with `--control`, the values Avalonia actually applies), use the
CLI's `probe` command against the example configuration:

```bash
dotnet run --project Examples/Enx.Atomic.Avalonia.Example.Config -- probe "hover:ml-4 p-4" --control Button
```

## Releasing

`Enx.Atomic.Avalonia`, `Enx.Atomic.Avalonia.Preset.Mini`, and
`Enx.Atomic.Avalonia.CodeGen` are published to nuget.org whenever a `vX.Y.Z` tag is
pushed — see [ARCHITECTURE.md](ARCHITECTURE.md#release-pipeline) for how versioning
and publishing work, and [CHANGELOG.md](CHANGELOG.md) for what shipped in each
release.

## Roadmap

Ideas being considered, not commitments with a date — this is an early, solo-maintained
project. Grouped by rough priority rather than a flat list: beta extends the
token/rule vocabulary and lays down infrastructure the later milestones need, v1 is
ready to build a real app on — every manual mechanism an app needs is there, just not
the tooling/automation on top of it yet (an editor extension, auto-discovering a
safelist from referenced packages) — v2 is where that tooling/automation, plus
deeper architectural bets, land — and v3 closes the remaining runtime gap
(resource-based theming) so runtime resolution matches build-time codegen. Alpha (stabilizing what
already shipped, including a tested MSBuild build system and a grid vocabulary —
`grid-cols-*`/`grid-rows-*`, `col-*`/`row-*`, `col-span-*`/`row-span-*`) has nothing
outstanding right now.

### Beta — extend the vocabulary, lay down infrastructure

- **More presets.** Only `Preset.Mini` exists so far — the engine itself
  (`Sources/Enx.Atomic.Avalonia`) doesn't assume any particular rule set.

### v1 — ready to build a real app on

- **Manual safelist.** A way to explicitly tell `AtomicCli` "generate a style for
  these tokens too," beyond whatever it finds by scanning `@(Compile)`/`@(AvaloniaXaml)`
  — needed the moment a token only exists somewhere the extractor doesn't look
  (built dynamically, or coming from a referenced library's compiled XAML). The
  ecosystem-wide automated version of this is in v2 below; this is the manual
  escape hatch an app can already reach for on its own.
- **`/template/` selector support.** `SelectorExpression` (`Compact/SelectorExpression.cs`)
  has no node for Avalonia's `Template` combinator (`Selectors.Template`, XAML's
  `/template/`) — there's currently no way for a rule/variant to express a style
  targeting a part inside a control's template (e.g. `Button /template/ ContentPresenter`).
  Real gap, but a rare one in practice — most styling never needs to reach into a
  template's parts — so it's not worth holding up earlier milestones for.

### v2 — tooling, automation, and bigger architectural bets

- **Editor autocomplete.** A Visual Studio / VS Code / Rider extension offering
  IntelliSense for utility-class tokens inside `Classes="..."` — completion, hover
  docs, maybe a color swatch — the way the official Tailwind CSS IntelliSense
  extension works. Would need its own token/scale data source (this repo's
  `Preset.Mini` rules aren't in a machine-readable form yet) plus one integration
  per IDE.
- **Safelists discovered from referenced libraries, automatically.** The v1 manual
  safelist above solves this per app; this is the automated version for an actual
  ecosystem of component libraries built on this engine — a library would emit its
  own list of tokens it needs (e.g. as a packaged file, the way
  `Enx.Atomic.Avalonia.CodeGen.targets` itself ships under `build/`), and a
  consuming app's MSBuild wiring would discover and merge every such list from its
  referenced packages with no manual wiring per dependency. Only worth building
  once there's actually more than one library out there to discover safelists from.
- **Computed resources with a baked-in dependency graph.** Beyond plain scale
  values, resources derived *from* other resources (e.g. a hover/pressed shade
  computed from a base color). The dependency graph itself (which resources
  depend on which) would be resolved once at build time, not walked from scratch
  at runtime — but recomputation still has to happen at runtime: if a base
  resource changes (theme switch, a user overriding one color), every value
  computed from it must update too, without hand-writing each derived shade in
  the theme. Only worth the complexity once resource-based theming (already shipped —
  `bg-*`/`text-*`/`border-*` are `DynamicResource`-backed) is actually in wide use.

### v3 — runtime parity

- **A runtime connector for resource-based theming.** A connector that detects
  newly-used classes across loaded components as they appear and resolves/emits
  what they need live — chiefly the resource dictionary entries
  `bg-*`/`text-*`/`border-*` resolve against — bringing that last codegen-only feature
  to the runtime path (`AtomicGenerator<TTheme>.Generate(...)`).

Have an opinion on any of these, or a different priority? Open an issue.

## License

[MIT](LICENSE)

## Contributing

This repository follows the org-wide
[contributing guidelines](https://github.com/TheKeyblader/.github/blob/main/CONTRIBUTING.md).
