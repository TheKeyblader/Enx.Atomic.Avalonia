# Enx.Atomic.Avalonia.Preset.Mini

A concrete rule/variant set for `Enx.Atomic.Avalonia`, inspired by
[`@unocss/preset-mini`](https://unocss.dev/presets/mini). This is the reference for every
utility-class token it registers — see the root
[README.md](../../README.md)/[ARCHITECTURE.md](../../ARCHITECTURE.md) for how the engine itself
resolves them.

## Usage

```csharp
var builder = ThemeBuilder<MiniTheme>.Create();
var configuration = new AtomicConfiguration<MiniTheme> { Theme = builder.Theme };
builder.AddMiniTheme(configuration);
```

`AddMiniTheme` registers every rule, variant, and default theme scale documented below, plus the
ghost-property combiner needed for per-side `m-*`/`p-*`/`rounded-*` tokens to actually resolve.
Everything is exposed individually too (`AddColorRules`, `AddSpacingRules`, ...) if you only want
part of it — see `Extensions/ThemeBuilderExtensions.cs`.

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
| `border` / `border-{width}` | `Border.BorderThicknessProperty` (uniform) | [LineWidths](#linewidths) |
| `border-t/r/b/l-{width}` | Border width, one side (ghost property) | [LineWidths](#linewidths) |
| `rounded-{radius}` | `Border.CornerRadiusProperty` (uniform) | [Radii](#radii) |
| `rounded-t/b/l/r-{radius}` | Two adjacent corners (ghost properties, combined) | [Radii](#radii) |
| `rounded-tl/tr/br/bl-{radius}` | One corner (ghost property) | [Radii](#radii) |
| `w-{size}` / `h-{size}` | `Layoutable.WidthProperty` / `HeightProperty` | [Sizes](#sizes) |
| `min-w-{size}` / `max-w-{size}` | `Layoutable.MinWidthProperty` / `MaxWidthProperty` | [Sizes](#sizes) |
| `min-h-{size}` / `max-h-{size}` | `Layoutable.MinHeightProperty` / `MaxHeightProperty` | [Sizes](#sizes) |
| `m-{n}` | `Layoutable.MarginProperty` (uniform) | [Spacing](#spacing) |
| `mx-{n}` / `my-{n}` | Margin left+right / top+bottom (ghost properties, combined) | [Spacing](#spacing) |
| `mt-{n}` / `mr-{n}` / `mb-{n}` / `ml-{n}` | Margin, one side (ghost property) | [Spacing](#spacing) |
| `-m-{n}`, `-ml-{n}`, ... | Same as above, negated | [Spacing](#spacing) |
| `p-{n}` | `Decorator.PaddingProperty` (uniform) | [Spacing](#spacing) |
| `px-{n}` / `py-{n}` | Padding left+right / top+bottom (ghost properties, combined) | [Spacing](#spacing) |
| `pt-{n}` / `pr-{n}` / `pb-{n}` / `pl-{n}` | Padding, one side (ghost property) | [Spacing](#spacing) |
| `gap-{n}` | `Spacing`/`ColumnSpacing`/`RowSpacing` on `StackPanel`, `Grid`, `UniformGrid` | [Spacing](#spacing) |
| `gap-x-{n}` | `ColumnSpacing` on `Grid` and `UniformGrid` only | [Spacing](#spacing) |
| `gap-y-{n}` | `RowSpacing` on `Grid` and `UniformGrid` only | [Spacing](#spacing) |
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
  handler that syncs the real collection — see `GridDefinitions` in `Dynamic/GridRules.cs`. Only wired
  up for the build-time code-gen path today, not runtime resolution.
- **`col-*`/`row-*`/`col-span-*`/`row-span-*` target the grid child, not the `Grid`** — they're
  `AttachedProperty`s meant to be set on whatever control sits inside the grid (e.g.
  `<Border Classes="col-span-2">`), not on the `Grid` element itself.
- **Per-side margin/padding/radius/border-width** (`ml-*`, `pt-*`, `rounded-tl-*`, `border-t-*`, ...)
  go through ghost properties: real, hidden `AvaloniaProperty`s combined back into one real
  struct-valued setter by `GhostPropertyCombiner` when several land on the same element (`ml-1 mr-2` →
  one `Margin` setter, not two competing ones). Requires `AddGhostPropertyCombiner` (already part of
  `AddMiniTheme`).
- **`text-` is shared** between `FontSizeRule` and `ForegroundColorRule` — a value that resolves in
  the `FontSizes` scale wins as a size; otherwise it's tried as a color. Likewise **`border-`** is
  shared between `BorderWidthRule` and `BorderColorRule` — a value resolving in `LineWidths` (named
  key or bare number) wins as a width, otherwise it's tried as a color.
- **`rounded` alone does not match anything** — the value segment is mandatory. Use `rounded-DEFAULT`
  for the base 4px radius (unlike `border`, whose value segment is optional and also defaults to
  `DEFAULT`).
- **Arbitrary values** (`bg-[#ff0000]`, `w-[123px]`, `m-[10px]`, `rounded-[6px]`, `border-[3px]`,
  `text-[18px]`) bypass the theme scale entirely — see [Arbitrary values](#arbitrary-values) below.
- **`w-*`/`h-*` use the small named [Sizes](#sizes) scale, not [Spacing](#spacing)** — this differs
  from Tailwind, where `w-*` shares the spacing scale. A bare number here (e.g. `w-64`) that isn't a
  named `Sizes` key falls back to being read as **rem** (`64rem × 16 = 1024px`), not the spacing
  scale's `64 → 256px`.
- Every scale below also accepts a bare number with no unit as a fallback when the key isn't found
  by name — treated as **rem** (`× RemToPxFactor`, default 16) everywhere *except*
  [LineWidths](#linewidths), which treats it as raw **px** (matching UnoCSS's `lineWidth` scale).

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
- No bracket support yet for non-numeric, non-color scale-driven values (there aren't any in this
  preset today) or for structural tokens (`grid-cols-*`, `col-span-*`, ...), which don't read a
  theme scale in the first place.

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

Used **only** by `w-*`/`h-*`/`min-w-*`/`max-w-*`/`min-h-*`/`max-h-*` — see the note above about how
this differs from [Spacing](#spacing).

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
