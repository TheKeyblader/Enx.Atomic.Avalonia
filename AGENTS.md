# Enx.Atomic.Avalonia

## Project Context

An atomic/utility-class styling engine for [Avalonia](https://avaloniaui.net/), inspired by
[UnoCSS](https://unocss.dev/): resolves Tailwind/UnoCSS-style tokens (`hover:bg-red-500`,
`sm:flex-row`, `ml-4`, `bg-[#ff0000]`, ...) into real Avalonia `Style`s, primarily at build time
via a source-generation pipeline (`Enx.Atomic.Avalonia.CodeGen`). Three packages ship to NuGet:
the core engine, the `Preset.Mini` rule/variant set (`@unocss/preset-mini` ported to Avalonia
concepts), and the codegen tooling.

**Runtime resolution (`AtomicGenerator<TTheme>.Generate(...)`) is a sandbox/demo path, not a
fully-supported alternative** — resource-based theming (`bg-*`/`text-*`/`border-*` colors) only
works through codegen and silently resolves to nothing at runtime (deliberate, not a bug — see
`ARCHITECTURE.md#runtime-resolution` and the root README's Roadmap, v3, which is where real
runtime parity — a connector detecting newly-used classes live, not a one-shot source scan —
would eventually land, deliberately last). Grid utilities and composite side properties work on
both paths. Don't "fix" the remaining gap by adding ad hoc runtime support for it; it needs the
connector or nothing.

**This is not a typical business-rules backend** — none of the usual application
architectures (VSA / Clean Architecture / DDD / Modular Monolith) apply. It's closer to a small
compiler: tokens → matched rules/variants → `StyleValue`s → emitted C# source (build time) or,
for the subset that supports it, compiled `Selector` expressions (runtime — see above). The
actual architecture — rules, variants, source transformers, emission order, composite
side properties, the codegen pipeline — is documented in **[ARCHITECTURE.md](ARCHITECTURE.md)**,
which is the source of truth; don't duplicate it here, read it before making non-trivial
changes.

## Repo Layout

```
Sources/
  Enx.Atomic.Avalonia/              # Core engine — rules, variants, StyleValue, AtomicGenerator<TTheme>
  Enx.Atomic.Avalonia.Preset.Mini/  # Concrete rule/variant set (Static/, Dynamic/, Variants/, Parts/)
  Enx.Atomic.Avalonia.CodeGen/      # Build-time C# emitter, AtomicCli (Spectre.Console.Cli), MSBuild .targets
Tests/Enx.Atomic.Avalonia.Tests/    # xUnit v3 + Avalonia.Headless.XUnit, single test project for everything
Examples/                           # End-to-end build-time codegen example (config project + consuming app)
Sandbox/                            # Manual runtime-resolution test bed
.opencode/skills/                   # Step-by-step workflows (add a rule/variant/emitter, port from UnoCSS)
.opencode/commands/                 # /verify, /update-docs
opencode.jsonc                      # Permissions, CSharpier formatter, Roslyn + agent-desktop MCP, upstream references
```

Every `Sources/*` project targets `net10.0`, `<ImplicitUsings>enable</ImplicitUsings>`,
`<Nullable>enable</Nullable>`, and references `Roslynator.Analyzers`. `Directory.Build.props`
centralizes package metadata and turns on `EnforceCodeStyleInBuild` (so `.editorconfig`'s style
rules show up as build warnings); `MinVer` derives the version from the nearest `vX.Y.Z` git tag —
no project hand-maintains a version number. `global.json` pins the .NET 10 SDK (`latestFeature`
roll-forward) — a newer major SDK installed side by side is deliberately not picked up. Avalonia is
on **12.x**.

**Where to look things up** — don't guess a token name, an Avalonia API, or an UnoCSS behavior:

- Every token Preset.Mini registers, and the Avalonia property it sets:
  `Sources/Enx.Atomic.Avalonia.Preset.Mini/README.md`. Check it before adding a "new" rule.
- UnoCSS upstream (the `unocss` reference in `opencode.jsonc`):
  `packages-presets/preset-mini/src/{_rules,_theme,_variants}`, `packages-presets/rule-utils`.
- Avalonia source at the referenced version (`avalonia` reference, branch `release/12.1.3`) and its
  docs (`avalonia-docs` reference) — for `Selector`/`StyleQuery`/`AddOwner`/`DynamicResource`
  semantics.

## Known Pitfalls

Each of these has already cost a debugging session. Details in `ARCHITECTURE.md`.

- **Static beats dynamic; among dynamic rules, registration order wins.** Two rules sharing a
  prefix (`border-2` width vs `border-red-500` color, `text-lg` size vs `text-red-500` color) rely
  on `AddMiniTheme` registering the more specific one first. A new rule on an existing prefix
  needs a case in `AmbiguousPrefixTests.cs`.
- **Per-side utilities write composite host side properties, never the real struct
  property.** `m-*`/`p-*`/`border-*` (width)/`rounded-*` target the
  `AttachedProperty<double>` sides of `MarginSides`/`PaddingSides`/`BorderThicknessSides`/
  `CornerRadiusSides` (`NaN` = unset), and the host's class handler recomposes the real
  `Thickness`/`CornerRadius` at `BindingPriority.Style`; writing the real property from
  two styles would let the later one replace the whole struct. Pass `typeof(Control)` as
  the target type (the property's `OwnerType` is the static host, i.e. a meaningless
  `<MarginSides>` selector). The scales are `double`, the side properties' type, so the resolved
  value boxes correctly as-is.
  UnoCSS's direction order (uniform < axis/edge < side/corner) comes from
  `IOrderRankedDynamicRule.GetOrderRank`, not from registration order.
- **Emission order is the cascade.** A class selector is an activator, so Avalonia frames
  every utility at `BindingPriority.StyleTrigger` and resolves equal-priority frames
  last-inserted-wins: in source order `hover:w-8 w-4` would lose its hover.
  `StyleOrdering.Sort` owns the order (specificity → layer → container order → rule index →
  rule rank → variant sort → selector text) on both the runtime and CLI paths; the layer puts
  shortcut-expanded styles before plain utilities. Never rely on
  source order, and give a new rule that shares a family's prefix a rank rather than
  assuming where it's registered will sort it.
- **`AddOwner` hides the declaring type.** `AvaloniaProperty.OwnerType` reports the *original*
  (sometimes non-public) owner, so selectors/emitters that need the public declaring type must be
  given it explicitly (see `StyleValue.TargetType`, `DarkVariant`).
- **Boxed numeric types must match exactly.** `Setter.Value` is `object?`: a `float` or `int`
  for a `double` property compiles in emitted code and only fails when Avalonia applies it.
  The Mini theme scales are `double` (the property type), so scale values need no conversion —
  but a rule that computes its own value must still box the exact type. `CodeGenTests` exists
  to catch this; extend it rather than relying on a source-text check.
- **Arbitrary values (`[...]`) are always `StyleValue.Literal`, never `Resource`**, always pixels,
  never rem-converted; the bare-number fallback is quarter-rems like UnoCSS (`TryResolve`:
  `w-4` = 1rem = 16px) except line widths, which are raw px (`TryResolvePx`).
- **Static init needs Avalonia's headless platform.** Some static rules (`Cursors`) construct real
  Avalonia types; a config project or test touching `AddMiniTheme` must bootstrap headless first
  (tests use `[AvaloniaFact]`, not `[Fact]`).
- **`CodeGen` never references `Preset.Mini`** — generated code must compile standalone.
- **Resource theming is codegen-only** (see Project Context) — a runtime-path test for a
  `bg-*`/`text-*`/`border-*` color asserting "nothing resolves" is correct, not a bug. Grid
  utilities work on both paths (`GridDefinitions` registers its handlers in a static constructor).
- **`GeneratedStyles/` is output**, regenerated on every build (edits are denied in
  `opencode.jsonc`). Fix the rule or the emitter, never the `.g.cs`.

## Coding Standards

- **XML doc comments on public members** — this codebase documents *why*, not *what*
  (`<summary>` explaining rationale/gotchas, not restating the signature). Follow the existing
  density: public engine types are thoroughly documented; private helpers are not.
- **File-scoped namespaces**, always.
- **No regions.**
- **Extend-only for the two shipped presets/engine** — this is still pre-1.0 (`0.0.x`), so
  breaking a public member is acceptable when there's a real reason, but check whether the old surface is actually load-bearing (used by a real rule/test)
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
  `AvaloniaPropertyNaming`, `SelectorEmitter`, ...) should be `internal`. A past review
  already caught and fixed a batch of these in `Enx.Atomic.Avalonia.CodeGen` — don't
  re-introduce the pattern of defaulting new emitter/naming helpers to `public`.
- **Declare public API changes.** Add each new or changed public member to the project's
  `PublicAPI.Unshipped.txt`; otherwise the build fails with RS0016. That's intentional: ask
  "should this be public?" before adding the entry. Move Unshipped entries to Shipped at release.

## Workflow Conventions (established, don't relitigate)

- **Never `git commit`/`git push` without an explicit request in that turn** — even immediately
  after implementing a feature the user clearly wants shipped. Ask, or wait to be told
  "commit et push" (or the English equivalent). This was corrected once already; don't
  re-introduce it.
- **For a new roadmap-sized feature, discuss the design in conversation first** (see this
  session's resource-theming and arbitrary-values features) before writing code — the maintainer
  iterates on API shape (e.g. `Themed<T>`, auto-derived resource keys) through discussion, not
  through a written plan doc.
- **Update `ARCHITECTURE.md`/root `README.md`/`CHANGELOG.md`/`Preset.Mini/README.md` only when
  asked** ("mets à jour ..."), not proactively after every change — but when asked, update all
  four together, keep the CHANGELOG's `[Unreleased]` section current, and keep the root
  README's Roadmap in sync (remove a bullet once it ships).
- **Verify before reporting done** (`/verify` runs all three): `dotnet build Enx.Atomic.Avalonia.slnx`
  with no warnings, `dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj`
  green, and `dotnet csharpier check .` clean (CI enforces it). New engine mechanisms get real
  regression tests (a Roslyn compile-and-run check for anything touching codegen, mirroring
  `CodeGenTests.cs`/`GridRuleTests.cs`), not just hand-verification.
- Communication in this repo's sessions is primarily in French; code, comments, docs, and commit
  messages stay in English.

## MCP Tools (`roslyn` server — cwm-roslyn-navigator)

Configured in `opencode.jsonc` as the `roslyn` MCP server, pinned as a local tool in
`dotnet-tools.json` (run `dotnet tool restore` once after cloning). Under Code Mode its tools are
under `tools.roslyn.*` — discover exact signatures with `search({ namespace: "roslyn" })`. Prefer
them over grep for anything touching public API surface or cross-project usage:

- `find_symbol` / `get_public_api` / `get_symbol_source` before modifying a type.
- `find_references` / `find_implementations` / `find_callers` before touching a shared type like
  `StyleValue`, `IValueEmitter`, or `IDynamicRule<TTheme>` — several presets/emitters key off these.
- `get_project_graph` / `detect_circular_dependencies` to confirm a change doesn't break the
  intended direction `Enx.Atomic.Avalonia` → `Preset.Mini`, `Enx.Atomic.Avalonia` → `CodeGen`
  (`CodeGen` never references `Preset.Mini`, since generated code must compile standalone — see
  `EmittableGhostPropertyHostAttribute` in `ARCHITECTURE.md`).
- `get_diagnostics` after non-trivial edits, same role as `dotnet build`/an IDE.

If the server isn't connected (it loads the whole solution, which takes a few seconds), fall back
to `dotnet build` + grep rather than skipping the check.

## MCP Tools (`agent-desktop` server — desktop computer use)

[agent-desktop](https://github.com/vercel-labs/agent-desktop) (`agent-desktop mcp`), configured in
`opencode.jsonc`. It's installed globally on the maintainer's machine, **not** pinned in
`dotnet-tools.json`; if it isn't connected, say so instead of installing it. It reads a running app's
accessibility tree (AT-SPI on Linux) and acts on element refs. Tools are named after the CLI
commands (`desktop_snapshot`, `desktop_click`, ...) under `tools["agent-desktop"].*` — discover them
with `search({ namespace: "agent-desktop" })`.

- **Read the guides first**, once per session: `desktop_skills_get` with `name: "desktop"`, then
  `name: "platform"` (they ship with the binary and match its version). They use CLI syntax:
  `agent-desktop snapshot --app X` is `desktop_snapshot({ app: "X" })`.
- **Use it to check a running app, not instead of tests**: launch `Sandbox/` or
  `Examples/Enx.Atomic.Avalonia.Example.App` (`dotnet run --project ...`, in the background), snapshot the
  window, and check that a utility actually applied (a visible element, an enabled state, a
  pointer-over or focus reaction). An Avalonia regression still needs an `[AvaloniaFact]`/codegen test.
- **Refs belong to their snapshot** (`@<snapshot>:eN`). After any UI change, take a new snapshot
  instead of reusing old refs. Retry only when `error.disposition.retry` is `safe`.
- It's headless/semantic by default. Global flags (`--headed`, `--session`, `--wait-for`, `--trace`)
  are fixed by the `command` in `opencode.jsonc`, not passed per call. Commands that need physical
  input (`hover`, `drag`, `mouse-*`, `double-click`) return `POLICY_DENIED` as configured. Don't add
  `--headed` to the config without the user's go-ahead, since it moves their real cursor.
- **Linux/KDE Wayland**: the server runs with `WAYLAND_DISPLAY` blanked (see `opencode.jsonc`) so
  it uses X11 against the Xwayland-hosted Avalonia window. That is what makes
  `desktop_screenshot({ app: "<process name>", path: "/tmp/..." })` capture just the app window
  (read the PNG to check colors, spacing, rounded corners: the visual check this library needs).
  Never take a screenshot without `app`/`window_id`: it captures the user's whole desktop.
  `desktop_press({ combo, app })` only reaches the app while it owns the active window (true right
  after launch, lost as soon as the user clicks elsewhere), and KWin refuses `focus-window`/`focus`
  to take it back. If `press` fails with `display_active_window`, don't fight it: drive the UI through
  semantic ref actions (`desktop_type`, `desktop_click`, `desktop_check`), and keep Enter/shortcut
  handlers covered by an `[AvaloniaFact]` anyway.
- **Deep trees**: list items (`ItemsControl` children) can be cut by the default snapshot depth.
  Take a second `desktop_snapshot({ app, root: "<list ref>" })` to reach them. `desktop_type`
  inserts at the caret, it doesn't replace: `desktop_clear` first if the field may hold text.
  On Linux a ref must be unique in its window: identical controls (one unnamed `CheckBox` per list
  item) are refused with `AMBIGUOUS_TARGET`. Test with a single item, or give each control an
  accessible name (`AutomationProperties.Name`) in the app under test.
- Snapshots and screenshots can show private desktop content. Keep them to the app under test and
  out of commits and logs.

## Skills & Commands

Load the matching skill before starting one of these tasks — each encodes the files to touch,
the registration order, and the tests expected:

| Skill | When |
|---|---|
| `add-static-rule` | A fixed-name token (`cursor-pointer`, `truncate`) |
| `add-dynamic-rule` | A regex/theme-driven token family (`m-*`, `bg-*`, `rounded-tl-*`) |
| `add-variant` | A prefix like `hover:`, `sm:`, `dark:` |
| `add-value-emitter` | Codegen fails with `No value emitter registered for type ...` |
| `port-unocss-rule` | Bringing an upstream `@unocss/preset-mini` rule/variant/scale over |

Commands: `/verify` (build + test + format check), `/probe` (resolve tokens and explain matching,
emission order and effective values, `--json` for agents), `/update-docs` (the four docs together, when
asked).

## Commands

```bash
# Once after cloning: CSharpier + the Roslyn MCP server (pinned in dotnet-tools.json)
dotnet tool restore

# Format (OpenCode runs this automatically after each edit) / check, as CI does
dotnet csharpier format .
dotnet csharpier check .

# Build everything
dotnet build Enx.Atomic.Avalonia.slnx

# Run the full test suite
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj

# Probe tokens against the example configuration (add --control Button, --pseudo :pointerover, --json)
dotnet run --project Examples/Enx.Atomic.Avalonia.Example.Config -- probe "hover:ml-4 p-4"

# With coverage (Cobertura report under coverage/)
dotnet test Tests/Enx.Atomic.Avalonia.Tests/Enx.Atomic.Avalonia.Tests.csproj \
  /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=../../coverage/
```

Releasing is tag-driven (`vX.Y.Z` → `.github/workflows/release.yml` → NuGet via Trusted
Publishing) — see `ARCHITECTURE.md#release-pipeline`. Don't hand-push a tag without being asked;
same rule as commits.
