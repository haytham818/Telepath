using Godot;
using Telepath.Core;
using Telepath.Godot;
using Telepath.Showcase.CounterApp;
using Telepath.Showcase.ListApp;
using Telepath.Showcase.SearchApp;
using Telepath.Showcase.TodoApp;

namespace Telepath.Showcase.Shell;

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
            .Register<DirectoryViewModel>("res://Shell/DirectoryView.tscn")
            .Register<CounterViewModel>("res://CounterApp/CounterView.tscn")
            .Register<SearchViewModel>("res://SearchApp/SearchView.tscn")
            .Register<ListViewModel>("res://ListApp/ListView.tscn")
            .Register<TodoListViewModel>("res://TodoApp/TodoListView.tscn")
            .Register<PauseDemoViewModel>("res://Shell/PauseDemoView.tscn")
            .Register<AboutViewModel>("res://Shell/AboutView.tscn")
            .Register<ToastViewModel>("res://Shell/ToastView.tscn")
            .Register<BannerViewModel>("res://Shell/BannerView.tscn");
        bindings.BindContent(vm.ActiveItem, _content.Content(registry));
        bindings.BindOverlayHost(vm.Overlay, _overlay, registry);
        if (vm.ActiveItem.Value is null)
        {
            vm.Navigate(new DirectoryViewModel(vm, vm.Overlay));
        }
    }
}
