# Enx.Atomic.Avalonia

[![CI](https://github.com/TheKeyblader/Enx.Atomic.Avalonia/actions/workflows/ci.yml/badge.svg)](https://github.com/TheKeyblader/Enx.Atomic.Avalonia/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/TheKeyblader/Enx.Atomic.Avalonia/graph/badge.svg)](https://codecov.io/gh/TheKeyblader/Enx.Atomic.Avalonia)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

An atomic utility-class engine for [Avalonia](https://avaloniaui.net/), inspired by
[UnoCSS](https://unocss.dev/): resolve Tailwind/UnoCSS-style tokens
(`hover:bg-red-500`, `sm:flex-row`, `ml-4`, ...) into real Avalonia styles instead of
hand-written XAML `Style`s.

```xml
<Button Classes="flex-row p-4 rounded-md bg-blue-500 hover:bg-blue-600" Content="Click me" />
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for how the engine actually works — rules,
variants, source transformers, the Mini preset, and build-time code generation.

## Projects

| Project | Role |
|---|---|
| `Sources/Enx.Atomic.Avalonia` | The generic engine: resolves tokens into styles, independent of any preset. |
| `Sources/Enx.Atomic.Avalonia.Preset.Mini` | A concrete rule/variant set, inspired by `@unocss/preset-mini`. |
| `Sources/Enx.Atomic.Avalonia.CodeGen` | Build-time C# code generation: emitter, CLI, and MSBuild wiring. |
| `Sandbox` | An Avalonia console app used as a manual test bed for the engine and the presets. |
| `Examples/` | A minimal end-to-end example of the build-time code generation pipeline. |
| `Tests/Enx.Atomic.Avalonia.Tests` | The automated test suite (xUnit v3). |

## Getting started

Requires a .NET SDK compatible with `net10.0` (a `net10.0`-targeting preview SDK
works too).

```bash
dotnet build Enx.Atomic.Avalonia.slnx
```

There are two ways to resolve utility tokens into styles:

- **At runtime** — call `AtomicGenerator<TTheme>.Generate(...)` yourself and add the
  resulting styles to your `Application.Styles`. See `Sandbox/Program.cs`.
- **At build time** — set up a small configuration project and wire it into your
  app's build via `Enx.Atomic.Avalonia.CodeGen`, so tokens are resolved once at
  compile time instead of on every startup. See `Examples/` and
  [ARCHITECTURE.md](ARCHITECTURE.md#build-time-c-code-generation) for the full setup.

## Testing

```bash
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj
```

With code coverage (Cobertura report under `coverage/`):

```bash
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj \
  /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../coverage/
```

## Releasing

`Enx.Atomic.Avalonia`, `Enx.Atomic.Avalonia.Preset.Mini`, and
`Enx.Atomic.Avalonia.CodeGen` are published to nuget.org whenever a `vX.Y.Z` tag is
pushed — see [ARCHITECTURE.md](ARCHITECTURE.md#release-pipeline) for how versioning
and publishing work, and [CHANGELOG.md](CHANGELOG.md) for what shipped in each
release.

## License

[MIT](LICENSE)

## Contributing

This repository follows the org-wide
[contributing guidelines](https://github.com/TheKeyblader/.github/blob/main/CONTRIBUTING.md).
