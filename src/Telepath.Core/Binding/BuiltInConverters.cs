namespace Telepath.Core;

/// <summary>
/// Built-in <c>T → string</c> conversion. Implicit Label / RichTextLabel bindings
/// pass <see cref="Convert{T}"/>; explicit bindings use <see cref="ToStringConverter{T}"/>.
/// </summary>
public static class ToStringConverter
{
    public static string Convert<T>(T value) => System.Convert.ToString(value) ?? string.Empty;
}

/// <summary>
/// Typed wrapper around <see cref="ToStringConverter.Convert{T}"/> for explicit
/// <c>[LinkTo]</c> Converter declarations.
/// </summary>
public sealed class ToStringConverter<T> : IValueConverter<T, string>
{
    public static ToStringConverter<T> Instance { get; } = new();

    public string Convert(T value) => ToStringConverter.Convert(value);
}

/// <summary>
/// Built-in <c>int ↔ double</c> conversion used by Range bindings.
/// </summary>
public sealed class IntToDoubleConverter : ITwoWayValueConverter<int, double>
{
    public static IntToDoubleConverter Instance { get; } = new();

    public double Convert(int value) => value;

    public int ConvertBack(double value) => (int)value;
}

/// <summary>
/// Built-in <c>float ↔ double</c> conversion used by Range bindings.
/// </summary>
public sealed class FloatToDoubleConverter : ITwoWayValueConverter<float, double>
{
    public static FloatToDoubleConverter Instance { get; } = new();

    public double Convert(float value) => value;

    public float ConvertBack(double value) => (float)value;
}
