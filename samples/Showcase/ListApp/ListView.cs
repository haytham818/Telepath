using Godot;
using Telepath.Core;
using Telepath.Godot;

namespace Telepath.Showcase.ListApp;

[TelepathView<ListViewModel>]
public partial class ListView : Control
{
    [LinkTo("%Draft", nameof(ListViewModel.Draft))]
    [LinkTo("%Draft", nameof(ListViewModel.AddCommand), Kind = LinkKind.Command)]
    private LineEdit _draft = null!;

    [LinkTo("%Add", nameof(ListViewModel.AddCommand), Parameter = nameof(_draft))]
    private Button _add = null!;

    [LinkTo("%Items", nameof(ListViewModel.Selected), Kind = LinkKind.Selected)]
    private ItemList _items = null!;

    [LinkTo("%Remove", nameof(ListViewModel.RemoveCommand))]
    private Button _remove = null!;

    [LinkTo("%Clear", nameof(ListViewModel.ClearCommand))]
    private Button _clear = null!;

    private OptionButton _choices = null!;

    public override partial void _Notification(int what);

    private ListViewModel CreateViewModel() => new();

    private void OnReady() => _choices = GetNode<OptionButton>("%Choices");

    private void OnBind(ListViewModel vm, BindingSet bindings)
    {
        bindings.BindItems(vm.Items, _items.Items());
        bindings.BindItems(vm.Items, _choices.Items());
    }
}
