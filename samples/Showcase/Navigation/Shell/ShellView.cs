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
            .Register<DirectoryViewModel>("uid://c8telepathdir")
            .Register<CounterViewModel>("uid://82v1sb24scbx")
            .Register<SearchViewModel>("uid://cl3o7xlibbdfu")
            .Register<ListViewModel>("uid://c8k4listappsc")
            .Register<TodoListViewModel>("uid://datodolistsc")
            .Register<FormViewModel>("uid://c8k4formappsc")
            .Register<PauseDemoViewModel>("uid://c8telepathpau")
            .Register<AboutViewModel>("uid://c8telepathabt")
            .Register<ToastViewModel>("uid://c8telepathtst")
            .Register<BannerViewModel>("uid://c8telepathbnr")
            .Register<ConfirmViewModel>("uid://c8telepathcfm");
        var presented = new PresentedViews();
        bindings.BindContent(vm.ActiveItem, _content.Content(registry, presented));
        bindings.BindOverlayHost(vm.Overlay, _overlay, registry, presented);
        if (vm.ActiveItem.Value is null)
        {
            vm.Navigate(new DirectoryViewModel(vm, vm.Overlay, vm.Interaction));
        }
    }
}
