using System.Globalization;
using System.Text;

namespace Enx.Atomic.Avalonia.CodeGen;

/// <summary>Formats primitive .NET values as the C# literal text an emitted source file would use.</summary>
public static class CSharpLiteral
{
    public static string String(string value)
    {
        var builder = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }
        return builder.Append('"').ToString();
    }

    public static string Double(double value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Float(float value) => $"{value.ToString(CultureInfo.InvariantCulture)}f";
}
