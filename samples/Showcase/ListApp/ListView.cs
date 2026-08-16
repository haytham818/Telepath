using Godot;
using Telepath.Godot;

namespace Telepath.Showcase.ListApp;

[TelepathView<ListViewModel>]
public partial class ListView : Control
{
    [NodeInject("%Draft")]
    [BindTo(nameof(ListViewModel.Draft))]
    [BindTo(nameof(ListViewModel.AddCommand), Kind = LinkKind.Command)]
    private LineEdit _draft = null!;

    [NodeInject("%Add")]
    [BindTo(nameof(ListViewModel.AddCommand), Parameter = nameof(_draft))]
    private Button _add = null!;

    [NodeInject("%Items")]
    [BindTo(nameof(ListViewModel.Items))]
    [BindTo(nameof(ListViewModel.Selected), Kind = LinkKind.Selected)]
    private ItemList _items = null!;

    [NodeInject("%Remove")]
    [BindTo(nameof(ListViewModel.RemoveCommand))]
    private Button _remove = null!;

    [NodeInject("%Clear")]
    [BindTo(nameof(ListViewModel.ClearCommand))]
    private Button _clear = null!;

    [NodeInject("%Choices")]
    [BindTo(nameof(ListViewModel.Items), Kind = LinkKind.Items)]
    private OptionButton _choices = null!;

    public override partial void _Notification(int what);

    private ListViewModel CreateViewModel() => new();
}
