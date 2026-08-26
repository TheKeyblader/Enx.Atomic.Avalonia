using System.Linq.Expressions;

namespace Enx.Atomic.Avalonia;

/// <summary>
/// Derives a stable resource-dictionary key from a <see cref="StyleValue.Resource.ThemeAccess"/> expression —
/// e.g. <c>t =&gt; t.Colors[value]</c> becomes <c>"Colors[red-500]"</c> — instead of a rule passing one in by
/// hand. Two rules reading the exact same theme-scale entry always agree on the same key this way, and two
/// rules reading different entries can never collide on a key an author mistyped or reused.
///
/// <para>
/// Works entirely off the expression as data, walking member/indexer access nodes structurally — the one
/// exception is an indexer's argument (e.g. <c>value</c> above), which has to be evaluated to turn into text.
/// That's done by compiling *just* that argument subtree on its own, deliberately with no parameters bound —
/// which is exactly why it only supports an argument that doesn't itself depend on the lambda's <c>TTheme</c>
/// parameter (a closed-over local, like every current rule uses, works fine; compilation throws instead of
/// letting an unbound parameter reference silently through).
/// </para>
/// </summary>
internal static class ThemeResourceKey
{
    public static string From(LambdaExpression expression) => Build(expression.Body, expression.Parameters[0]);

    private static string Build(Expression node, ParameterExpression root) =>
        node switch
        {
            ParameterExpression p when p == root => string.Empty,

            UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert => Build(
                convert.Operand,
                root
            ),

            MemberExpression member => Join(Build(member.Expression!, root), member.Member.Name),

            MethodCallExpression { Method.IsSpecialName: true } call when call.Method.Name == "get_Item" => Index(
                Build(call.Object!, root),
                call.Arguments[0]
            ),

            IndexExpression index => Index(Build(index.Object!, root), index.Arguments[0]),

            _ => throw new NotSupportedException(
                $"ThemeResourceKey can't derive a key from a '{node.NodeType}' node ({node}) — only member/indexer access reachable from the theme parameter is supported."
            ),
        };

    private static string Join(string owner, string member) => owner.Length == 0 ? member : $"{owner}.{member}";

    private static string Index(string owner, Expression argument) =>
        $"{owner}[{Expression.Lambda(argument).Compile().DynamicInvoke()}]";
}
