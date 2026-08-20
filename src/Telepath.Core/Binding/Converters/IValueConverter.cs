namespace Telepath.Core;

/// <summary>
/// One-way conversion from a ViewModel value to a target value.
/// Implementations should be stateless and constructible with <c>new()</c>.
/// </summary>
public interface IValueConverter<in TSource, out TTarget>
{
    TTarget Convert(TSource value);
}

/// <summary>
/// Two-way conversion between a ViewModel value and a target value.
/// </summary>
public interface ITwoWayValueConverter<TSource, TTarget> : IValueConverter<TSource, TTarget>
{
    TSource ConvertBack(TTarget value);
}
