using Telepath.Core;

namespace Telepath.Showcase;

public sealed class CountTextConverter : IValueConverter<int, string>
{
    public string Convert(int value) => $"Count: {value}";
}
