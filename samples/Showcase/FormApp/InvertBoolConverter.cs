using Telepath.Core;

namespace Telepath.Showcase.FormApp;

public sealed class InvertBoolConverter : ITwoWayValueConverter<bool, bool>
{
    public bool Convert(bool value) => !value;

    public bool ConvertBack(bool value) => !value;
}
