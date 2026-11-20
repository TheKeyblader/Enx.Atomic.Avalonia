---
name: Add a dynamic rule
description: Add a regex/theme-driven utility family to Preset.Mini (m-*, bg-*, rounded-tl-*, w-[123px], ...) — an IDynamicRule<TTheme> resolving values from a theme part, arbitrary [..] values or bare numbers. Covers theme parts, registration order for shared prefixes, composite side properties and resources.
---

# Add a dynamic rule (Preset.Mini)

Read ARCHITECTURE.md sections "Core engine", "`Enx.Atomic.Avalonia.Preset.Mini`",
"Emission order" and "Composite side properties" first if you haven't this session.

## 1. Scope it

- Check `Sources/Enx.Atomic.Avalonia.Preset.Mini/README.md` ("Dynamic utilities") — extend an
  existing rule class rather than adding a competing one on the same prefix.
- Porting from UnoCSS? Load `port-unocss-rule` and read the upstream rule in the `unocss`
  reference first.
- **One class per family**, not per direction: use named capture groups (`side`, `axis`, `bound`,
  `neg`, `value`) like `MarginRule`/`RoundedRule` (`Dynamic/SpacingRules.cs`, `Dynamic/RadiusRules.cs`).

## 2. Theme part

A rule declares the parts it needs via `where TTheme : class, I...Part` — only those it reads.

- Existing scale fits (`ISpacingPart.Spacing`, `ISizePart.Sizes`, `IRadiusPart.Radii`,
  `ILineWidthPart.LineWidths`, `IFontSizePart.FontSizes`, `IColorPart.Colors`,
  `IBreakpointPart.Breakpoints`, + `IRemToPxPart.RemToPxFactor`) → reuse it.
- New scale → new `Parts/I<Name>Part.cs` interface, implement it on `MiniTheme`, add its default
  values to `Extensions/DefaultTheme.cs` (port from `preset-mini/src/_theme/*.ts`, rem → px at
  16px/rem), and a new `Add<Name>Rules` extension in `ThemeBuilderExtensions.cs` that seeds
  `builder.Theme.<Scale>` and registers the rule.

## 3. Write the rule

In `Sources/Enx.Atomic.Avalonia.Preset.Mini/Dynamic/`, namespace
`Enx.Atomic.Avalonia.Preset.Mini.Dynamic`:

```csharp
/// <summary>Dynamic rule setting ... from <see cref="ISizePart.Sizes"/> (<c>w-*</c>, ...). Explain the *why*.</summary>
public partial class MyRule<TTheme> : IDynamicRule<TTheme>
    where TTheme : class, ISizePart, IRemToPxPart
{
    public RuleMetadata Metadata { get; init; } = new();
    public Regex Regex { get; } = CompiledRegex();

    public ImmutableArray<StyleValue> Match(Match match, RuleContext<TTheme> context)
    {
        if (!context.Theme.Sizes.TryResolve(match.Groups["value"].Value, context.Theme.RemToPxFactor, out var value))
            return []; // empty = miss → the next rule in declaration order gets a chance

        return [Layoutable.WidthProperty.ToLiteral(value)];
    }

    [GeneratedRegex("^w-(?<value>.+)$")]
    private static partial Regex CompiledRegex();
}
```

Value resolution — always go through `ThemeScale` (`Dynamic/ThemeScale.cs`), never parse by hand:

| Helper | Order tried | Bare number means |
|---|---|---|
| `TryResolve` | `[arbitrary]` → scale key → bare number | **rem** × `RemToPxFactor` (spacing, sizes, radii, font sizes) |
| `TryResolvePx` | `[arbitrary]` → scale key → bare number | **px** as-is (line widths: border/ring/outline) |
| `TryResolveUnscaled` | `[arbitrary]` → bare number | **rem** × `RemToPxFactor`; no scale — for a family whose upstream entries are relative (`leading-*` multipliers, `tracking-*` `em`) |

Arbitrary `[...]` is always px (optional `px` suffix, other units rejected), never rem, and always a
`StyleValue.Literal`. Colors don't use `ThemeScale`: reuse `ArbitraryColor` in `ColorRules.cs`.

`Literal` vs `Resource`:

- Value is a **named theme entry that should follow Light/Dark** (colors today) → 
  `property.ToResource((TTheme t) => t.Colors[value])`. The resource key is derived from that
  expression — never hand-type a key. Only works through codegen (runtime path resolves nothing —
  that's expected).
- Everything else (and every arbitrary value) → `property.ToLiteral(value)`.

Property gotchas:

- **Numeric type must match the property exactly** (`double` for `WidthProperty`; scales are
  `float` or `double` — convert explicitly). Wrong boxing compiles and fails at runtime.
- **Shared `AddOwner` properties** need one entry per owner with an explicit target type
  (`TemplatedControl.BackgroundProperty.ToLiteral(v, typeof(TemplatedControl))`) — see
  `BackgroundColorRule`.
- **Attached properties** default their target to `StyledElement`; narrow it explicitly if needed.
- **Struct-valued properties set per side** (`Thickness`, `CornerRadius`): never write the
  real property. Every direction — the uniform one included — writes the four
  `AttachedProperty<double>` of the family's composite host in `Composites/`
  (`MarginSides`, `PaddingSides`, `BorderThicknessSides`, `CornerRadiusSides`), with the
  scale value converted to `double` and `typeof(Control)` passed as the target type; the
  host's class handler recomposes the real `Thickness`/`CornerRadius` at
  `BindingPriority.Style`. A new four-sided property needs a new host (four fields in the
  `CompositeKind` slot order, a `CompositePropertyDescriptor`, a class handler,
  `[EmittableGhostPropertyHost]`) — see `ARCHITECTURE.md`, "Composite side properties".

## 4. Register — order matters

Add the rule in the matching `Add*Rules` method via `configuration.AddRuleOnce(new MyRule<TTheme>())`,
and make sure that method is chained in `AddMiniTheme`.

Static rules always win; among dynamic rules **the first registered match wins**. If your regex
shares a prefix with an existing rule (`border-`, `text-`, `bg-`, ...), the more specific rule
(the one that returns `[]` when its scale lacks the key) must be registered **before** the generic
one — e.g. `AddLineWidthRules`/`AddFontSizeRules` before `AddColorRules`. Document the ordering
constraint in the `Add*Rules` `<summary>`.

Registration order says nothing about how *one* rule's matches rank against each other. If your
rule consolidates what UnoCSS keeps as several entries (uniform vs axis vs side, edge vs corner),
implement `IOrderRankedDynamicRule<TTheme>` and return the ascending rank from `GetOrderRank` —
`StyleOrdering` consumes it right after the rule index, so `m-4 mx-2` lets the axis win whatever
the source order. Don't try to encode that with `StringifiedUtil.Sort`, which is a variant hint.

## 5. Tests

`[AvaloniaFact]` tests using `TestHelpers.CreateMiniGenerator()` + `generator.ParseToken("...")`,
in the family's file (`SpacingRuleTests.cs`, `BorderRuleTests.cs`, ...) or a new
`<Family>RuleTests.cs`. Cover:

- a scale key, a bare number (check the rem conversion), an arbitrary `[..px]` value, a miss;
- each named group branch (each `side`/`axis`);
- shared prefix → a case in `AmbiguousPrefixTests.cs` both ways;
- per-side/composite values → assert the *effective* property on a shown control, not just the
  setters: `generator.Generate(...)` then `utils.ShowWithStyles(control)` (`CompositeUtilityTests.cs`
  is the model), including a variant and a lone side token;
- rank → a case in both source orders (`"m-4 mx-2"` / `"mx-2 m-4"`) asserting the same effective
  value, as `SpacingRuleTests`/`CompositeUtilityTests` do;
- resources → `ThemeResourceTests.cs` (key, `ResolvedResources`, Light/Dark split);
- **codegen**: add a representative token to the source in `CodeGenTests.cs` (compiled with
  Roslyn, setter types checked). New value type → `add-value-emitter`.

## 6. Finish

Run `/verify`. Docs only when asked (`/update-docs`).
