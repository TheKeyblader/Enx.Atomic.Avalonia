# Preset.Mini parity matrix

A family-by-family comparison between
[`@unocss/preset-mini`](https://github.com/unocss/unocss/tree/main/packages-presets/preset-mini)
(upstream, read from `unocss/unocss@main`: `packages-presets/preset-mini/src/{_rules,_variants,_theme}`)
and this port. It exists to answer "does token X work here, and if not, why?" without reading two
codebases — and to keep that answer from rotting.

**Granularity.** One row per token *family* a user would recognize (`m-*`, `mx-*`, `border-x-*`,
`rounded-tl-*`, ...), not per regex alternative. This port often folds several upstream rule entries
into one C# rule (UnoCSS's separate uniform/axis/side entries become one regex plus
`IOrderRankedDynamicRule.GetOrderRank`), so the rows describe families, and the `Here` column names
the class that implements them.

## Status legend

| Status | Meaning |
|---|---|
| ✅ ported | The whole family resolves here, through the normal rule path. |
| 🟡 partial | Some values/forms resolve; the `Notes` column says what is missing. |
| ❌ dropped | Deliberately not ported: either Avalonia has no equivalent (`display`, `float`, CSS variables, `calc()`, `!important`, pseudo-elements, `@supports`, ...) or the family is subsumed by an existing Avalonia default. |
| ⏳ todo | A plausible Avalonia equivalent exists (or the family is a trivial alias) but nothing is registered yet. `Notes` says what it would map to. |

## How this file stays honest

`Tests/Enx.Atomic.Avalonia.Tests/ParityMatrixTests.cs` parses the tables below:

- every ✅/🟡 row must list at least one example token, and every listed token must resolve to ≥1
  style through the Mini preset (`AtomicGenerator.Generate`, i.e. the real extraction path — not a
  hand-call to `ParseToken`), reported per token;
- every ⏳/❌ row must leave the `Examples` column empty, so a row can't claim to be unimplemented
  while advertising working tokens (or vice versa).

So the `Examples` column is not illustrative prose: it is executable. When you port a family, add
the C# type to `Here`, flip the status, add one or two tokens that the test can resolve, and remove
the row from [Next to port](#next-to-port) below.

Resource-backed colors (`bg-*`, `text-*`, `border-*`) are **codegen-only**: `AtomicGenerator.Generate`
returns the style, but only the CLI builds the `ResourceDictionary` its `DynamicResource` lookup resolves
against. `ARCHITECTURE.md#runtime-resolution` explains why that is deliberate, not a bug. Grid utilities
work on both paths.

## Rules

Upstream's registration order lives in `_rules/default.ts`; `_rules/index.ts` only re-exports.
Neither file declares rules of its own, so neither gets rows. The files below are listed in
upstream's order.

### `align.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `vertical-*`, `align-*`, `v-*` (`vertical-align`) | ❌ dropped | — | | `vertical-align` has no Avalonia property; inline baseline alignment is not settable. |
| `text-left/center/right/justify` | ✅ ported | `Static/TextAlign.cs` (`TextAlign`) | `text-left`, `text-center`, `text-right`, `text-justify` | `TextBlock.TextAlignmentProperty`. |
| `text-start` / `text-end` | ❌ dropped | — | | Avalonia's `TextAlignment` enum has `Left`/`Center`/`Right`/`Justify`, no flow-relative `start`/`end`. |
| `text-align-{value}` | ⏳ todo | — | | Alias of the ported `text-{value}` statics; trivial, not registered. |

### `behaviors.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `outline-*` (width / color / offset / style / `outline-none`) | ❌ dropped | — | | Avalonia has no `outline`/`outline-offset`; focus visuals come from each control template's `FocusAdorner`. |
| `appearance-auto` / `appearance-none` | ❌ dropped | — | | Web-only native-control appearance. |
| `will-change-*` | ❌ dropped | — | | A rendering hint with no Avalonia equivalent. |

### `border.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `border`, `border-{width}` (uniform) | ✅ ported | `Dynamic/BorderRules.cs` (`BorderWidthRule<TTheme>`) | `border`, `border-2`, `border-none` | Writes the four `BorderThicknessSides` doubles, recomposed into `Border.BorderThicknessProperty`; unmatched bare numbers are px (`lineWidth`). |
| `border-{width\|size}-{n}` | ⏳ todo | — | | Explicit alias form of the family above; not registered. |
| `border-x-{width}`, `border-y-{width}` | ✅ ported | `Dynamic/BorderRules.cs` (`BorderWidthRule<TTheme>`) | `border-x-2`, `border-y-4` | `BorderThicknessSides` left+right / top+bottom, exactly like the ported uniform and single-side forms; rank 1, between the uniform token (0) and a single side (2). |
| `border-t/r/b/l-{width}` | ✅ ported | `Dynamic/BorderRules.cs` (`BorderWidthRule<TTheme>`) | `border-t-2`, `border-b`, `border-l-2` | One side each; rank 2 keeps a side above the axis (1) and uniform (0) entries. |
| `border-s/e-{width}` | ❌ dropped | — | | Logical sides can't map: `BorderThickness` is physical, and a style cannot branch on `FlowDirection`. |
| `border-block-*` / `border-inline-*` | ❌ dropped | — | | Same physical-only limitation. |
| `border-bs/be/is/ie-*` | ❌ dropped | — | | Same physical-only limitation. |
| `border-{color}` | ✅ ported | `Dynamic/ColorRules.cs` (`BorderColorRule<TTheme>`) | `border-red-500`, `border-[#0000ff]` | `Border.BorderBrushProperty`; a value resolving in `LineWidths` wins as a width first. |
| `border-{x\|y\|t\|r\|b\|l}-{color}` (per-side colors) | ❌ dropped | — | | Avalonia `Border` has a single `BorderBrush`; there are no per-side brushes. |
| `border-opacity-*` | ❌ dropped | — | | The brush carries the alpha; no separate per-color opacity channel (use an ARGB arbitrary value). |
| `rounded-*` (uniform) | ✅ ported | `Dynamic/RadiusRules.cs` (`RoundedRule<TTheme>`) | `rounded-md`, `rounded-full`, `rounded-[6px]` | Four `CornerRadiusSides` doubles; `rounded` alone is not a token — use `rounded-DEFAULT`. |
| `rounded-t/r/b/l-*` | ✅ ported | `Dynamic/RadiusRules.cs` (`RoundedRule<TTheme>`) | `rounded-t-md`, `rounded-b-lg` | Two adjacent corners, rank 1. |
| `rounded-tl/tr/br/bl-*` | ✅ ported | `Dynamic/RadiusRules.cs` (`RoundedRule<TTheme>`) | `rounded-tl-md`, `rounded-br-none` | One corner, rank 2. |
| `rounded-s/e-*` | ❌ dropped | — | | Flow-relative corners don't exist in `CornerRadius`. |
| `rounded-ss/se/es/ee-*` | ❌ dropped | — | | Same physical-only limitation. |
| `border-{style}` (`border-solid`, `border-dashed`, ...) | ❌ dropped | — | | Avalonia `Border` has no border style/dash; a dashed border needs a `Rectangle` shape with `StrokeDashArray`. |
| `b-*`, `b-rounded-*`, `rd-*` | ⏳ todo | — | | Short aliases of the ported families; not registered. |

### `color.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `bg-{color}` | ✅ ported | `Dynamic/ColorRules.cs` (`BackgroundColorRule<TTheme>`) | `bg-blue-500`, `bg-black`, `bg-[#ff0000]` | Three styles per token: `Border`, `TemplatedControl` and `Panel` backgrounds (the same property, three `AddOwner` targets). |
| `bg-[url(...)]`, `bg-[...gradient(...)]`, `bg-[image:...]` | ⏳ todo | — | | `ImageBrush` and gradient brushes exist; parsing CSS `url()`/gradient syntax is a separate design. |
| `bg-[length:...]`, `bg-[position:...]` | ⏳ todo | — | | `ImageBrush.SourceRect`/`DestinationRect`/`Stretch`/`TileMode` are the closest model; mapping CSS `background-size`/`position` needs design. |
| `bg-opacity-*` | ❌ dropped | — | | No separate per-color opacity variable; bake alpha into the brush (`bg-[#80ff0000]`). |
| `opacity-*` / `op-*` | ✅ ported | `Dynamic/OpacityRules.cs` (`OpacityRule<TTheme>`) | `opacity-50`, `op-25`, `opacity-[0.35]` | `Visual.OpacityProperty`; a bare token is a percentage (0–1, so `opacity-0.5` is 0.005), an arbitrary value is taken as written (`[0.35]` fraction, `[50%]` percentage). |
| `color-scheme-*` | ❌ dropped | — | | A web form-control hint; Avalonia uses `ThemeVariant` (see `dark:`). |

### `container.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `@container`, `@container/{name}`, `@container-{normal\|inline-size\|size}` | ⏳ todo | — | | `Container.NameProperty` / `Container.SizingProperty` exist; no rule registers them yet. The engine already emits Avalonia container *queries* (see `BreakpointVariant`). |

### `decoration.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `underline` / `overline` / `line-through` | ✅ ported | `Static/TextDecoration.cs` (`TextDecoration`) | `underline`, `overline`, `line-through` | `Inline.TextDecorationsProperty`. |
| `no-underline` | ✅ ported | `Static/TextDecoration.cs` (`TextDecoration`) | `no-underline` | Sets `null`. |
| `decoration-{underline\|overline\|line-through}` | ⏳ todo | — | | Alias of the line family; trivial. |
| `decoration-none` | ⏳ todo | — | | Alias of `no-underline`; trivial. |
| `(underline\|decoration)-{size}` | ⏳ todo | — | | `TextDecoration.StrokeThickness` exists — needs constructing a `TextDecoration` instance instead of the shared static constants. |
| `(underline\|decoration)-(auto\|from-font)` | ❌ dropped | — | | No auto/from-font thickness model. |
| `(underline\|decoration)-{color}` | ⏳ todo | — | | `TextDecoration.Stroke` (brush); same per-value instance construction. |
| `(underline\|decoration)-offset-*` | ⏳ todo | — | | `TextDecoration.StrokeOffset` + `StrokeOffsetUnit`. |
| `(underline\|decoration)-opacity-*` | ❌ dropped | — | | No separate opacity channel; bake alpha into the decoration brush. |
| `underline-{style}` / `decoration-{style}` (solid / dashed / dotted / wavy) | ⏳ todo | — | | `TextDecoration.StrokeDashArray` covers dashed/dotted; `wavy` has no equivalent. |

### `flex.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `flex` / `inline-flex` / `flex-inline` | ❌ dropped | — | | No `display`; the chosen layout panel decides. |
| `flex-{n}`, `flex-auto`, `flex-initial`, `flex-none`, `flex-[...]` | ❌ dropped | — | | Avalonia has no flex-grow/shrink/basis model; `Grid` star sizing is a different layout. |
| `shrink-*` / `flex-shrink-*` | ❌ dropped | — | | Same missing flex model. |
| `grow-*` / `flex-grow-*` | ❌ dropped | — | | Same missing flex model. |
| `basis-*` / `flex-basis-*` | ❌ dropped | — | | Same missing flex model. |
| `flex-row` / `flex-col` | ✅ ported | `Static/FlexDirection.cs` (`FlexDirection`) | `flex-row`, `flex-col` | `Orientation` on `StackPanel`, `WrapPanel`, `ProgressBar`, `ScrollBar` and `TickBar`. |
| `flex-row-reverse` / `flex-col-reverse` | ❌ dropped | — | | No reversed direction; child order would have to be reversed by hand. |
| `flex-wrap` / `flex-wrap-reverse` / `flex-nowrap` | ❌ dropped | — | | `WrapPanel` always wraps and exposes no wrap mode. |

### `gap.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `gap-*` | ✅ ported | `Dynamic/SpacingRules.cs` (`GapRule<TTheme>`) | `gap-0`, `gap-4` | `StackPanel.SpacingProperty` plus `Grid`/`UniformGrid` row and column spacing. |
| `gap-x-*` | ✅ ported | `Dynamic/SpacingRules.cs` (`GapRule<TTheme>`) | `gap-x-4` | `Grid`/`UniformGrid` `ColumnSpacing` only — `StackPanel` has a single spacing. |
| `gap-y-*` | ✅ ported | `Dynamic/SpacingRules.cs` (`GapRule<TTheme>`) | `gap-y-4` | `Grid`/`UniformGrid` `RowSpacing` only. |
| `gap-col-*` / `gap-row-*` | ⏳ todo | — | | Aliases of `gap-x`/`gap-y`; trivial. |

### `grid.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `grid` / `inline-grid` | ❌ dropped | — | | No `display`; use a `Grid` panel. |
| `(grid-)?(row\|col)-{n}` | ✅ ported | `Dynamic/GridRules.cs` (`GridCellRule<TTheme>`) | `col-2`, `row-2` | Attached `Grid.Column`/`Grid.Row`, 0-based, on the grid child. Codegen-only. |
| `(grid-)?(row\|col)-span-{n}` | ✅ ported | `Dynamic/GridRules.cs` (`GridCellRule<TTheme>`) | `col-span-2`, `row-span-2` | `Grid.ColumnSpanProperty`/`RowSpanProperty`. Codegen-only. |
| `(grid-)?(row\|col)-span-full` | ❌ dropped | — | | `1/-1` needs line-based placement; `Grid` exposes only `Column` + `Span`. |
| `row-start-*` / `row-end-*` / `col-start-*` / `col-end-*` | ❌ dropped | — | | Same line-based limitation. |
| `auto-rows-*` / `auto-cols-*` | ❌ dropped | — | | No implicit-track sizing property. |
| `grid-auto-flow-*` / `auto-flow-*` / `grid-flow-*` | ❌ dropped | — | | No auto-placement modes. |
| `grid-rows-{n}` / `grid-cols-{n}` | ✅ ported | `Dynamic/GridRules.cs` (`GridColumnsRule<TTheme>`, `GridRowsRule<TTheme>`) | `grid-cols-3`, `grid-rows-2` | `n` equal `*` tracks through a ghost attached property + class handler. Codegen-only. |
| `grid-rows/cols-[...]` / `-minmax-*` | ⏳ todo | — | | Track lists could map to `ColumnDefinitions`/`RowDefinitions`; needs track parsing and codegen design. |
| `grid-cols-none` / `grid-rows-none` | ⏳ todo | — | | Empty definitions; trivial. |
| `grid-cols-subgrid` / `grid-rows-subgrid` | ❌ dropped | — | | Avalonia `Grid` has no subgrid. |
| `grid-areas-*` / `grid-area-*` | ❌ dropped | — | | No named grid areas. |

### `layout.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `overflow-hidden` / `overflow-visible` | ✅ ported | `Static/Clipping.cs` (`Clipping`) | `overflow-hidden`, `overflow-visible` | `Visual.ClipToBoundsProperty`. |
| `overflow-{auto\|scroll\|clip\|overlay}` | ⏳ todo | — | | `auto`/`scroll` would set *both* `ScrollViewer` visibility properties, `clip` ≈ `ClipToBounds`; `overlay` has no equivalent. Not registered — `overflow-hidden`/`overflow-visible` already cover the clipping case. |
| `overflow-x-*` / `overflow-y-*` | 🟡 partial | `Static/ScrollBarVisibility.cs` (`ScrollBarVisibilityRules`) | `scroll-x-auto`, `scroll-y-hidden` | Ported under `scroll-x-*`/`scroll-y-*` (not upstream names), setting `ScrollViewer.{Horizontal,Vertical}ScrollBarVisibility`; no `clip`/`overlay` values. |

### `position.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `relative` / `absolute` / `fixed` / `sticky` / `static` | ❌ dropped | — | | Avalonia has no `position`; layout is panel-driven (`Canvas` for absolute placement). |
| `justify-{start\|end\|center\|between\|around\|evenly\|stretch}` (`justify-content`) | ❌ dropped | — | | No `justify-content`; `Grid`/`UniformGrid`/`StackPanel` distribute children differently. |
| `justify-items-*` | ✅ ported | `Static/ItemsAlign.cs` (`ItemsAlign`) | `justify-items-center`, `justify-items-stretch` | `ContentControl.HorizontalContentAlignmentProperty` — affects the control's content, not arbitrary children. |
| `justify-self-*` | 🟡 partial | `Static/SelfAlign.cs` (`SelfAlign`) | `justify-self-start`, `justify-self-center` | `Layoutable.HorizontalAlignmentProperty`; `auto`, `baseline` and the `*-safe` forms are not ported. |
| `justify-self-auto` / `justify-self-baseline` / `justify-self-*-safe` | ❌ dropped | — | | No baseline alignment; `auto` means "unset", `safe` has no equivalent. |
| `content-*` (`align-content`) | ❌ dropped | — | | No `align-content`. |
| `items-*` (`align-items`) | 🟡 partial | `Static/ItemsAlign.cs` (`ItemsAlign`) | `items-center`, `items-stretch` | `ContentControl.VerticalContentAlignmentProperty`; `baseline`, `baseline-last` and `*-safe` dropped. |
| `self-*` (`align-self`) | 🟡 partial | `Static/SelfAlign.cs` (`SelfAlign`) | `self-center`, `self-end` | `Layoutable.VerticalAlignmentProperty`; `auto`, `baseline`, `baseline-last` and `*-safe` dropped. |
| `place-content-*` | ❌ dropped | — | | No align/justify-content pairing. |
| `place-items-*` | ⏳ todo | — | | Could set both `HorizontalContentAlignment` and `VerticalContentAlignment`; needs design. |
| `place-self-*` | ⏳ todo | — | | Could set both `HorizontalAlignment` and `VerticalAlignment`; needs design. |
| `flex-*` / `grid-*` prefixed aliases of justify/items/place | ❌ dropped | — | | Attributify-mode aliases; there is no attributify equivalent here. |
| `inset-*`, `inset-x/y-*`, `top/right/bottom/left-*`, `start/end-*` | ⏳ todo | — | | `Canvas.Left/Top/Right/Bottom` are the direct equivalent (only meaningful inside a `Canvas`); spacing resolution is already in place. |
| `float-*` | ❌ dropped | — | | No float layout. |
| `clear-*` | ❌ dropped | — | | No float layout. |
| `z-*` | ✅ ported | `Dynamic/ZIndexRules.cs` (`ZIndexRule<TTheme>`) | `z-10`, `z10`, `z-[5]` | `Visual.ZIndexProperty` (`int`); non-integral and `z-auto` values don't resolve (`auto` cannot be expressed as an `int`). |
| `box-border` / `box-content` | ❌ dropped | — | | Avalonia has no box-sizing switch. |

### `question-mark.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `?` / `where` | ❌ dropped | — | | Dev-only UnoCSS debugging hack injected as CSS keyframes. |

### `ring.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `ring`, `ring-{width}`, `ring-{color}` | ⏳ todo | — | | No box-shadow; needs an emulation design (an extra `Border`, or `Visual.Effect`). |
| `ring-offset-*`, `ring-opacity-*`, `ring-inset` | ⏳ todo | — | | Same box-shadow design. |

### `shadow.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `shadow-*` (size) | 🟡 partial | `Dynamic/ShadowRules.cs` (`ShadowRule<TTheme>`) | `shadow-md`, `shadow-inner` | `Border.BoxShadowProperty` + `ContentPresenter.BoxShadowProperty` (`BoxShadows`, multi-layer, inset). Sizes only: shadow colors (`shadow-red-500`), the `shadow-inset` modifier, `shadow-opacity-*` and arbitrary values are not ported — the color is part of each theme entry. A Fluent `Button` template does not forward `BoxShadow`, so the effect shows on a `Border`/`ContentPresenter`. |

### `size.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `w-{n}` / `h-{n}` | ✅ ported | `Dynamic/SizeRules.cs` (`SizeRule<TTheme>`) | `w-4`, `h-4`, `w-[123px]` | `Layoutable.WidthProperty`/`HeightProperty`; reads the `Sizes` scale, then bare rem. |
| `min-w-*` / `max-w-*` / `min-h-*` / `max-h-*` | ✅ ported | `Dynamic/SizeRules.cs` (`SizeRule<TTheme>`) | `min-w-4`, `max-h-4` | Respective `Layoutable` properties. |
| `size-{n}`, `size-min-*`, `size-max-*` | ✅ ported | `Dynamic/SizeRules.cs` (`SizeBothRule<TTheme>`) | `size-xs`, `size-4`, `size-full`, `size-auto` | `Width` + `Height` (or `Min*`/`Max*`) through the same `Sizes` scale, bare-rem and arbitrary-px resolution as `w-*`/`h-*`. Registered before `SizeRule` (as upstream declares it), so `size-4 w-8` keeps `w-8`'s width and `size-4`'s height; within the rule `auto` < a concrete size < `full`, as for the single-axis keywords. |
| `w-full` / `h-full` | 🟡 partial | `Dynamic/SizeRules.cs` (`SizeRule<TTheme>`) | `w-full`, `h-full` | No percentage lengths in Avalonia: clears `Width`/`Height` (`double.NaN`) and sets `HorizontalAlignment`/`VerticalAlignment` to `Stretch`. Fills the space the parent offers (a Grid cell, a DockPanel, a StackPanel's cross axis) but cannot express "100%" on a StackPanel's main axis. Ranks above a concrete size, so it wins over `w-4` in either source order. |
| `w-screen` / `h-screen`, `w-{a/b}` fractions | ❌ dropped | — | | Viewport units and percentages aren't representable: `Layoutable.Width` is an absolute `double`. |
| `w-min` / `w-max` / `w-fit` | ❌ dropped | — | | No content-based (`min-content`/`fit-content`) sizing. |
| `w-auto` / `h-auto` | ✅ ported | `Dynamic/SizeRules.cs` (`SizeRule<TTheme>`) | `w-auto`, `h-auto` | Clears `Width`/`Height` to `double.NaN` (auto sizing) and leaves the alignment untouched. Ranks below a concrete size, so `w-4` still wins. |
| `block-*` / `inline-*` (logical sizes) | ❌ dropped | — | | `Width`/`Height` are physical only. |
| `w-screen-{breakpoint}` / `h-screen-{breakpoint}` | ❌ dropped | — | | No viewport units. |
| `aspect-*` (`aspect-ratio`) | ❌ dropped | — | | No aspect-ratio property; `Viewbox` approximates but is a different control. |

### `spacing.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `m-*` | ✅ ported | `Dynamic/SpacingRules.cs` (`MarginRule<TTheme>`) | `m-4`, `m-0`, `m-[10px]` | Four `MarginSides` doubles, rank 0 (uniform). |
| `mx-*` / `my-*` | ✅ ported | `Dynamic/SpacingRules.cs` (`MarginRule<TTheme>`) | `mx-2`, `my-2` | Two sides, rank 1. |
| `mt/r/b/l-*` | ✅ ported | `Dynamic/SpacingRules.cs` (`MarginRule<TTheme>`) | `mt-1`, `ml-1` | One side, rank 2. |
| `-m-*`, `-mx-*`, `-mt-*`, ... (negative) | ✅ ported | `Dynamic/SpacingRules.cs` (`MarginRule<TTheme>`) | `-m-4`, `-ml-4` | The leading `-` negates the resolved scale value inside the rule; upstream implements this as a variant. |
| `m-s/e-*`, `m-bs/be/is/ie-*`, `m-block/inline-*` | ❌ dropped | — | | Physical sides only. |
| `p-*` | ✅ ported | `Dynamic/SpacingRules.cs` (`PaddingRule<TTheme>`) | `p-4`, `p-0`, `p-[10px]` | Four `PaddingSides` doubles, rank 0. |
| `px-*` / `py-*` | ✅ ported | `Dynamic/SpacingRules.cs` (`PaddingRule<TTheme>`) | `px-2`, `py-2` | Two sides, rank 1. |
| `pt/r/b/l-*` | ✅ ported | `Dynamic/SpacingRules.cs` (`PaddingRule<TTheme>`) | `pt-1`, `pl-1` | One side, rank 2. |
| `p-s/e-*`, `p-bs/be/is/ie-*`, `p-block/inline-*` | ❌ dropped | — | | Physical sides only. |

Upstream's value-less `m-xy` / `p-xy` shorthand is not row-worthy: it resolves the `DEFAULT` spacing
key on both axes, and this port has no `DEFAULT` spacing key (see [Theme scales](#theme-scales)).

### `static.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `inline`, `block`, `inline-block`, `contents`, `flow-root`, `list-item`, `display-*` | ❌ dropped | — | | No `display`; the control/panel type decides layout. |
| `hidden` | ✅ ported | `Static/Visibility.cs` (`Visibility`) | `hidden` | `Visual.IsVisibleProperty = false` (CSS `display: none`). |
| `visible` | ✅ ported | `Static/Visibility.cs` (`Visibility`) | `visible` | `Visual.IsVisibleProperty = true`. |
| `invisible` (`visibility: hidden`) | ⏳ todo | — | | Closest is `Opacity = 0` (+ `IsHitTestVisible = false`), which keeps layout space — same as CSS. |
| `backface-visible` / `backface-hidden` (`backface-*`) | ❌ dropped | — | | No backface-visibility. |
| `cursor-{value}` | 🟡 partial | `Static/Cursor.cs` (`Cursors`) | `cursor-pointer`, `cursor-not-allowed` | 9 values ported (`default`, `pointer`, `text`, `wait`, `move`, `not-allowed`, `crosshair`, `help`, `none`); upstream's remaining ~30 exist as `StandardCursorType` values but aren't registered. |
| `contain-*` | ❌ dropped | — | | No CSS containment. |
| `pointer-events-auto` / `pointer-events-none` | ✅ ported | `Static/PointerEvents.cs` (`PointerEvents`) | `pointer-events-none`, `pointer-events-auto` | `InputElement.IsHitTestVisibleProperty`. |
| `resize-*` | ❌ dropped | — | | No resize-handle property. |
| `select-*` (`user-select`) | ❌ dropped | — | | No `user-select`; `TextBox.IsReadOnly` is a different model. |
| `whitespace-*` | 🟡 partial | `Static/TextWrap.cs` (`TextWrap`) | `whitespace-nowrap`, `whitespace-normal` | `TextBlock.TextWrappingProperty`; `pre`, `pre-line`, `pre-wrap` and `break-spaces` dropped. |
| `content-visibility-*` | ❌ dropped | — | | No CSS containment/rendering hint. |
| `content-*` (CSS `content`) | ❌ dropped | — | | No pseudo-elements. |
| `break-*` (`word-break` / `overflow-wrap`) | ❌ dropped | — | | `TextBlock` has no word-break/overflow-wrap. |
| `text-wrap` / `text-nowrap` / `text-balance` / `text-pretty` | ⏳ todo | — | | `text-wrap`/`text-nowrap` could map to `TextWrapping.Wrap`/`NoWrap` (duplicating `whitespace-*`); `balance`/`pretty` would stay dropped. |
| `truncate` / `text-truncate` | 🟡 partial | `Static/TextWrap.cs` (`TextWrap`) | `truncate` | Sets `NoWrap` + `CharacterEllipsis`; the `text-truncate` alias is missing. |
| `text-ellipsis` / `text-clip` | ✅ ported | `Static/TextWrap.cs` (`TextWrap`) | `text-ellipsis`, `text-clip` | `TextBlock.TextTrimmingProperty`. |
| `case-*` (`text-transform`) | ❌ dropped | — | | No text-transform. |
| `italic` / `not-italic` / `oblique` and `font-*` forms | 🟡 partial | `Static/FontStyles.cs` (`FontStyles`) | `italic`, `not-italic`, `oblique` | `TextElement.FontStyleProperty`; `font-italic`, `font-oblique`, `not-oblique` and `font-not-*` aliases missing. |
| `antialiased` / `subpixel-antialiased` | ⏳ todo | — | | `TextOptions.TextRenderingMode` is the closest knob; there is no grayscale/subpixel pair. |
| `field-sizing-*` | ❌ dropped | — | | No equivalent. |

### `svg.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `fill-{color}` / `fill-none` | ⏳ todo | — | | `Shape.FillProperty` (and `Fill = null`); no rule yet. |
| `fill-opacity-*` / `stroke-opacity-*` | ❌ dropped | — | | Opacity is baked into the brush; no separate channel. |
| `stroke-{width}`, `stroke-{color}`, `stroke-none`, `stroke-dash-*`, `stroke-offset-*`, `stroke-cap-*`, `stroke-join-*` | ⏳ todo | — | | `Shape.StrokeThickness`, `Stroke`, `StrokeDashArray`, `StrokeDashOffset`, `StrokeLineCap`, `StrokeJoin` all exist; no rule yet. |

### `transform.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `origin-*` (`transform-origin`) | ⏳ todo | — | | `Visual.RenderTransformOriginProperty` (`RelativePoint`). |
| `perspective-*` / `perspective-origin-*` | ❌ dropped | — | | No perspective properties. |
| `translate-*` | ⏳ todo | — | | `TranslateTransform` through `RenderTransform`; composing several transform tokens needs a composite design first. |
| `rotate-*` | ⏳ todo | — | | `RotateTransform`; same composition caveat. |
| `skew-*` | ⏳ todo | — | | `SkewTransform`; same composition caveat. |
| `scale-*` | ⏳ todo | — | | `ScaleTransform`; same composition caveat. |
| `transform`, `transform-cpu`, `transform-gpu`, `transform-none` | ❌ dropped | — | | CSS custom-property accumulation; a plain setter per token would clobber another token's transform (needs a composite host like `MarginSides`). |
| `preserve-3d` / `preserve-flat` | ❌ dropped | — | | No transform-style. |

### `transition.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `transition-*`, `duration-*`, `delay-*`, `ease-*`, `property-*` | ⏳ todo | — | | Avalonia `Transitions` (`DoubleTransition`, `BrushTransition`, ...) exist, but they attach to `Animatable.Transitions`, not to a single setter value — needs design. |
| `transition-none` | ⏳ todo | — | | Clearing `Transitions`; same design. |
| `transition-discrete` / `transition-normal` | ❌ dropped | — | | No `transition-behavior`. |

### `typography.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `text-{size}` | 🟡 partial | `Dynamic/FontSizeRules.cs` (`FontSizeRule<TTheme>`) | `text-sm`, `text-4xl`, `text-[18px]` | `TextElement.FontSizeProperty` only — upstream's paired line-height/letter-spacing is dropped (`DefaultTheme.FontSizes` is size-only). |
| `text-{size}/{leading}` shorthand | ⏳ todo | — | | Could set `FontSize` + `LineHeight` once `leading-*` exists. |
| `text-size-*` / `font-size-*` | ⏳ todo | — | | Aliases of the size family; trivial. |
| `text-{color}` | ✅ ported | `Dynamic/ColorRules.cs` (`ForegroundColorRule<TTheme>`) | `text-red-500`, `text-[#00ff00]` | `TextElement.ForegroundProperty`; a value resolving in `FontSizes` wins as a size first. |
| `color-*` / `c-*` | ⏳ todo | — | | Alias of the `text-{color}` family; low value but trivial. |
| `text-opacity-*` | ❌ dropped | — | | Alpha lives in the brush; no separate channel. |
| `font-{weight}` | 🟡 partial | `Static/FontWeights.cs` (`FontWeights`) | `font-bold`, `font-thin` | 9 named weights only; numeric `font-{100..900}` and the `fw-*` alias are missing. |
| `leading-*` / `lh-*` / `line-height-*` | ⏳ todo | — | | `TextBlock.LineHeightProperty` (`double`), plus the `lineHeight` scale (none/tight/.../loose). |
| `font-synthesis-*` | ❌ dropped | — | | No font-synthesis. |
| `tracking-*` (`letter-spacing`) | ⏳ todo | — | | `TextElement.LetterSpacingProperty`. |
| `word-spacing-*` | ❌ dropped | — | | Avalonia has no `word-spacing` property. |
| `font-stretch-*` | ⏳ todo | — | | `TextElement.FontStretchProperty` (`FontStretch`). |
| `font-{family}` | ⏳ todo | — | | `TextElement.FontFamilyProperty`; needs a `fontFamily` scale (not ported). |
| `tab-*` | ❌ dropped | — | | No tab-size. |
| `indent-*` (`text-indent`) | ❌ dropped | — | | No text-indent. |
| `text-stroke-*` | ❌ dropped | — | | Shape stroke only; no glyph-outline stroke on text. |
| `text-shadow*` | ⏳ todo | — | | `Visual.Effect` (`DropShadowEffect`) can approximate one shadow; no per-color/opacity multi-shadow model. |

### `variables.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `{prop}-$var` (CSS variables) | ❌ dropped | — | | CSS custom properties don't exist. Theme values become `DynamicResource` references, but those are engine-owned keys, not user-defined variables. |
| `[prop:value]` (arbitrary CSS property) | ❌ dropped | — | | Arbitrary *values* are supported per rule; arbitrary *properties* have no typed Avalonia equivalent. |

## Variants

Upstream registration lives in `_variants/default.ts`; `_variants/index.ts` only re-exports, so it
declares nothing on its own.

### `aria.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `aria-*`, `group-aria-*`, `peer-aria-*`, `parent-aria-*`, `previous-aria-*`, `has-aria-*` | ❌ dropped | — | | No ARIA attribute model; `AutomationProperties` exposes a fixed set of attached properties, not arbitrary states, and there is no attribute-selector variant. |

### `breakpoints.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `sm:` / `md:` / `lg:` / `xl:` / `2xl:` and `max-*:` | ✅ ported | `Variants/BreakpointVariant.cs` (`BreakpointVariant<TTheme>`) | `sm:hidden`, `max-sm:hidden` | Folds a container-width comparison into the resolved style's `StyleQuery`; unlike CSS media queries the utility only applies inside an element registered as a `Container` with a width sizing mode. |

### `children.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `*:` (direct children) | ⏳ todo | — | | Avalonia's `Selectors.Child` exists, but `SelectorExpression` models only `Class`/`Is`/`OfType`/`PropertyEquals`. |

### `combinators.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `all:` (descendant) | ⏳ todo | — | | `Selectors.Descendant`; needs `SelectorExpression` support. |
| `children:` | ⏳ todo | — | | Same as `*:`. |
| `next:` / `sibling:` / `siblings:` | ❌ dropped | — | | Avalonia's selector grammar has no sibling combinators (`+`/`~`). |

### `container.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `@container` / `@sm` style container-query variants | ⏳ todo | — | | The engine already emits Avalonia `ContainerQuery`s (`Variants/BreakpointVariant.cs`), but the `@` prefix isn't registered. |

### `dark.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `dark:` | ✅ ported | `Variants/DarkVariant.cs` (`DarkVariant<TTheme>`) | `dark:bg-slate-800`, `dark:hover:underline` | `PropertyEquals(ThemeVariantScope.ActualThemeVariantProperty, Dark)` — inherited, so it works anywhere under the scope. Upstream's media-query and class strategies collapse into this one. |
| `light:` | ❌ dropped | — | | An equivalent exists but is subsumed: an un-prefixed token already applies in `Light`. |

### `data.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `data-*` plus tagged (`group-data-*`, `peer-data-*`, ...) variants | ❌ dropped | — | | No `data-*` attributes; the closest is a style class plus a pseudo-class. |

### `directions.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `ltr:` / `rtl:` | ⏳ todo | — | | `PropertyEquals(Visual.FlowDirectionProperty, ...)`; the `ltr`/`rtl` static tokens exist, the variant does not. |

### `important.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `important:` / trailing `!` | ❌ dropped | — | | Avalonia has no `!important`; `StyleOrdering` owns the cascade. |

### `media.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `print:` | ❌ dropped | — | | No print medium. |
| `media-*` (custom media queries) | ❌ dropped | — | | No media queries; only container queries. |

### `misc.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `selector-[...]` | ❌ dropped | — | | Arbitrary selector text can't be mapped onto Avalonia's typed selector API. |
| `layer-*` / `uno-layer-*` | ❌ dropped | — | | No cascade layers. |
| `scope-[...]` | ❌ dropped | — | | No scope-placeholder mechanism. |
| `[&:hover]:` and friends (`variantVariables`) | ❌ dropped | — | | No CSS-variable/selector-template mechanism. |
| `theme(...)` (`variantTheme`) | ❌ dropped | — | | Values are resolved by rules against the theme, not by a value function. |

### `negative.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `-` prefix (`-mt-4`, `-m-4`, ...) | ✅ ported | `Dynamic/SpacingRules.cs` (`MarginRule<TTheme>`) | `-m-4`, `-ml-4` | Implemented inside `MarginRule` (leading `-`) rather than as a variant: a generic negation variant would need per-rule opt-in hooks. |

### `pseudo.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| State pseudo-classes (`hover:`, `focus:`, `disabled:`, `checked:`, `readonly:`, ...) | 🟡 partial | `Variants/PseudoClassVariant.cs` (`PseudoClassVariant<TTheme>`) | `hover:underline`, `disabled:underline` | 17 states ported; upstream also has structural, pseudo-element, function and tagged variants (rows below). |
| Structural pseudo-classes (`first:`, `last:`, `odd:`, `even:`, `nth-*`, `only-child`) | ⏳ todo | — | | Avalonia has `Selectors.NthChild`/`NthLastChild`; `SelectorExpression` doesn't model them yet. |
| `first-of-type` / `last-of-type` / `only-of-type` / `nth-of-type` | ❌ dropped | — | | Avalonia can filter by type (`OfType`) but has no `nth-of-type`. |
| `not:` | ⏳ todo | — | | `Selectors.Not` exists; needs `SelectorExpression` support. |
| `is:` / `where:` / `has:` | ❌ dropped | — | | `Selectors.Is` is type-based, not a selector list; `:has`/`:where` don't exist. |
| Pseudo-elements (`before:`, `after:`, `placeholder:`, `selection:`, `marker:`, `first-letter:`, ...) | ❌ dropped | — | | Avalonia has no pseudo-elements. |
| `group-*` / `peer-*` tagged pseudo-classes | ❌ dropped | — | | Needs ancestor/sibling relationships and `.group`/`.peer` conventions; no sibling combinators. |
| `part-*` (`variantPartClasses`) | ❌ dropped | — | | CSS `::part` has no Avalonia equivalent. |

### `startingstyle.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `starting:` | ❌ dropped | — | | No `@starting-style`. |

### `supports.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `supports-*` | ❌ dropped | — | | No `@supports`. |

## Theme scales

Upstream scales are `_theme/*.ts`; `_theme/default.ts` composes them and `_theme/index.ts` /
`_theme/types.ts` declare no data. A theme row's status says whether the *scale* is ported — i.e.
whether the corresponding utilities can read it — and its examples are tokens that exercise it.

### `_theme/colors.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `colors` | 🟡 partial | `Extensions/DefaultTheme.cs` (`ColorHex`) + `Dynamic/ColorRules.cs` | `bg-blue-500`, `text-red-500`, `border-green-700` | Full Tailwind palette (22 families × 11 shades, plus `black`/`white`/`transparent`), flattened to `{family}-{shade}` keys; a shade-less family key doesn't resolve. `inherit`/`current` dropped (no color-inheritance keywords), and this port adds non-upstream `light`/`dark` families. |

### `_theme/filters.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `blur` | ⏳ todo | — | | `BlurEffect` exists on `Visual.Effect`; no scale or utility yet. |
| `dropShadow` | ⏳ todo | — | | `DropShadowEffect` exists; no scale or utility yet. |

### `_theme/font.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `fontFamily` | ⏳ todo | — | | `TextElement.FontFamilyProperty` exists; no `font-{family}` rule yet. |
| `fontSize` | 🟡 partial | `Extensions/DefaultTheme.cs` (`FontSizes`) | `text-sm`, `text-4xl` | Size only, in px; upstream's paired line-height/letter-spacing is dropped. |
| `fontWeight` | 🟡 partial | `Static/FontWeights.cs` (`FontWeights`) | `font-bold` | Ported as 9 static rules rather than a scale; numeric weights missing. |
| `textIndent` | ❌ dropped | — | | No text-indent. |
| `textStrokeWidth` | ❌ dropped | — | | No text stroke. |
| `lineHeight` | ⏳ todo | — | | `TextBlock.LineHeightProperty`; no `leading-*` rule yet. |
| `letterSpacing` | ⏳ todo | — | | `TextElement.LetterSpacingProperty`; values are in `em` upstream and would need px conversion. |
| `textShadow` | ⏳ todo | — | | `Visual.Effect` can approximate one shadow; no text-shadow utility. |
| `wordSpacing` | ❌ dropped | — | | Avalonia has no `word-spacing` property. |

### `_theme/misc.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `breakpoints` | ✅ ported | `Extensions/DefaultTheme.cs` (`Breakpoints`) + `Variants/BreakpointVariant.cs` | `sm:hidden`, `max-sm:hidden` | Mapped to container queries, not media queries: a utility only applies inside an element registered as a `Container` with a width sizing mode. |
| `verticalBreakpoints` | ⏳ todo | — | | `StyleQueryExpression.Height` exists, but no height-breakpoint variant. |
| `lineWidth` | ✅ ported | `Extensions/DefaultTheme.cs` (`LineWidths`) | `border-2`, `border-t-2` | `DEFAULT` = 1px, `none` = 0; unmatched bare numbers stay px. |
| `spacing` | 🟡 partial | `Extensions/DefaultTheme.cs` (`Spacing`) | `m-4`, `gap-4`, `p-4` | This port ships the numeric Tailwind scale (`0`–`96` at 4 px/step, plus `px`) instead of upstream's named scale (`DEFAULT`/`none`/`xs`/.../`9xl`); `m-4` resolves to 16 px either way (upstream via rem, here by key), but `m-xs`/`m-DEFAULT` don't resolve here. |
| `duration` | ⏳ todo | — | | Avalonia transitions can use durations; no scale or utility yet. |
| `borderRadius` | ✅ ported | `Extensions/DefaultTheme.cs` (`Radii`) | `rounded-md`, `rounded-full` | rem converted to px at 16/rem. |
| `boxShadow` | 🟡 partial | `Extensions/DefaultTheme.cs` (`BoxShadows`) + `Dynamic/ShadowRules.cs` | `shadow-md`, `shadow-inner` | Ported as `BoxShadows` values (DEFAULT, none, sm, md, lg, xl, 2xl, inner) with the `var(--un-shadow-inset)` placeholder dropped; shadow colors and opacity are not ported. |
| `ringWidth` | ⏳ todo | — | | Needs the `ring-*` design. |
| `zIndex` | ❌ dropped | — | | Upstream's only entry is `auto`, and `Visual.ZIndex` is an `int` — there is no "as if unset" value. The `z-*` rule takes the number directly, so no scale is needed. |
| `media` | ❌ dropped | — | | No media queries. |

### `_theme/preflight.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `preflightBase` (transform/ring/shadow CSS variables) | ❌ dropped | — | | Only exists to seed CSS custom properties; the Avalonia equivalent is the composite-host mechanism (`MarginSides` etc.), not a preflight dictionary. |

### `_theme/size.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `baseSize` | 🟡 partial | `Extensions/DefaultTheme.cs` (`Sizes`) | `w-xs`, `h-md` | rem converted to px; `prose` (`65ch`) dropped — no character-based unit. |
| `width` / `height` / `maxWidth` / `maxHeight` / `minWidth` / `minHeight` | 🟡 partial | `Extensions/DefaultTheme.cs` (`Sizes`) + `Dynamic/SizeRules.cs` | `w-4`, `max-w-4` | Same `baseSize` keys shared by all size properties. Upstream's `auto`/`full` are mapped outside the scale (`w-auto`, `w-full`); `none`, `screen` and fractions are not registered — no percentages or viewport units. |
| `inlineSize` / `blockSize` / `maxInlineSize` / `maxBlockSize` / `minInlineSize` / `minBlockSize` | ❌ dropped | — | | Logical sizes have no physical Avalonia counterpart. |
| `containers` | ⏳ todo | — | | Only needed once the `@container` variant lands. |

### `_theme/transition.ts`

| Upstream | Status | Here | Examples | Notes |
|---|---|---|---|---|
| `easing` | ⏳ todo | — | | Avalonia has easing functions (`CubicBezierEasing` etc.), but no `ease-*` utility. |
| `transitionProperty` | ⏳ todo | — | | Same `Transitions` design as the transition rules. |

## Summary

| Section | ✅ ported | 🟡 partial | ❌ dropped | ⏳ todo | Total |
|---|---|---|---|---|---|
| Rules | 38 | 12 | 71 | 41 | 162 |
| Variants | 3 | 1 | 19 | 7 | 30 |
| Theme scales | 3 | 7 | 7 | 12 | 29 |
| **Total** | **44** | **20** | **97** | **60** | **221** |

> Counts are asserted by `ParityMatrixTests.Counts_MatchSummary` — update this table when rows change.

### Next to port

Ranked by (how common in real UIs) × (how clear the Avalonia equivalent is). All are ⏳ rows
above.

1. **`size-*`** — sets `Width` + `Height` (and their `min`/`max` forms) from the existing `Sizes`
   scale; the only new part is emitting two to four properties per token.
2. **`w-auto` / `h-auto`** — `double.NaN` is exactly "auto"; trivial, and completes the size family.
5. **`leading-*` / `lh-*` / `line-height-*`** — `TextBlock.LineHeightProperty` plus the (small)
   `lineHeight` scale; very common in typography-heavy UIs.
6. **`tracking-*`** — `TextElement.LetterSpacingProperty`; convert the `em`-based scale at
   `RemToPxFactor`.
7. **`font-{family}`** — `TextElement.FontFamilyProperty` plus the `fontFamily` scale (sans/serif/
   mono are already spelled out upstream).
8. **`top/right/bottom/left-*` + `inset-*`** — `Canvas.Left/Top/Right/Bottom`; only meaningful in a
   `Canvas`, but that is the Avalonia idiom for absolute placement.

Trivial aliases (`text-truncate`, `decoration-none`, `gap-col-*`, `text-size-*`, `b-*`/`rd-*`,
`text-align-*`, ...) are deliberately *not* in this list: they add surface without new capability.
