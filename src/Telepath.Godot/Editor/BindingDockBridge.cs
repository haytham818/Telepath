#if TOOLS
#nullable enable
using Godot;
using R3;
using Telepath.Core;
using Telepath.Godot;

namespace Telepath.Godot.Editor;

/// <summary>
/// Editor-only C# helper. Must not <c>+=</c> Godot signals or use
/// <c>Callable.From</c> / R3 <c>FromEvent</c>: Godot snapshots
/// <c>ManagedCallable</c>s before <see cref="ISerializationListener"/>
/// (godotengine/godot#81903, csharp_script.cpp reload path).
/// Dock UI signals live in <c>TelepathBindingDock.gd</c>.
/// </summary>
[Tool]
public partial class BindingDockBridge : GodotObject, ISerializationListener
{
    private EditorPlugin? _plugin;
    private Node? _dock;
    private BindingDockViewModel? _viewModel;
    private BindingSet? _bindings;

    public void Attach(EditorPlugin plugin, Node dock)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(dock);
        _plugin = plugin;
        _dock = dock;
        AlcUnloadHook.Register();
        EnsureViewModel();
        BindUi();
    }

    public void Detach()
    {
        DropBindings();
        _plugin = null;
        _dock = null;
    }

    public void NotifySelectionChanged() => _viewModel?.RefreshSelection();

    public void BeginAdd() => Execute(_viewModel?.BeginAddCommand);

    public void Add() => Execute(_viewModel?.AddCommand);

    public void Update() => Execute(_viewModel?.UpdateCommand);

    public void Remove() => Execute(_viewModel?.RemoveCommand);

    public void SelectScene(long index) => Set(_viewModel?.SelectedSceneIndex, index);

    public void SelectControl(long index) => Set(_viewModel?.SelectedControlIndex, index);

    public void SelectMember(long index) => Set(_viewModel?.SelectedMemberIndex, index);

    public void SelectKind(long index) => Set(_viewModel?.SelectedKindIndex, index);

    public void SelectConverter(long index) => Set(_viewModel?.SelectedConverterIndex, index);

    public void SelectParameter(long index) => Set(_viewModel?.SelectedParameterIndex, index);

    public void SelectItemView(long index) => Set(_viewModel?.SelectedItemViewIndex, index);

    public void SetItemScene(string path)
    {
        if (_viewModel is not null)
        {
            _viewModel.ItemScene.Value = path ?? "";
        }
    }

    public void OnBeforeSerialize() => DropBindings();

    public void OnAfterDeserialize()
    {
        AlcUnloadHook.Register();
        if (_dock is not null && GodotObject.IsInstanceValid(_dock))
        {
            StaleCallableCleanup.DropInvalidTree(_dock);
        }

        if (EditorInterface.Singleton is not null)
        {
            StaleCallableCleanup.DropInvalid(EditorInterface.Singleton.GetSelection());
        }

        if (_plugin is null || _dock is null || !GodotObject.IsInstanceValid(_dock))
        {
            return;
        }

        EnsureViewModel();
        BindUi();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Detach();
        }

        base.Dispose(disposing);
    }

    private void EnsureViewModel()
    {
        if (_viewModel is not null && !_viewModel.IsDisposed)
        {
            return;
        }

        _viewModel = new BindingDockViewModel();
        if (_plugin is not null)
        {
            _viewModel.Connect(_plugin);
        }
    }

    private void DropBindings()
    {
        _bindings?.Dispose();
        _bindings = null;
        _viewModel?.Dispose();
        _viewModel = null;
    }

    private void BindUi()
    {
        if (_viewModel is null || _dock is null || !GodotObject.IsInstanceValid(_dock))
        {
            return;
        }

        _bindings?.Dispose();
        var bindings = new BindingSet();
        var vm = _viewModel;
        var status = Require<Label>("%Status");
        var sceneSection = Require<Control>("%SceneSection");
        var beginAdd = Require<Button>("%BeginAdd");
        var sceneEmpty = Require<Control>("%SceneEmpty");
        var sceneList = Require<ItemList>("%SceneList");
        var attributeSection = Require<Control>("%AttributeSection");
        var attributeList = Require<ItemList>("%AttributeList");
        var editor = Require<Control>("%Editor");
        var controlOption = Require<OptionButton>("%ControlOption");
        var memberOption = Require<OptionButton>("%MemberOption");
        var kindOption = Require<OptionButton>("%KindOption");
        var converterRow = Require<Control>("%ConverterRow");
        var converterOption = Require<OptionButton>("%ConverterOption");
        var parameterRow = Require<Control>("%ParameterRow");
        var parameterOption = Require<OptionButton>("%ParameterOption");
        var itemFields = Require<Control>("%ItemFields");
        var itemScene = Require<LineEdit>("%ItemScene");
        var itemViewOption = Require<OptionButton>("%ItemViewOption");
        var add = Require<Button>("%Add");
        var update = Require<Button>("%Update");
        var remove = Require<Button>("%Remove");

        bindings.Bind(vm.Status, status.Text());
        bindings.Bind(vm.HasTarget, sceneSection.Visible());
        BindCanExecute(bindings, vm.BeginAddCommand, beginAdd);
        bindings.Bind(vm.ShowSceneEmpty, sceneEmpty.Visible());
        bindings.Bind(vm.ShowSceneList, sceneList.Visible());
        bindings.BindItems(vm.SceneItems, sceneList.Items());
        BindSelected(bindings, vm.SelectedSceneIndex, sceneList);
        bindings.Bind(vm.ShowAttributeSection, attributeSection.Visible());
        bindings.BindItems(vm.AttributeItems, attributeList.Items());
        bindings.Bind(vm.EditorVisible, editor.Visible());
        bindings.BindItems(vm.ControlLabels, controlOption.Items());
        BindSelected(bindings, vm.SelectedControlIndex, controlOption);
        bindings.BindItems(vm.MemberLabels, memberOption.Items());
        BindSelected(bindings, vm.SelectedMemberIndex, memberOption);
        bindings.BindItems(vm.KindLabels, kindOption.Items());
        BindSelected(bindings, vm.SelectedKindIndex, kindOption);
        bindings.Bind(vm.ShowConverter, converterRow.Visible());
        bindings.BindItems(vm.ConverterLabels, converterOption.Items());
        BindSelected(bindings, vm.SelectedConverterIndex, converterOption);
        bindings.Bind(vm.ShowParameter, parameterRow.Visible());
        bindings.BindItems(vm.ParameterLabels, parameterOption.Items());
        BindSelected(bindings, vm.SelectedParameterIndex, parameterOption);
        bindings.Bind(vm.ShowItemFields, itemFields.Visible());
        bindings.Bind(vm.ItemScene, BindingTarget<string>.OneWay(value =>
        {
            if (itemScene.Text != value)
            {
                itemScene.Text = value;
            }
        }));
        bindings.BindItems(vm.ItemViewLabels, itemViewOption.Items());
        BindSelected(bindings, vm.SelectedItemViewIndex, itemViewOption);
        bindings.Bind(vm.ShowAddButton, add.Visible());
        BindCanExecute(bindings, vm.AddCommand, add);
        bindings.Bind(vm.ShowApplyButton, update.Visible());
        BindCanExecute(bindings, vm.UpdateCommand, update);
        bindings.Bind(vm.ShowRemove, remove.Visible());
        BindCanExecute(bindings, vm.RemoveCommand, remove);
        _bindings = bindings;
    }

    private T Require<T>(string path) where T : class
    {
        var node = _dock!.GetNode<T>(path);
        return node ?? throw new InvalidOperationException($"Telepath Binding Dock missing '{path}'.");
    }

    private static void Execute(ReactiveCommand? command)
    {
        if (command is null || !command.CanExecute())
        {
            return;
        }

        command.Execute(Unit.Default);
    }

    private static void Set(BindableReactiveProperty<long>? property, long index)
    {
        if (property is not null)
        {
            property.Value = index;
        }
    }

    private static void BindCanExecute(BindingSet bindings, ReactiveCommand command, BaseButton button)
    {
        void Sync() => button.Disabled = !command.CanExecute();
        Sync();
        EventHandler handler = (_, _) => Sync();
        command.CanExecuteChanged += handler;
        bindings.Add(Disposable.Create(() => command.CanExecuteChanged -= handler));
    }

    private static void BindSelected(BindingSet bindings, BindableReactiveProperty<long> source, ItemList list)
    {
        bindings.Add(source.Subscribe(index =>
        {
            if (index < 0 || index >= list.ItemCount)
            {
                list.DeselectAll();
                return;
            }

            var selected = (int)index;
            if (!list.IsSelected(selected))
            {
                list.Select(selected);
            }
        }));
    }

    private static void BindSelected(BindingSet bindings, BindableReactiveProperty<long> source, OptionButton button)
    {
        bindings.Add(source.Subscribe(index =>
        {
            var selected = (int)index;
            if (selected < 0 || selected >= button.ItemCount || button.Selected == selected)
            {
                return;
            }

            button.Select(selected);
        }));
    }
}
#endif
