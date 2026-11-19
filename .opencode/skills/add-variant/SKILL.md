---
name: Add a variant
description: Add a token prefix/suffix variant (hover:, sm:, max-sm:, dark:, group-*, ...) — a VariantBase<TTheme> + VariantHandlerBase transforming the resolved SelectorExpression or StyleQueryExpression. Use for anything that changes *when/where* a utility applies rather than *what* it sets.
---

# Add a variant

Variants strip a prefix (`hover:`) from the token, then wrap the rule's result: the handler gets
the context after `next(input)` and rewrites its `Selector` (`SelectorExpression`) and/or
`ContainerQuery` (`StyleQueryExpression`). Matching repeats until no variant matches, which is how
`hover:focus:...` chains.

## 1. Pick the Avalonia mechanism

| Intent | Avalonia | Data node (`Sources/Enx.Atomic.Avalonia/Compact/`) |
|---|---|---|
| Control state (`hover:`) | pseudo-class `:pointerover` | `Selector.Class(":pointerover")` |
| Property state (`dark:`) | `Selectors.PropertyEquals` | `Selector.PropertyEquals(prop, value, declaringType)` |
| Size (`sm:`, `max-sm:`) | container query | `ContainerQuery.Width(op, size)` |
| Type restriction | `Selectors.Is`/`OfType` | `Is` / `OfType` |

- A plain state pseudo-class → just add an entry to `PseudoClassVariant`'s dictionary
  (`Preset.Mini/Variants/PseudoClassVariant.cs`) — no new class. Check in the `avalonia` reference
  that the control really raises it.
- **Missing node** (e.g. `/template/`, descendant `Child`/`Descendant` combinators): adding one to
  `SelectorExpression`/`StyleQueryExpression` also requires `SelectorEmitter`/`StyleQueryEmitter`
  support and the runtime compile path (`AtomicGenerator.ApplyVariants`). That's roadmap-sized —
  discuss the design first.
- **Never** build a compiled `Expression<Func<Selector, Selector>>` and inspect it; work on the
  data trees only.

## 2. Write it

In `Sources/Enx.Atomic.Avalonia.Preset.Mini/Variants/`, namespace
`Enx.Atomic.Avalonia.Preset.Mini.Variants`. Model on `DarkVariant.cs` (simplest) or
`BreakpointVariant.cs` (theme-driven, container query):

```csharp
public record MyVariant<TTheme> : VariantBase<TTheme>
    where TTheme : class
{
    public MyVariant() => MultiPass = true; // may match again in a chained token

    public override VariantHandlerBase[] Match(string matcher, VariantContext<TTheme> context)
    {
        const string prefix = "my:";
        if (!matcher.StartsWith(prefix, StringComparison.Ordinal))
            return [];
        return [new MyVariantHandler { Matcher = matcher[prefix.Length..] }];
    }
}

public record MyVariantHandler : VariantHandlerBase
{
    public override VariantHandlerContext Handle(VariantHandlerContext input, Func<VariantHandlerContext, VariantHandlerContext> next)
    {
        var result = next(input);
        return result with { Selector = result.Selector.Class(":my-state") };
    }
}
```

- `Matcher` = the remaining token; `Order` on the variant (try order) / handler (pipeline order,
  lower = outermost).
- Theme-driven variants add `where TTheme : class, I...Part` and read `context.Theme`.
- **`PropertyEquals` on an `AddOwner`'d / inherited property** must pass the public declaring type
  explicitly (`typeof(ThemeVariantScope)` in `DarkVariant`) — `OwnerType` reports the original,
  possibly internal owner and the emitted code wouldn't compile.
- Prefix collisions: `PseudoClassVariant` tries names in dictionary order with `StartsWith` —
  make sure a new prefix isn't a prefix of an existing one (`focus:` vs `focus-visible:`
  works only because of the `:`).

## 3. Register

Add/extend an `Add*Variant(s)` method in `Extensions/ThemeBuilderExtensions.cs` using
`configuration.AddVariantOnce(new MyVariant<TTheme>())`, and chain it in `AddMiniTheme`.

## 4. Tests

- `Tests/Enx.Atomic.Avalonia.Tests/VariantTests.cs`: `generator.ParseToken("my:bg-red-500")`,
  then assert on `util.ResolveSelector().ToString()` / `util.ResolveContainerQuery()`. Cover
  chaining with an existing variant (`hover:my:...`) and a multi-owner rule (`bg-*` → 3 utils).
- `CodeGenTests.cs`: add a token using the variant to the compiled source — this is what catches
  an emitter that doesn't know a node, or a non-public declaring type.

## 5. Finish

Run `/verify`. Docs only when asked (`/update-docs`; the Preset.Mini README has a Variants table).
