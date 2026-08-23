# Enx.Atomic.Avalonia

[![CI](https://github.com/TheKeyblader/Enx.Atomic.Avalonia/actions/workflows/ci.yml/badge.svg)](https://github.com/TheKeyblader/Enx.Atomic.Avalonia/actions/workflows/ci.yml)

An atomic utility-class engine for [Avalonia](https://avaloniaui.net/), inspired by
[UnoCSS](https://unocss.dev/): resolve Tailwind/UnoCSS-style tokens
(`hover:bg-red-500`, `sm:flex-row`, `ml-4`, ...) into real Avalonia styles instead of
hand-written XAML `Style`s.

See [ARCHITECTURE.md](ARCHITECTURE.md) for how the engine actually works — rules,
variants, source transformers, the Mini preset, and the planned build-time codegen.

## Projects

| Project | Role |
|---|---|
| `Sources/Enx.Atomic.Avalonia` | The generic engine: resolves tokens into styles, independent of any preset. |
| `Sources/Enx.Atomic.Avalonia.Preset.Mini` | A concrete rule/variant set, inspired by `@unocss/preset-mini`. |
| `Sandbox` | An Avalonia console app used as a manual test bed for the engine and the presets. |
| `Tests/Enx.Atomic.Avalonia.Tests` | The automated test suite (xUnit v3). |

## Getting started

Requires a .NET SDK compatible with `net10.0` (a `net10.0`-targeting preview SDK
works too).

```bash
dotnet build Enx.Atomic.Avalonia.slnx
```

## Testing

```bash
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj
```

With code coverage (Cobertura report under `coverage/`):

```bash
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj \
  /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../coverage/
```

## Contributing

This repository follows the org-wide
[contributing guidelines](https://github.com/TheKeyblader/.github/blob/main/CONTRIBUTING.md).
