---
name: Add a value emitter
description: Teach Enx.Atomic.Avalonia.CodeGen to emit a new Setter/selector value type as C# (IValueEmitter + ValueEmitterRegistry). Use when codegen throws "No value emitter registered for type ...", or when a new rule produces a value type the emitter has never seen.
---

# Add a value emitter (CodeGen)

`StyleEmitter`/`ResourceDictionaryEmitter` turn every runtime value into C# source via
`ValueEmitterRegistry` (`Sources/Enx.Atomic.Avalonia.CodeGen/Values/`), which picks the **first**
registered `IValueEmitter` whose `CanHandle(type)` is true.

## 1. Can it be reconstructed from data?

The emitter only sees the runtime object. It must rebuild an equivalent value **from its public
state** — never from a compiled `Expression`, never via reflection into private fields. If the
object doesn't expose enough (e.g. `Cursor` built from a bitmap), support the subset you can and
throw `NotSupportedException` with a clear message for the rest — see `CursorValueEmitter.cs`.

Existing emitters, in order: `PrimitiveValueEmitter`, `EnumValueEmitter`, `ThicknessValueEmitter`,
`CornerRadiusValueEmitter`, `BoxShadowsValueEmitter`, `BrushValueEmitter`, `FontFamilyValueEmitter`,
`CursorValueEmitter`, `TextDecorationsValueEmitter`, `TextTrimmingValueEmitter`,
`ThemeVariantValueEmitter`, `GridDefinitionsValueEmitter`, `DynamicResourceValueEmitter`. Extend one if
the type is a close relative.

## 2. Write it

`IValueEmitter` implementations are a **public, documented extension point** (callers can pass
their own list to `StyleEmitter.Emit`), so the class is `public sealed`. Helpers it uses stay
`internal` (`CSharpLiteral`, `CSharpTypeNaming`, `AvaloniaPropertyNaming`).

```csharp
namespace Enx.Atomic.Avalonia.CodeGen.Values;

/// <summary>Emits a <see cref="Foo"/> ... — explain what's reconstructible and what throws.</summary>
public sealed class FooValueEmitter : IValueEmitter
{
    public bool CanHandle(Type type) => type == typeof(Foo);
    public IEnumerable<string> GetNamespaces(object value) => ["Avalonia.Foo"];
    public string Emit(object value) => $"new Foo({CSharpLiteral.Double(((Foo)value).Bar)})";
}
```

- Numeric literals must carry the exact C# type of the target (`1d`, `1f`, ...) — use
  `CSharpLiteral`; a bare `1` boxes as `int` and only fails when Avalonia applies the setter.
- Return every namespace the emitted text needs from `GetNamespaces` — the generated file must
  compile standalone (and `CodeGen` never references `Preset.Mini`).
- Use `typeof(IFoo).IsAssignableFrom(type)` in `CanHandle` for interface-typed values (see
  `BrushValueEmitter` for `ISolidColorBrush`). Mind order: put a specific emitter before a broader one that would also
  claim the type.

## 3. Register

Add `new FooValueEmitter()` to `ValueEmitterRegistry.Default` at the right position.

## 4. Tests

A real **compile-and-run** check, not a string comparison:

- Add a token producing the value to the source in
  `Tests/Enx.Atomic.Avalonia.Tests/CodeGenTests.cs` → it compiles the emitted file with Roslyn,
  instantiates the `Styles`, and asserts each `Setter.Value` is an instance of the property type.
- If the value is a resource (theme scale entry), also cover `ResourceDictionaryEmitter` in
  `ThemeResourceTests.cs`.
- For the unsupported branch, a unit test asserting `NotSupportedException`.

## 5. Finish

Run `/verify` (the MSBuild integration tests in `MSBuildIntegrationTests.cs` also exercise the
emitter end to end).
