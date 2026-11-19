# Enx.Atomic.Avalonia

## Project Context

An atomic/utility-class styling engine for [Avalonia](https://avaloniaui.net/), inspired by
[UnoCSS](https://unocss.dev/): resolves Tailwind/UnoCSS-style tokens (`hover:bg-red-500`,
`sm:flex-row`, `ml-4`, `bg-[#ff0000]`, ...) into real Avalonia `Style`s, primarily at build time
via a source-generation pipeline (`Enx.Atomic.Avalonia.CodeGen`). Three packages ship to NuGet:
the core engine, the `Preset.Mini` rule/variant set (`@unocss/preset-mini` ported to Avalonia
concepts), and the codegen tooling.

**Runtime resolution (`AtomicGenerator<TTheme>.Generate(...)`) is a sandbox/demo path, not a
fully-supported alternative** — grid utilities and resource-based theming only work through
codegen and silently resolve to nothing at runtime (deliberate, not a bug — see
`ARCHITECTURE.md#runtime-resolution` and the root README's Roadmap, v3, which is where real
runtime parity — a connector detecting newly-used classes live, not a one-shot source scan —
would eventually land, deliberately last). Don't "fix" this gap by adding ad hoc runtime support
for one feature; it needs the connector or nothing.

**This is not a typical business-rules backend** — none of dotnet-claude-kit's four
architectures (VSA / Clean Architecture / DDD / Modular Monolith) apply. It's closer to a small
compiler: tokens → matched rules/variants → `StyleValue`s → emitted C# source (build time) or,
for the subset that supports it, compiled `Selector` expressions (runtime — see above). The
actual architecture — rules, variants, source transformers, ghost properties, the codegen
pipeline — is documented in **[ARCHITECTURE.md](ARCHITECTURE.md)**, which is the source of
truth; don't duplicate it here, read it before making non-trivial changes.

## Repo Layout

```
Sources/
  Enx.Atomic.Avalonia/              # Core engine — rules, variants, StyleValue, AtomicGenerator<TTheme>
  Enx.Atomic.Avalonia.Preset.Mini/  # Concrete rule/variant set (Static/, Dynamic/, Variants/, Parts/)
  Enx.Atomic.Avalonia.CodeGen/      # Build-time C# emitter, AtomicCli (Spectre.Console.Cli), MSBuild .targets
Tests/Enx.Atomic.Avalonia.Tests/    # xUnit v3 + Avalonia.Headless.XUnit, single test project for everything
Examples/                           # End-to-end build-time codegen example (config project + consuming app)
Sandbox/                            # Manual runtime-resolution test bed
```

Every `Sources/*` project targets `net10.0`, `<ImplicitUsings>enable</ImplicitUsings>`,
`<Nullable>enable</Nullable>`, and references `Roslynator.Analyzers`. `Directory.Build.props`
centralizes package metadata; `MinVer` derives the version from the nearest `vX.Y.Z` git tag —
no project hand-maintains a version number.

## Coding Standards

- **XML doc comments on public members** — this codebase documents *why*, not *what*
  (`<summary>` explaining rationale/gotchas, not restating the signature). Follow the existing
  density: public engine types are thoroughly documented; private helpers are not.
- **File-scoped namespaces**, always.
- **No regions.**
- **Extend-only for the two shipped presets/engine** — this is still pre-1.0 (`0.0.x`), so
  breaking a public member is acceptable when there's a real reason (see the `csharp-api-design`
  skill), but check whether the old surface is actually load-bearing (used by a real rule/test)
  before assuming a break is low-risk — it usually is, since most consumer-facing surface is
  exercised by the shipped `Preset.Mini` itself.
- **Never decompile a compiled `Expression`** — the codegen pipeline (`StyleEmitter`,
  `SelectorEmitter`, `StyleQueryEmitter`) works entirely off `SelectorExpression`/
  `StyleQueryExpression` data trees built by rules/variants, never by compiling then
  reverse-engineering an `Expression<T>`. `StyleValue.Resource.ThemeAccess` is the one place an
  `Expression` is compiled — and only *invoked*, never decompiled to text.
- **`internal` by default for implementation plumbing, even in a NuGet-published project** — a
  new top-level type only stays `public` if it's a genuine, documented extension point (e.g.
  `IValueEmitter` and its implementations, since `StyleEmitter.Emit`/`ValueEmitterRegistry` both
  accept a caller-supplied `IEnumerable<IValueEmitter>`). Naming/formatting helpers, reflection
  lookups, and anything with no plausible external call site (`CSharpLiteral`,
  `AvaloniaPropertyNaming`, `SelectorEmitter`, ...) should be `internal`. A `/health-check` run
  already caught and fixed a batch of these in `Enx.Atomic.Avalonia.CodeGen` — don't
  re-introduce the pattern of defaulting new emitter/naming helpers to `public`.

## Workflow Conventions (established, don't relitigate)

- **Never `git commit`/`git push` without an explicit request in that turn** — even immediately
  after implementing a feature the user clearly wants shipped. Ask, or wait to be told
  "commit et push" (or the English equivalent). This was corrected once already; don't
  re-introduce it.
- **Never commit `Examples/Enx.Atomic.Avalonia.Example.App/MainWindow.axaml`** unless explicitly
  asked — the maintainer edits it directly as a manual/live test bed, and those edits are his,
  not generated output.
- **For a new roadmap-sized feature, discuss the design in conversation first** (see this
  session's resource-theming and arbitrary-values features) before writing code — the maintainer
  iterates on API shape (e.g. `Themed<T>`, auto-derived resource keys) through discussion, not
  through a written plan doc.
- **Update `ARCHITECTURE.md`/root `README.md`/`CHANGELOG.md`/`Preset.Mini/README.md` only when
  asked** ("mets à jour ..."), not proactively after every change — but when asked, update all
  four together, keep the CHANGELOG's `[Unreleased]` section current, and keep the root
  README's Roadmap in sync (remove a bullet once it ships).
- **Verify before reporting done**: `dotnet build Enx.Atomic.Avalonia.slnx` and
  `dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj` must both be
  clean. New engine mechanisms get real regression tests (a Roslyn compile-and-run check for
  anything touching codegen, mirroring `CodeGenTests.cs`/`GridRuleTests.cs`), not just
  hand-verification.
- Communication in this repo's sessions is primarily in French; code, comments, docs, and commit
  messages stay in English.

## MCP Tools (cwm-roslyn-navigator)

Available via the `dotnet-claude-kit` plugin. Prefer these over grep for anything touching public
API surface or cross-project usage:

- `find_symbol` / `get_public_api` before modifying a type.
- `find_references` / `find_implementations` before touching a shared type like `StyleValue`,
  `IValueEmitter`, or `IDynamicRule<TTheme>` — several presets/emitters key off these.
- `get_project_graph` to confirm a change doesn't introduce a dependency cycle between
  `Enx.Atomic.Avalonia` → `Preset.Mini` → `CodeGen` (that's the intended direction; `CodeGen`
  never references `Preset.Mini`, since generated code must compile standalone — see
  `EmittableGhostPropertyHostAttribute` in `ARCHITECTURE.md`).
- `get_diagnostics` after non-trivial edits, same role as `dotnet build`/an IDE.

## Commands

```bash
# Build everything
dotnet build Enx.Atomic.Avalonia.slnx

# Run the full test suite
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj

# With coverage (Cobertura report under coverage/)
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj \
  /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../coverage/
```

Releasing is tag-driven (`vX.Y.Z` → `.github/workflows/release.yml` → NuGet via Trusted
Publishing) — see `ARCHITECTURE.md#release-pipeline`. Don't hand-push a tag without being asked;
same rule as commits.
