---
name: Port a UnoCSS rule
description: Port a rule, variant or theme scale from @unocss/preset-mini (CSS) to Enx.Atomic.Avalonia's Preset.Mini (Avalonia properties, selectors, container queries). Use before adding any token that exists upstream — gives the upstream file locations and the established CSS → Avalonia mapping.
---

# Port from `@unocss/preset-mini`

Port from the **real upstream source**, not from memory. The `unocss` reference (see
`opencode.jsonc`) is a checkout of `unocss/unocss@main`:

| What | Upstream path |
|---|---|
| Rules | `packages-presets/preset-mini/src/_rules/*.ts` |
| Theme scales | `packages-presets/preset-mini/src/_theme/*.ts` |
| Variants | `packages-presets/preset-mini/src/_variants/*.ts` |
| Value parsing helpers (`h.bracket`, `h.rem`, `h.px`, ...) | `packages-presets/rule-utils/src`, `packages-presets/preset-mini/src/_utils` |

Avalonia semantics: the `avalonia` reference (`AvaloniaUI/Avalonia@release/12.1.3`, e.g.
`src/Avalonia.Base/Styling/`) and `avalonia-docs`.

## 1. Read upstream, then decide what survives

For the rule: its regex, which scale it reads, its fallbacks (`h.bracket.cssvar.global.rem` etc.),
and the CSS properties it emits. Then map each CSS property to an Avalonia property — **or drop
it**. Not everything has an equivalent; a partial port documented in the `<summary>` beats an
invented approximation. Unsupported upstream features (CSS variables, `calc()`, `!important`,
structural pseudo-classes like `:nth-child`) are dropped deliberately.

## 2. Established mappings (keep consistent)

| CSS / UnoCSS | Avalonia | Notes |
|---|---|---|
| `margin` / `padding` | `MarginSides` / `PaddingSides` side properties, recomposed into `Layoutable.MarginProperty` / `Decorator.PaddingProperty` | every direction writes the host's `double` sides (one/two/four), target `Control` |
| `gap` / `gap-x` / `gap-y` | `StackPanel.SpacingProperty`, `Grid`/`UniformGrid` `ColumnSpacing`/`RowSpacing` | see `GapRule` |
| `width`/`height`/`min-*`/`max-*` | `Layoutable.*Property` (`double`) | |
| `border-width` | `BorderThicknessSides` sides, recomposed into `Border.BorderThicknessProperty` (`Thickness`) | `lineWidth` scale = **px**, not rem → `TryResolvePx` |
| `border-radius` | `CornerRadiusSides` sides, recomposed into `Border.CornerRadiusProperty` | slot order TopLeft/TopRight/BottomRight/BottomLeft (`CompositeKind`) |
| `background-color` | `Border`/`TemplatedControl`/`Panel` `.BackgroundProperty` (3 entries, explicit target types) | `ToResource` for theme colors |
| `color` | `TextElement.ForegroundProperty` | |
| `font-size` / `font-weight` / `font-style` | `TextElement.FontSize/FontWeight/FontStyleProperty` | |
| `font-family` | `TextElement.FontFamilyProperty` | a `FontFamily` value needs `FontFamilyValueEmitter` |
| `opacity` | `Visual.OpacityProperty` (`double` 0–1; a bare token is a percentage) | |
| `z-index` | `Visual.ZIndexProperty` (`int`) | |
| `line-height` | `TextBlock.LineHeightProperty` (absolute px; no multiplier form) | |
| `letter-spacing` | `TextElement.LetterSpacingProperty` (absolute px; `em` upstream) | |
| `top`/`right`/`bottom`/`left`/`inset` | `Canvas.{Top,Right,Bottom,Left}Property` (only inside a `Canvas`) | |
| `text-align` | `TextBlock.TextAlignmentProperty` | |
| `display: none` | `Visual.IsVisibleProperty = false` | |
| `overflow: hidden` | `Visual.ClipToBoundsProperty` | |
| `flex-direction` | `Orientation` on `StackPanel`/`WrapPanel`/... | |
| `justify-self`/`align-self` | `HorizontalAlignment`/`VerticalAlignment` | |
| `grid-template-*`, `grid-column/row` | `Grid.ColumnDefinitions`/`RowDefinitions`, attached `Grid.Column/Row/Span` | codegen-only |
| `cursor` | `InputElement.CursorProperty` (`StandardCursorType`) | |
| `pointer-events` | `InputElement.IsHitTestVisibleProperty` | |
| `rem` units | px × `RemToPxFactor` (default 16) | `DefaultTheme` stores px already |
| `[arbitrary]` | `ArbitraryValue.TryUnwrap` → px literal / `Color.TryParse` | never rem, never a resource |
| `hover:`/`focus:`/`disabled:`/... | Avalonia pseudo-classes (`:pointerover`, ...) | `PseudoClassVariant` |
| `sm:`/`max-sm:` (media queries) | **container queries** (`min-width`/`max-width`) | `BreakpointVariant` |
| `dark:` (class/media) | `PropertyEquals(ThemeVariantScope.ActualThemeVariantProperty, Dark)` | `DarkVariant` |

Three things UnoCSS gets for free need to be explicit here, because Avalonia has neither:

- **The cascade.** UnoCSS's stylesheet order is its specificity/media order; Avalonia ties every
  class-activated style at `StyleTrigger` and resolves ties last-inserted-wins. Never assume
  source order — `StyleOrdering` owns the emission order (see ARCHITECTURE.md, "Emission order").
- **Per-direction rule entries.** UnoCSS keeps `m-*`, `mx-*`, `ml-*` as separate rules in that
  increasing order. A consolidated rule here implements `IOrderRankedDynamicRule<TTheme>` and
  ranks its own matches (uniform < axis/edge < side/corner) — see `add-dynamic-rule` §4.
- **Struct-valued properties.** CSS's four independent `margin-*`/`padding-*`/`border-*-width`/
  `border-radius` properties collapse into a composite host's four `double` sides in Avalonia
  (`MarginSides`/`PaddingSides`/`BorderThicknessSides`/`CornerRadiusSides`), recomposed by a class
  handler — see `add-dynamic-rule` §3.

## 3. Theme scales

Port `_theme/*.ts` into `Extensions/DefaultTheme.cs`, converting rem → px at 16px/rem
(`0.25rem` → `4`). Keep upstream key names so tokens read the same as in UnoCSS docs.

## 4. Implement

Then load the specific skill: `add-static-rule`, `add-dynamic-rule`, or `add-variant`. In the new
class's `<summary>`, cite the upstream file and list what was intentionally not ported.

## 5. Finish

`/verify`. When asked to document: `/update-docs` (Preset.Mini README token tables).
