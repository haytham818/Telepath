using Godot;
using Telepath.Core;
using Telepath.Godot;

namespace Telepath.Showcase;

[TelepathView<ShellViewModel>]
public partial class ShellView : Control
{
    [NodeInject("%Content")]
    private Control _content = null!;

    [NodeInject("%Overlay")]
    private Control _overlay = null!;

    public override partial void _Notification(int what);

    private ShellViewModel CreateViewModel() => new();

    private void OnBind(ShellViewModel vm, BindingSet bindings)
    {
        var registry = new ViewRegistry()
            .Register<DirectoryViewModel>("res://Navigation/Directory/DirectoryView.tscn")
            .Register<CounterViewModel>("res://Counter/CounterView.tscn")
            .Register<SearchViewModel>("res://Search/SearchView.tscn")
            .Register<ListViewModel>("res://List/ListView.tscn")
            .Register<TodoListViewModel>("res://Todo/TodoListView.tscn")
            .Register<FormViewModel>("res://Form/FormView.tscn")
            .Register<PauseDemoViewModel>("res://Navigation/Pause/PauseDemoView.tscn")
            .Register<AboutViewModel>("res://Navigation/About/AboutView.tscn")
            .Register<ToastViewModel>("res://Navigation/Toast/ToastView.tscn")
            .Register<BannerViewModel>("res://Navigation/Banner/BannerView.tscn")
            .Register<ConfirmViewModel>("res://Navigation/Confirm/ConfirmView.tscn");
        bindings.BindContent(vm.ActiveItem, _content.Content(registry));
        bindings.BindOverlayHost(vm.Overlay, _overlay, registry);
        if (vm.ActiveItem.Value is null)
        {
            vm.Navigate(new DirectoryViewModel(vm, vm.Overlay, vm.Interaction));
        }
    }
}
