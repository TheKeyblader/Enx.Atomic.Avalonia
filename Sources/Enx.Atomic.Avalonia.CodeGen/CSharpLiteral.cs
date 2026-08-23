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

    // A whole-number double (e.g. 448.0) formats as "448" with no decimal point or suffix — parsed back as
    // a C# *int* literal wherever it lands in an `object?`-typed argument (e.g. Setter(AvaloniaProperty,
    // object? value)), since there's no implicit-conversion context there to make it a double the way there
    // is for, say, a Thickness(double, double, double, double) constructor argument. Boxed as the wrong CLR
    // type, it then fails at runtime when Avalonia expects a boxed double. The "d" suffix, like Float's "f",
    // forces the literal to always parse as double regardless of whether it has a fractional part.
    public static string Double(double value) =>
        value switch
        {
            double.PositiveInfinity => "double.PositiveInfinity",
            double.NegativeInfinity => "double.NegativeInfinity",
            double.NaN => "double.NaN",
            _ => $"{value.ToString(CultureInfo.InvariantCulture)}d",
        };

    public static string Float(float value) => $"{value.ToString(CultureInfo.InvariantCulture)}f";
}
