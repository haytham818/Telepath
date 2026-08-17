using System.Globalization;
using Telepath.Core;

namespace Telepath.Showcase.FormApp;

public sealed class IntStringConverter : ITwoWayValueConverter<int, string>
{
    public string Convert(int value) => value.ToString(CultureInfo.InvariantCulture);

    public int ConvertBack(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
}
