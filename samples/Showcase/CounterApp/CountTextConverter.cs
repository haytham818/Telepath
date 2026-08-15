using Telepath.Core;

namespace Telepath.Showcase.CounterApp;

public sealed class CountTextConverter : IValueConverter<int, string>
{
    public string Convert(int value) => $"Count: {value}";
}
