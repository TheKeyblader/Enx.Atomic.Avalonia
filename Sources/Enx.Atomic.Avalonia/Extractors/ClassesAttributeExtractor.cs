using System.Text.RegularExpressions;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Extracts the class name from Avalonia's conditional class syntax, <c>Classes.line-through="{Binding IsDone}"</c>,
/// which the XAML compiler turns into <c>Classes.Set("line-through", value)</c>: the class a control ends up with
/// is exactly the attribute name's suffix, so a utility token used there needs its style generated like any other.
/// <see cref="SplitExtractor"/> can't see it — it doesn't split on <c>.</c> or <c>=</c> (splitting on <c>.</c>
/// would break tokens like <c>p-0.5</c>), so the whole attribute reaches it as one unmatchable candidate.
/// <para>
/// The suffix is an XML attribute name, which bounds what can be toggled this way: no variant (<c>:</c> is read as a
/// namespace prefix and fails XAML parsing), no arbitrary <c>[...]</c> value and no <c>/</c> (not legal in an XML
/// name). A name containing a <c>.</c> (<c>Classes.p-0.5</c>) is deliberately not matched, as it's ambiguous with
/// the attribute syntax itself. For any of those, toggle the class from code-behind instead —
/// <c>Classes.Set("dark:line-through", isDone)</c> — whose quoted string <see cref="SplitExtractor"/> already picks up.
/// </para>
/// </summary>
public partial class ClassesAttributeExtractor : Extractor
{
    /// <inheritdoc/>
    public override void Extract(ExtractorContext context)
    {
        foreach (Match match in ClassesAttributeRegex().Matches(context.Code))
            context.Extracted.Add(match.Groups[1].Value);
    }

    /// <summary>Matches a <c>Classes.&lt;name&gt;=</c> attribute, capturing <c>&lt;name&gt;</c> (letters, digits, <c>_</c> and <c>-</c>).</summary>
    [GeneratedRegex("""\bClasses\.([\w-]+)\s*=""")]
    public static partial Regex ClassesAttributeRegex();
}
