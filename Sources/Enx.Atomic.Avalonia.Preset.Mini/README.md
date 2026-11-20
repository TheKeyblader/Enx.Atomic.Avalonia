# Enx.Atomic.Avalonia.Preset.Mini

A concrete rule/variant set for `Enx.Atomic.Avalonia`, inspired by
[`@unocss/preset-mini`](https://unocss.dev/presets/mini). This is the reference for every
utility-class token it registers — see the root
[README.md](../../README.md)/[ARCHITECTURE.md](../../ARCHITECTURE.md) for how the engine itself
resolves them.

For how this compares to upstream, family by family (what's ported, partial, dropped
and why, or still to do), see [PARITY.md](PARITY.md). It is test-backed: every example
token listed for a ported or partial family must resolve.

## Usage

```csharp
var builder = ThemeBuilder<MiniTheme>.Create();
var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };
builder.AddMiniTheme(configuration);
```

`AddMiniTheme` registers every rule, variant, and default theme scale documented below.
Everything is exposed individually too (`AddColorRules`, `AddSpacingRules`, ...) if you only
want part of it — see `Extensions/ThemeBuilderExtensions.cs`.

```xml
<Button Classes="flex-row p-4 gap-2 rounded-md bg-blue-500 hover:bg-blue-600 sm:flex-col" />
```

## Variants

Prefixed onto a token with `:`. Chainable — `hover:focus:underline` applies both.

### Pseudo-classes (`PseudoClassVariant`)

| Prefix | Avalonia pseudo-class |
|---|---|
| `hover:` | `:pointerover` |
| `active:` / `pressed:` | `:pressed` |
| `disabled:` | `:disabled` |
| `enabled:` | `:enabled` |
| `focus:` | `:focus` |
| `focus-visible:` | `:focus-visible` |
| `focus-within:` | `:focus-within` |
| `selected:` | `:selected` |
| `checked:` | `:checked` |
| `unchecked:` | `:unchecked` |
| `indeterminate:` | `:indeterminate` |
| `dragging:` | `:dragging` |
| `empty:` | `:empty` |
| `open:` | `:open` |
| `invalid:` | `:invalid` |
| `readonly:` | `:readonly` |

### Breakpoints (`BreakpointVariant`)

`{name}:` matches when the container is **at least** that wide (`min-width`); `max-{name}:`
matches when it's **at most** that wide (`max-width`) — see [Breakpoints](#breakpoints) for the
scale. Resolves to an Avalonia container query, not a media query — the utility only applies
inside a control whose `ContainerName`/size the query can see.

```xml
<StackPanel Classes="flex-col sm:flex-row" />
```

### Dark mode (`DarkVariant`)

`dark:` matches when the current Avalonia `ThemeVariant` is `Dark` — Avalonia's native
Light/Dark theming, checked via `ThemeVariantScope.ActualThemeVariantProperty` (inherited, so it
resolves correctly regardless of where the `ThemeVariantScope`/`TopLevel` sits above the element).
There's no `light:` — since `Light` is the default, an un-prefixed token already covers it.

```xml
<Border Classes="bg-white dark:bg-slate-800" />
```

## Static utilities

Fixed name, fixed value — no theme lookup.

| Token | Sets |
|---|---|
| `hidden` / `visible` | `Visual.IsVisibleProperty` = `false` / `true` |
| `overflow-hidden` / `overflow-visible` | `Visual.ClipToBoundsProperty` = `true` / `false` |
| `pointer-events-none` / `pointer-events-auto` | `InputElement.IsHitTestVisibleProperty` = `false` / `true` |
| `enabled` / `disabled` | `InputElement.IsEnabledProperty` = `true` / `false` |
| `focusable` / `not-focusable` | `InputElement.FocusableProperty` = `true` / `false` |
| `ltr` / `rtl` | `Visual.FlowDirectionProperty` = `LeftToRight` / `RightToLeft` |
| `flex-row` / `flex-col` | `Orientation` = `Horizontal` / `Vertical`, on `StackPanel`, `WrapPanel`, `ProgressBar`, `ScrollBar`, and `TickBar` |
| `justify-self-start/center/end/stretch` | `Layoutable.HorizontalAlignmentProperty` |
| `self-start/center/end/stretch` | `Layoutable.VerticalAlignmentProperty` |
| `justify-items-start/center/end/stretch` | `ContentControl.HorizontalContentAlignmentProperty` |
| `items-start/center/end/stretch` | `ContentControl.VerticalContentAlignmentProperty` |
| `scroll-x-auto/hidden/visible/disabled` | `ScrollViewer.HorizontalScrollBarVisibilityProperty` |
| `scroll-y-auto/hidden/visible/disabled` | `ScrollViewer.VerticalScrollBarVisibilityProperty` |
| `object-fill/contain/cover/none` | `Stretch` on `Image`, `Shape`, and `Viewbox` |
| `italic` / `not-italic` / `oblique` | `TextElement.FontStyleProperty` |
| `font-thin/extralight/light/normal/medium/semibold/bold/extrabold/black` | `TextElement.FontWeightProperty` |
| `text-left/center/right/justify` | `TextBlock.TextAlignmentProperty` |
| `underline` / `line-through` / `overline` / `no-underline` | `Inline.TextDecorationsProperty` |
| `whitespace-nowrap` / `whitespace-normal` | `TextBlock.TextWrappingProperty` |
| `text-clip` / `text-ellipsis` | `TextBlock.TextTrimmingProperty` |
| `truncate` | Both of the above: `NoWrap` + `CharacterEllipsis` |
| `cursor-default/pointer/text/wait/move/not-allowed/crosshair/help/none` | `InputElement.CursorProperty` |

## Dynamic utilities

Theme-driven — the value after the prefix is resolved against a scale (see
[Theme scales](#theme-scales) below).

| Token | Sets | Scale |
|---|---|---|
| `bg-{color}` | `Border.BackgroundProperty`, `TemplatedControl.BackgroundProperty`, **and** `Panel.BackgroundProperty` (three styles — see note below) | [Colors](#colors) |
| `text-{color}` | `TextElement.ForegroundProperty` | [Colors](#colors) |
| `border-{color}` | `Border.BorderBrushProperty` | [Colors](#colors) |
| `text-{size}` | `TextElement.FontSizeProperty` | [FontSizes](#fontsizes) |
| `border` / `border-{width}` | `BorderThicknessSides` (four sides), recomposed into `Border.BorderThicknessProperty` | [LineWidths](#linewidths) |
| `border-x-{width}` / `border-y-{width}` | `BorderThicknessSides`, left+right / top+bottom | [LineWidths](#linewidths) |
| `border-t/r/b/l-{width}` | `BorderThicknessSides`, one side | [LineWidths](#linewidths) |
| `rounded-{radius}` | `CornerRadiusSides` (four corners), recomposed into `Border.CornerRadiusProperty` | [Radii](#radii) |
| `rounded-t/b/l/r-{radius}` | `CornerRadiusSides`, two adjacent corners | [Radii](#radii) |
| `rounded-tl/tr/br/bl-{radius}` | `CornerRadiusSides`, one corner | [Radii](#radii) |
| `w-{size}` / `h-{size}` | `Layoutable.WidthProperty` / `HeightProperty` | [Sizes](#sizes) |
| `w-full` / `h-full` | `Layoutable.WidthProperty` / `HeightProperty` = `double.NaN` **and** the matching alignment = `Stretch` | *(structural)* |
| `w-auto` / `h-auto` | `Layoutable.WidthProperty` / `HeightProperty` = `double.NaN` | *(structural)* |
| `min-w-{size}` / `max-w-{size}` | `Layoutable.MinWidthProperty` / `MaxWidthProperty` | [Sizes](#sizes) |
| `min-h-{size}` / `max-h-{size}` | `Layoutable.MinHeightProperty` / `MaxHeightProperty` | [Sizes](#sizes) |
| `size-{size}` | `Layoutable.WidthProperty` **and** `HeightProperty` | [Sizes](#sizes) |
| `size-min-{size}` / `size-max-{size}` | `MinWidth`+`MinHeight` / `MaxWidth`+`MaxHeight` | [Sizes](#sizes) |
| `size-full` / `size-auto` | Both lengths = `double.NaN`, plus both alignments = `Stretch` for `full` | *(structural)* |
| `m-{n}` | `MarginSides` (four sides), recomposed into `Layoutable.MarginProperty` | [Spacing](#spacing) |
| `mx-{n}` / `my-{n}` | `MarginSides`, left+right / top+bottom | [Spacing](#spacing) |
| `mt-{n}` / `mr-{n}` / `mb-{n}` / `ml-{n}` | `MarginSides`, one side | [Spacing](#spacing) |
| `-m-{n}`, `-ml-{n}`, ... | Same as above, negated | [Spacing](#spacing) |
| `p-{n}` | `PaddingSides` (four sides), recomposed into `Decorator.PaddingProperty` | [Spacing](#spacing) |
| `px-{n}` / `py-{n}` | `PaddingSides`, left+right / top+bottom | [Spacing](#spacing) |
| `pt-{n}` / `pr-{n}` / `pb-{n}` / `pl-{n}` | `PaddingSides`, one side | [Spacing](#spacing) |
| `shadow` / `shadow-{size}` | `Border.BoxShadowProperty` **and** `ContentPresenter.BoxShadowProperty` (two styles) | [BoxShadows](#boxshadows) |
| `gap-{n}` | `Spacing`/`ColumnSpacing`/`RowSpacing` on `StackPanel`, `Grid`, `UniformGrid` | [Spacing](#spacing) |
| `gap-x-{n}` | `ColumnSpacing` on `Grid` and `UniformGrid` only | [Spacing](#spacing) |
| `gap-y-{n}` | `RowSpacing` on `Grid` and `UniformGrid` only | [Spacing](#spacing) |
| `opacity-{n}` / `op-{n}` | `Visual.OpacityProperty` (bare `n` is a percentage: `50` → `0.5`) | *(structural)* |
| `z-{n}` / `z{n}` | `Visual.ZIndexProperty` (`int`) | *(structural)* |
| `leading-{n}` / `lh-{n}` / `line-height-{n}` | `TextBlock.LineHeightProperty` (absolute px — see note) | *(structural)* |
| `tracking-{n}` | `TextElement.LetterSpacingProperty` (absolute px — see note) | *(structural)* |
| `font-sans` / `font-serif` / `font-mono` | `TextElement.FontFamilyProperty` | [FontFamilies](#fontfamilies) |
| `inset-{n}` | All four `Canvas.Left`/`Top`/`Right`/`Bottom` attached properties | [Spacing](#spacing) |
| `inset-x-{n}` / `inset-y-{n}` | `Canvas`, left+right / top+bottom | [Spacing](#spacing) |
| `inset-t/r/b/l-{n}` | One `Canvas` side each | [Spacing](#spacing) |
| `top-{n}` / `right-{n}` / `bottom-{n}` / `left-{n}` | The matching `Canvas` side | [Spacing](#spacing) |
| `-inset-{n}`, `-top-{n}`, ... | Same as above, negated | [Spacing](#spacing) |
| `grid-cols-{n}` | `Grid.ColumnDefinitions` = `n` equal (`1*`) columns (ghost property, on the `Grid` itself) | *(structural — see note below)* |
| `grid-rows-{n}` | `Grid.RowDefinitions` = `n` equal (`1*`) rows (ghost property, on the `Grid` itself) | *(structural)* |
| `col-{n}` / `row-{n}` | `Grid.ColumnProperty` / `RowProperty` (0-based, on a grid **child**) | *(structural)* |
| `col-span-{n}` / `row-span-{n}` | `Grid.ColumnSpanProperty` / `RowSpanProperty` (on a grid **child**) | *(structural)* |

Notes:
- **`bg-{color}` emits three styles.** `Border.BackgroundProperty`, `TemplatedControl.BackgroundProperty`,
  and `Panel.BackgroundProperty` are the exact same `AvaloniaProperty` (`TemplatedControl`/`Panel` share
  it via `AddOwner`), but a single selector can't match `Border`-, `TemplatedControl`-, and `Panel`-derived
  elements (e.g. `Button`, `StackPanel`) at once — see `ARCHITECTURE.md` for why.
- **`grid-cols-*`/`grid-rows-*` are structural, not theme-scaled** — `n` is parsed directly as a plain
  integer, not looked up in a scale. `Grid.ColumnDefinitions`/`RowDefinitions` aren't real
  `AvaloniaProperty`s (Avalonia limitation), so these go through a ghost `AttachedProperty` plus a class
  handler that syncs the real collection — see `GridDefinitions` in `Dynamic/GridRules.cs`. Both paths
  work: the real class registers the handlers in its static constructor, and the codegen copy has its own.
- **`col-*`/`row-*`/`col-span-*`/`row-span-*` target the grid child, not the `Grid`** — they're
  `AttachedProperty`s meant to be set on whatever control sits inside the grid (e.g.
  `<Border Classes="col-span-2">`), not on the `Grid` element itself.
- **Per-side margin/padding/border-width/radius** (`ml-*`, `pt-*`, `border-t-*`,
  `rounded-tl-*`, ...) — and their uniform/axis forms — don't write the real struct-valued
  property: each writes the `double` side attached properties of its host (`MarginSides`,
  `PaddingSides`, `BorderThicknessSides`, `CornerRadiusSides`) — one for a single side, two
  for an axis/edge, four for a uniform token — and the host's class handler recomposes the
  real `Thickness`/`CornerRadius` from them. So `ml-1 mr-2` composes into one `Margin`, a
  variant stays its own style (pseudo-class and container query included), and overlapping
  tokens in a family resolve in UnoCSS's direction order (`m-*` < `mx-*` < `ml-*`) whatever
  the source order. A side no token sets composes as `0` — see `ARCHITECTURE.md`,
  "Composite side properties".
- **`p-*`/`border-*` (width)/`rounded-*` reach `TemplatedControl` subclasses** — the tokens
  target `Control`, and the real properties are `AddOwner`'ed by `TemplatedControl` (and
  `ContentPresenter`) too, so `<Button Classes="p-4 rounded-md" />` works. The padding
  handler skips controls that never register `Padding` (e.g. `Canvas`).
- **`text-` is shared** between `FontSizeRule` and `ForegroundColorRule` — a value that resolves in
  the `FontSizes` scale wins as a size; otherwise it's tried as a color. Likewise **`border-`** is
  shared between `BorderWidthRule` and `BorderColorRule` — a value resolving in `LineWidths` (named
  key or bare number) wins as a width, otherwise it's tried as a color.
- **`rounded` alone does not match anything** — the value segment is mandatory. Use `rounded-DEFAULT`
  for the base 4px radius (unlike `border`, whose value segment is optional and also defaults to
  `DEFAULT`).
- **Arbitrary values** (`bg-[#ff0000]`, `w-[123px]`, `m-[10px]`, `rounded-[6px]`, `border-[3px]`,
  `text-[18px]`) bypass the theme scale entirely — see [Arbitrary values](#arbitrary-values) below.
- **`w-full`/`h-full` stretch, they are not "100%".** Avalonia has no percentage lengths: the tokens
  clear the length and set `HorizontalAlignment`/`VerticalAlignment` to `Stretch`, so they fill what the
  parent offers (a `Grid` cell, a `DockPanel`, a `StackPanel`'s cross axis) but cannot fill a
  `StackPanel`'s main axis. `w-auto`/`h-auto` only clear the length. Ordering within the family is
  fixed by the rule's rank — `auto` < a concrete size < `full` — so `w-4 w-full` and `w-full w-4` both
  fill, and `w-auto w-4` stays 16px.
- **`shadow-{size}` is size-only.** The values come from the [BoxShadows](#boxshadows) scale and set
  `BoxShadows` on `Border` and `ContentPresenter`; a Fluent `Button` template does not forward
  `BoxShadow`, so the effect isn't visible on a `Button` itself. Shadow colors (`shadow-red-500`), the
  `shadow-inset` modifier, `shadow-opacity-*` and arbitrary values are not implemented.
- **`w-*`/`h-*` use the small named [Sizes](#sizes) scale, not [Spacing](#spacing)** — this differs
  from Tailwind, where `w-*` shares the spacing scale. A bare number here (e.g. `w-64`) that isn't a
  named `Sizes` key falls back to being read as **rem** (`64rem × 16 = 1024px`), not the spacing
  scale's `64 → 256px`.
- **`size-*` sets both lengths and is registered before `w-*`/`h-*`** (upstream's order), so an
  axis-specific token wins only its own property: `size-4 w-8` is 128px wide and 64px tall in either
  source order. Inside the rule `size-auto` < a concrete size < `size-full`, as for `w-auto`/`w-full`.
- **`opacity-*`/`op-*` read a bare token as a percentage** (UnoCSS's `h.bracket.percent`), so
  `opacity-50` is `0.5` and `opacity-0.5` is `0.005`; an arbitrary value is taken as written —
  `opacity-[0.35]` is a fraction, `opacity-[50%]` spells the percentage out. There is no theme scale.
- **`z-*` takes an `int`** (Avalonia's `Visual.ZIndexProperty`): `z-10`/`z10`/`z-[5]` resolve, while a
  fractional value is a miss rather than a rounded one. Upstream's `z-auto` is not ported — `auto`
  means "as if unset", which an `int` cannot express.
- **`leading-*`/`tracking-*` are absolute-length only.** Avalonia's `TextBlock.LineHeight` and
  `TextElement.LetterSpacing` are pixels, so an arbitrary value (`leading-[24px]`) or a bare number
  read as rem (`leading-4` = 64px) resolves; upstream's named scales are unitless line-height
  multipliers and `em` letter-spacing values, which a style cannot resolve against the element's own
  font size, so `leading-tight`/`tracking-wide`/... do not resolve.
- **`font-sans`/`font-serif`/`font-mono` come from the [FontFamilies](#fontfamilies) scale** and set
  the inherited `TextElement.FontFamilyProperty`. `font-bold` and the other weights stay static rules
  (a static rule always wins the shared `font-` prefix), and upstream's `font-[Arial]` form is not
  ported: a bracket value there would be text, not a pixel length.
- **`inset-*`/`top-*`/... are `Canvas` placement** (`Canvas.Left`/`Top`/`Right`/`Bottom`), so they are
  only meaningful on a child of a `Canvas`; a uniform `inset-*` writes all four sides, and the family
  ranks uniform < axis < side, so `inset-0 top-4` composes. Upstream's `auto`/fraction forms are not
  ported (a side is a plain `double`, `NaN` meaning unset).
- **`border-x-*`/`border-y-*` write the composite host's two sides** and rank between the uniform
  token (0) and a single side (2), so `border-2 border-x-4 border-t-8` composes to 4/8/4/2 whatever the
  source order.
- Every numeric scale below also accepts a bare number with no unit as a fallback when the key
  isn't found by name — treated as **rem** (`× RemToPxFactor`, default 16) everywhere *except*
  [LineWidths](#linewidths), which treats it as raw **px** (matching UnoCSS's `lineWidth` scale).
  [BoxShadows](#boxshadows) is not numeric and has no bare-number fallback.

### Arbitrary values

UnoCSS/Tailwind's bracket syntax, `{prefix}-[{value}]`, bypasses a token's theme scale entirely —
an escape hatch for a one-off value not worth naming in the theme:

```xml
<Border Classes="bg-[#ff0000] w-[123px] rounded-[6px]" />
```

- **Colors** (`bg-{color}`, `text-{color}`, `border-{color}`) accept any
  `Avalonia.Media.Color.TryParse`-compatible text — `#ff0000`, `#80ff0000` (ARGB), a named color
  (`red`), `rgb(255,0,0)`. Always resolves to a fixed brush, never a `DynamicResource` — see
  [Resource-based theming](../../ARCHITECTURE.md#build-time-c-code-generation) in
  `ARCHITECTURE.md` for why an arbitrary value can't participate in that.
- **Every px-based scale** (Spacing, Sizes, Radii, LineWidths, FontSizes — so `m-*`/`p-*`/`gap-*`,
  `w-*`/`h-*`, `rounded-*`, `border-*` width, `text-*` size) accepts a number with an optional
  trailing `px` (`w-[123px]` or `w-[123]` — both the same). Taken **literally as pixels**, unlike a
  bare unbracketed number, which is read as **rem** (or raw px for `LineWidths`) — brackets always
  mean "exactly this," no scale conversion applied.
- Any other unit (`w-[50%]`, `w-[2em]`) or unparseable content doesn't match — the token falls
  through as if no rule applied, same as an unresolvable named key.
- `leading-*`/`tracking-*` accept the same `[Npx]` form (they have no named scale — see the notes
  above). `opacity-*` is the exception: `[0.35]` is a fraction and `[50%]` a percentage.
- No bracket support for a font family (`font-[Arial]` — that value would be text, not a length) or
  for structural tokens (`grid-cols-*`, `col-span-*`, ...), which don't read a theme scale in the
  first place.

## Theme scales

`MiniTheme`'s defaults, from `Extensions/DefaultTheme.cs`. `RemToPxFactor` defaults to `16`.

### Spacing

Used by margin/padding/gap. Values in px.

| Key | px | Key | px | Key | px | Key | px |
|---|---|---|---|---|---|---|---|
| `px` | 1 | `3.5` | 14 | `11` | 44 | `40` | 160 |
| `0` | 0 | `4` | 16 | `12` | 48 | `44` | 176 |
| `0.5` | 2 | `5` | 20 | `14` | 56 | `48` | 192 |
| `1` | 4 | `6` | 24 | `16` | 64 | `52` | 208 |
| `1.5` | 6 | `7` | 28 | `20` | 80 | `56` | 224 |
| `2` | 8 | `8` | 32 | `24` | 96 | `60` | 240 |
| `2.5` | 10 | `9` | 36 | `28` | 112 | `64` | 256 |
| `3` | 12 | `10` | 40 | `32` | 128 | `72` | 288 |
| | | | | `36` | 144 | `80` | 320 |
| | | | | | | `96` | 384 |

### Sizes

Used **only** by `w-*`/`h-*`/`min-w-*`/`max-w-*`/`min-h-*`/`max-h-*` and the `size-*` pair — see the
note above about how this differs from [Spacing](#spacing).

| Key | px | Key | px | Key | px |
|---|---|---|---|---|---|
| `xs` | 320 | `md` | 448 | `3xl` | 768 |
| `sm` | 384 | `lg` | 512 | `4xl` | 896 |
| | | `xl` | 576 | `5xl` | 1024 |
| | | `2xl` | 672 | `6xl` | 1152 |
| | | | | `7xl` | 1280 |

### Radii

| Key | px |
|---|---|
| `none` | 0 |
| `sm` | 2 |
| `DEFAULT` | 4 |
| `md` | 6 |
| `lg` | 8 |
| `xl` | 12 |
| `2xl` | 16 |
| `3xl` | 24 |
| `full` | 9999 |

### LineWidths

| Key | px |
|---|---|
| `none` | 0 |
| `DEFAULT` | 1 |

### BoxShadows

Used by `shadow` (the `DEFAULT` key) and `shadow-{key}`. Each entry is one or two Avalonia
`BoxShadows` layers; `none` is the empty value. `inner` is the only inset entry. Lengths are px;
the colour is `rgb(0 0 0 / α)` with α in ARGB.

| Key | Layers (`offsetX offsetY blur spread alpha`) |
|---|---|
| `none` | *(no shadow)* |
| `sm` | `0 1 2 0` α 0.05 |
| `DEFAULT` | `0 1 3 0` α 0.1; `0 1 2 -1` α 0.1 |
| `md` | `0 4 6 -1` α 0.1; `0 2 4 -2` α 0.1 |
| `lg` | `0 10 15 -3` α 0.1; `0 4 6 -4` α 0.1 |
| `xl` | `0 20 25 -5` α 0.1; `0 8 10 -6` α 0.1 |
| `2xl` | `0 25 50 -12` α 0.25 |
| `inner` | `0 2 4 0` α 0.05, inset |

### FontSizes

| Key | px | Key | px |
|---|---|---|---|
| `xs` | 12 | `4xl` | 36 |
| `sm` | 14 | `5xl` | 48 |
| `base` | 16 | `6xl` | 60 |
| `lg` | 18 | `7xl` | 72 |
| `xl` | 20 | `8xl` | 96 |
| `2xl` | 24 | `9xl` | 128 |
| `3xl` | 30 | | |

### FontFamilies

Used by `font-{family}`. Already parsed into Avalonia `FontFamily` values: a comma-separated fallback
list, in upstream's order but without the web-only generic keywords (`ui-sans-serif`, `system-ui`,
`-apple-system`, …) and without quotes (Avalonia's parser splits on commas only).

| Key | Family |
|---|---|
| `sans` | `FontFamily.Default` — the platform's own UI font (upstream's `ui-sans-serif`/`system-ui` head) |
| `serif` | `Georgia, Cambria, Times New Roman, Times, serif` |
| `mono` | `Menlo, Monaco, Consolas, Liberation Mono, Courier New, monospace` |

`leading-*`/`lh-*`/`line-height-*` and `tracking-*` have no scale: upstream's entries are unitless
line-height multipliers and `em` letter-spacing values, which have no absolute Avalonia equivalent —
only their `[Npx]` and bare-rem forms resolve.

### Breakpoints

| Key | px |
|---|---|
| `sm` | 640 |
| `md` | 768 |
| `lg` | 1024 |
| `xl` | 1280 |
| `2xl` | 1536 |

### Colors

Key format is `{family}-{shade}` (e.g. `blue-500`), plus three bare, shade-less keys: `black`,
`white`, `transparent`. A family name with no shade (`bg-red`) does not resolve. Shades: `50`,
`100`, `200`, `300`, `400`, `500`, `600`, `700`, `800`, `900`, `950` — every family below has all
eleven.

`rose`, `pink`, `fuchsia`, `purple`, `violet`, `indigo`, `blue`, `sky`, `cyan`, `teal`, `emerald`,
`green`, `lime`, `yellow`, `amber`, `orange`, `red`, `gray`, `slate`, `zinc`, `neutral`, `stone`,
`light`, `dark`

Used by `bg-*`, `text-*`, and `border-*`.
