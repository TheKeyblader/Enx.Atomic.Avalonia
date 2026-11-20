---
name: Add a static rule
description: Add a fixed-name utility token to Preset.Mini (e.g. cursor-pointer, truncate, flex-row) — a Rule.Static with a fixed set of StyleValues, no theme lookup. Use for any token whose value never depends on the theme or on part of the token.
---

# Add a static rule (Preset.Mini)

A static rule matches a token by **exact name** and carries a fixed `ImmutableArray<StyleValue>`.
It always wins over dynamic rules for the same token.

## 1. Check it doesn't exist / isn't dynamic

- Look the token up in `Sources/Enx.Atomic.Avalonia.Preset.Mini/README.md` ("Static utilities").
- If the value comes from a scale or from part of the token (`w-4`, `bg-red-500`), it's a
  **dynamic** rule — load `add-dynamic-rule` instead.
- Porting from UnoCSS? Load `port-unocss-rule` first for the CSS → Avalonia mapping.

## 2. Write the rule

Static rules live in `Sources/Enx.Atomic.Avalonia.Preset.Mini/Static/`, one `public static class`
per concern, exposing `public static readonly Rule.Static[] All`. Add to an existing file when the
concern matches (e.g. a new cursor → `Cursor.cs`), otherwise create one:

```csharp
using Avalonia.Controls;

namespace Enx.Atomic.Avalonia.Preset.Mini;

/// <summary>Static rules setting <see cref="TextBlock.TextWrappingProperty"/> ... (explain *why*/gotchas, not what).</summary>
public static class TextWrap
{
    public static readonly Rule.Static[] All =
    [
        new("whitespace-nowrap", [TextBlock.TextWrappingProperty.ToLiteral(TextWrapping.NoWrap)]),
    ];
}
```

- Namespace is `Enx.Atomic.Avalonia.Preset.Mini` (not `.Static`), file-scoped.
- Build values with `property.ToLiteral(value)` (`AvaloniaExtensions`). Static rules never use
  `ToResource` — that's for theme-scale values.
- **Shared/`AddOwner` properties**: if the same `AvaloniaProperty` instance is exposed by several
  unrelated types, emit one entry per type and pass the target explicitly:
  `TemplatedControl.BackgroundProperty.ToLiteral(v, typeof(TemplatedControl))`. Without it, all
  entries collapse onto the original `OwnerType`. See `StyleValue.TargetType` and
  `Static/FlexDirection.cs` (one entry per oriented control).
- **Struct properties set per side** (`Thickness`, `CornerRadius`) must go through the
  family's composite host, not the real property: write e.g.
  `MarginSides.MarginLeftProperty.ToLiteral(value, typeof(Control))` (uniform forms write all
  four sides). The host's class handler recomposes the real value — see ARCHITECTURE.md
  "Composite side properties".
- **Two static tokens setting the same property** resolve in `StringifiedUtil.Index` order
  (their position in `AllStaticRules`), never in source order — see ARCHITECTURE.md
  "Emission order".
- The value must be **exactly** the property's type (`double`, not `int`/`float`) — the codegen
  boxes it into `Setter.Value` and only fails at apply time.

## 3. Register it

New class → add `.. YourClass.All,` to `AllStaticRules` in
`Sources/Enx.Atomic.Avalonia.Preset.Mini/Extensions/ThemeBuilderExtensions.cs`. Adding to an
existing class's `All` needs nothing else. Dedup is by `Name`, so a duplicate name is silently
dropped — grep for the token first.

## 4. Codegen support

If the value's type is new to the pipeline (not bool/numeric/string/enum/Thickness/CornerRadius/
brush/FontFamily/Cursor/TextDecorations/ThemeVariant), codegen will throw
`No value emitter registered for type ...` — load `add-value-emitter`.

## 5. Tests

In `Tests/Enx.Atomic.Avalonia.Tests/StaticRuleTests.cs`, `[AvaloniaFact]` (headless platform is
required — some static values construct real Avalonia objects):

```csharp
[AvaloniaFact]
public void WhitespaceNowrap_SetsNoWrap()
{
    var (_, generator) = TestHelpers.CreateMiniGenerator();

    var util = Assert.Single(generator.ParseToken("whitespace-nowrap"));
    var setter = Assert.Single(util.Body);
    Assert.Equal(TextBlock.TextWrappingProperty, setter.Property);
    Assert.Equal(TextWrapping.NoWrap, setter.Value);
}
```

If the token reaches a composite host (or its effect is only visible once Avalonia applies the
setters), also assert the *effective* value on a shown control: `generator.Generate(...)` then
`utils.ShowWithStyles(control)` — `CompositeUtilityTests.cs` is the model.

If the token exercises a new value type or a multi-owner property, also add it to the source
string in `CodeGenTests.EmittedSource_CompilesAndProducesCorrectlyTypedSetters` (it compiles the
emitted C# with Roslyn and checks every boxed setter type).

## 6. Finish

Run `/verify`. Don't update README/ARCHITECTURE/CHANGELOG unless asked (then use `/update-docs`).
