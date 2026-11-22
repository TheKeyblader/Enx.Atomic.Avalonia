---
name: Debug a style / visual check
description: A utility doesn't apply, the wrong one wins, or a change must be checked in a running app. Narrows it down with probe (matching, emission order, effective values) before launching the app, then drives the Example app through the agent-desktop MCP (snapshot, screenshot of the app window only). Read before any agent-desktop call.
---

# Debug a style / visual check

Work from the cheapest check to the most expensive. Most "it doesn't apply" reports are settled at
step 1 or 2 without launching anything.

## 1. Does the token match?

```bash
dotnet run --project Examples/Enx.Atomic.Avalonia.Example.Config -- probe "<tokens>"
```

(`/probe` explains how to read the output.) Check `variants:`/`base:`/`rule:`. A `hint:` or no style
at all means a typo, an unknown variant, or a token Preset.Mini doesn't have. Check
`Sources/Enx.Atomic.Avalonia.Preset.Mini/README.md` before assuming a rule is broken. A wrong
`rule:` on a shared prefix (`border-2` vs `border-red-500`) means a registration-order problem, which
belongs in `AmbiguousPrefixTests.cs`.

## 2. Does it lose the cascade?

Probe the **whole** class list of the element, not just the suspect token: `style n/N` positions
are relative to the probed set, and the last inserted frame wins at equal priority. Compare the
`key:` parts (specificity → layer → container order → rule index → rank → variant sort). A shortcut
loses to a plain utility of equal specificity by design (layer).

## 3. What value does Avalonia end up with?

```bash
dotnet run --project Examples/Enx.Atomic.Avalonia.Example.Config -- probe "<tokens>" --control Button --pseudo :pointerover --width 700
```

`control:` shows the effective values, including the recomposed `Margin`/`Padding`/`BorderThickness`/
`CornerRadius` and their sides. If the value is right here but wrong in the app, the cause is in the app,
not in the engine. The usual suspects:

- **A local value** (`Margin="8"` written on the element) beats every style.
- **The control theme or template** sets the property on a template part (Fluent's
  `/template/ ContentPresenter#PART_ContentPresenter` for `Button` background on `:pointerover`), so
  a style on the control itself is overridden visually. Probe can't show this. Check the theme's
  styles in the `avalonia` reference.
- **The property doesn't exist or isn't used** on that control (`p-2` on a `StackPanel`, which has
  no `Padding`).
- **Breakpoints** (`sm:`, `max-sm:`) are container queries. They need an ancestor with
  `Container.Name="top-level"` (the Example app's `MainWindow` has one) and react to its width, not
  the window's.
- **Resource colors** (`bg-*`/`text-*`/`border-*`) only resolve through codegen. On the runtime
  path (`AtomicGenerator.Generate`, exercised by the console-only `Sandbox/`), resolving nothing is
  expected.
- **Generated code is stale**: `GeneratedStyles/` is rebuilt on every build. Rebuild the app.

Once the cause is found, write the regression test (`[AvaloniaFact]`, or a codegen compile-and-run
test) **before** fixing. Turn the probed tokens into the test's source.

## 4. Check the running app (agent-desktop)

Only for what probe can't show: templates, real layout, pointer-over and focus reactions, visual
rendering. It doesn't replace tests: a regression still needs an `[AvaloniaFact]` or codegen test.

[agent-desktop](https://github.com/vercel-labs/agent-desktop) (`agent-desktop mcp`, configured in
`opencode.jsonc`) is installed globally on the maintainer's machine, **not** pinned in
`dotnet-tools.json`. If it isn't connected, say so instead of installing it. It reads a running
app's accessibility tree (AT-SPI on Linux) and acts on element refs. Tools are named after the CLI
commands (`desktop_snapshot`, `desktop_click`, ...) under `tools["agent-desktop"].*`. Discover them
with `search({ namespace: "agent-desktop" })`.

- **Read the guides first**, once per session: `desktop_skills_get` with `name: "desktop"`, then
  `name: "platform"` (they ship with the binary and match its version). They use CLI syntax:
  `agent-desktop snapshot --app X` is `desktop_snapshot({ app: "X" })`.
- **Launch** `Examples/Enx.Atomic.Avalonia.Example.App` with `dotnet run --project ...` in the
  background (it's the only app with a window: `Sandbox/` is a console test bed that prints and exits), then snapshot the window and check what you need: a visible element, an
  enabled state, a pointer-over or focus reaction.
- **Refs belong to their snapshot** (`@<snapshot>:eN`). After any UI change, take a new snapshot
  instead of reusing old refs. Retry only when `error.disposition.retry` is `safe`.
- It's headless/semantic by default. Global flags (`--headed`, `--session`, `--wait-for`, `--trace`)
  are fixed by the `command` in `opencode.jsonc`, not passed per call. Commands that need physical
  input (`hover`, `drag`, `mouse-*`, `double-click`) return `POLICY_DENIED` as configured. Don't add
  `--headed` to the config without the user's go-ahead, since it moves their real cursor.
- **Linux/KDE Wayland**: the server runs with `WAYLAND_DISPLAY` blanked (see `opencode.jsonc`), so
  it uses X11 against the Xwayland-hosted Avalonia window. That is what makes
  `desktop_screenshot({ app: "<process name>", path: "/tmp/..." })` capture just the app window.
  Read the PNG to check colors, spacing and rounded corners: the visual check this library needs.
  **Never take a screenshot without `app`/`window_id`**: it captures the user's whole desktop.
- `desktop_press({ combo, app })` only reaches the app while it owns the active window (true right
  after launch, lost as soon as the user clicks elsewhere), and KWin refuses `focus-window`/`focus`
  to take it back. If `press` fails with `display_active_window`, don't fight it: drive the UI
  through semantic ref actions (`desktop_type`, `desktop_click`, `desktop_check`), and keep
  Enter/shortcut handlers covered by an `[AvaloniaFact]` anyway.
- **Deep trees**: list items (`ItemsControl` children) can be cut by the default snapshot depth.
  Take a second `desktop_snapshot({ app, root: "<list ref>" })` to reach them. `desktop_type`
  inserts at the caret, it doesn't replace: `desktop_clear` first if the field may hold text.
- On Linux a ref must be unique in its window: identical controls (one unnamed `CheckBox` per list
  item) are refused with `AMBIGUOUS_TARGET`. Test with a single item, or give each control an
  accessible name (`AutomationProperties.Name`) in the app under test.
- Snapshots and screenshots can show private desktop content. Keep them to the app under test and
  out of commits and logs.
- **Stop the app** when you're done with `desktop_close_app`. Don't use `pkill -f <pattern>` from
  the shell tool: the pattern appears in the tool's own command line, so it kills its own shell
  (exit code 143).
