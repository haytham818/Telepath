#if TOOLS
using Godot;
using ObservableCollections;
using R3;
using Telepath.Core;
using Telepath.Godot;

namespace Telepath.Godot.Editor;

public sealed partial class BindingDockViewModel : ViewModel
{
    private static readonly LinkKind[] Kinds =
    [
        LinkKind.Auto,
        LinkKind.Text,
        LinkKind.Command,
        LinkKind.Toggle,
        LinkKind.Value,
        LinkKind.Selected,
        LinkKind.Visible,
        LinkKind.Disabled,
        LinkKind.Items,
    ];

    private EditorPlugin? _plugin;
    private EditorSelection? _selection;
    private EditorFileDialog? _itemSceneDialog;
    private Node? _view;
    private Type? _viewType;
    private Type? _viewModelType;
    private IReadOnlyList<ControlInfo> _controls = [];
    private IReadOnlyList<ViewModelMember> _members = [];
    private IReadOnlyList<SceneBindingEntry> _sceneBindings = [];
    private readonly List<string> _controlPaths = [];
    private readonly List<string> _memberNames = [];
    private readonly List<string> _converterValues = [];
    private readonly List<string> _parameterPaths = [];
    private readonly List<string> _itemViewValues = [];
    private bool _suppressSelection;

    [Bindable]
    private bool _hasTarget = false;

    [Bindable]
    private bool _isAdding = false;

    [Bindable]
    private int _attributeCount = 0;

    [Bindable]
    private int _sceneCount = 0;

    [Bindable]
    private string _status = "选中一个 Telepath View（或其子节点）。";

    [Bindable]
    private string _itemScene = "";

    [Bindable]
    private long _selectedSceneIndex = -1;

    [Bindable]
    private long _selectedControlIndex = -1;

    [Bindable]
    private long _selectedMemberIndex = -1;

    [Bindable]
    private long _selectedKindIndex = 0;

    [Bindable]
    private long _selectedConverterIndex = 0;

    [Bindable]
    private long _selectedParameterIndex = 0;

    [Bindable]
    private long _selectedItemViewIndex = 0;

    [Bindable]
    private ObservableList<string>? _attributeItems;

    [Bindable]
    private ObservableList<string>? _sceneItems;

    [Bindable]
    private ObservableList<string>? _controlLabels;

    [Bindable]
    private ObservableList<string>? _memberLabels;

    [Bindable]
    private ObservableList<string>? _kindLabels;

    [Bindable]
    private ObservableList<string>? _converterLabels;

    [Bindable]
    private ObservableList<string>? _parameterLabels;

    [Bindable]
    private ObservableList<string>? _itemViewLabels;

    public BindingDockViewModel()
    {
        foreach (var kind in Kinds)
        {
            KindLabels.Add(kind.ToString());
        }

        Track(SelectedSceneIndex.Subscribe(OnSelectedSceneIndexChanged));
        Track(SelectedControlIndex.Subscribe(_ => RefreshKindLabels()));
    }

    public void Connect(EditorPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (_plugin is not null)
        {
            return;
        }

        _plugin = plugin;
        _selection = EditorInterface.Singleton.GetSelection();
        _selection.SelectionChanged += OnSelectionChanged;
        OnSelectionChanged();
    }

    protected override void OnDispose()
    {
        CloseItemSceneDialog();
        if (_selection is not null)
        {
            _selection.SelectionChanged -= OnSelectionChanged;
            _selection = null;
        }

        _plugin = null;
        _view = null;
        _viewType = null;
        _viewModelType = null;
    }

    [Bindable(nameof(AttributeCount))]
    private bool GetShowAttributeSection(int attributeCount) => attributeCount > 0;

    [Bindable(nameof(HasTarget), nameof(SceneCount))]
    private bool GetShowSceneEmpty(bool hasTarget, int sceneCount) => hasTarget && sceneCount == 0;

    [Bindable(nameof(SceneCount))]
    private bool GetShowSceneList(int sceneCount) => sceneCount > 0;

    [Bindable(nameof(HasTarget), nameof(IsAdding), nameof(SelectedSceneIndex))]
    private bool GetEditorVisible(bool hasTarget, bool isAdding, long selectedSceneIndex)
        => hasTarget && (isAdding || selectedSceneIndex >= 0);

    [Bindable(nameof(HasTarget), nameof(IsAdding))]
    private bool GetShowAddButton(bool hasTarget, bool isAdding) => hasTarget && isAdding;

    [Bindable(nameof(HasTarget), nameof(IsAdding), nameof(SelectedSceneIndex))]
    private bool GetShowApplyButton(bool hasTarget, bool isAdding, long selectedSceneIndex)
        => hasTarget && !isAdding && selectedSceneIndex >= 0;

    [Bindable(nameof(HasTarget), nameof(SelectedSceneIndex))]
    private bool GetShowRemove(bool hasTarget, long selectedSceneIndex)
        => hasTarget && selectedSceneIndex >= 0;

    [Bindable(nameof(SelectedKindIndex), nameof(SelectedControlIndex))]
    private bool GetShowConverter(long kindIndex, long controlIndex)
    {
        _ = kindIndex;
        _ = controlIndex;
        var kind = ResolveFormKind();
        return kind is LinkKind.Text or LinkKind.Value or LinkKind.Selected or LinkKind.Toggle;
    }

    [Bindable(nameof(SelectedKindIndex), nameof(SelectedControlIndex))]
    private bool GetShowParameter(long kindIndex, long controlIndex)
    {
        _ = kindIndex;
        _ = controlIndex;
        return ResolveFormKind() == LinkKind.Command;
    }

    [Bindable(nameof(SelectedKindIndex), nameof(SelectedControlIndex))]
    private bool GetShowItemFields(long kindIndex, long controlIndex)
    {
        _ = kindIndex;
        _ = controlIndex;
        return ResolveFormKind() == LinkKind.Items;
    }

    [Command(CanExecute = nameof(CanBeginAdd))]
    private void OnBeginAdd()
    {
        IsAdding.Value = true;
        WithSuppressedSelection(() => SelectedSceneIndex.Value = -1);
        PrefillControlFromEditorSelection();
        RefreshKindLabels();
    }

    private Observable<bool> CanBeginAdd() => HasTarget;

    [Command(CanExecute = nameof(CanAdd))]
    private void OnAdd()
    {
        if (!TryReadForm(out var entry))
        {
            return;
        }

        var next = _sceneBindings.ToList();
        next.Add(entry);
        IsAdding.Value = false;
        Commit(next, next.Count - 1);
    }

    private Observable<bool> CanAdd() => Observable.CombineLatest(
        HasTarget,
        SelectedControlIndex,
        SelectedMemberIndex,
        IsAdding,
        static (hasTarget, control, member, isAdding) =>
            hasTarget && isAdding && control >= 0 && member >= 0);

    [Command(CanExecute = nameof(CanUpdate))]
    private void OnUpdate()
    {
        if ((uint)SelectedSceneIndex.Value >= (uint)_sceneBindings.Count || !TryReadForm(out var entry))
        {
            return;
        }

        var next = _sceneBindings.ToList();
        next[(int)SelectedSceneIndex.Value] = entry;
        Commit(next, SelectedSceneIndex.Value);
    }

    private Observable<bool> CanUpdate() => Observable.CombineLatest(
        HasTarget,
        SelectedControlIndex,
        SelectedMemberIndex,
        SelectedSceneIndex,
        IsAdding,
        static (hasTarget, control, member, scene, isAdding) =>
            hasTarget && !isAdding && control >= 0 && member >= 0 && scene >= 0);

    [Command(CanExecute = nameof(CanRemove))]
    private void OnRemove()
    {
        if ((uint)SelectedSceneIndex.Value >= (uint)_sceneBindings.Count)
        {
            return;
        }

        var index = (int)SelectedSceneIndex.Value;
        var next = _sceneBindings.ToList();
        next.RemoveAt(index);
        var select = next.Count == 0 ? -1 : Math.Min(index, next.Count - 1);
        Commit(next, select);
    }

    private Observable<bool> CanRemove() => Observable.CombineLatest(
        HasTarget,
        SelectedSceneIndex,
        static (hasTarget, scene) => hasTarget && scene >= 0);

    [Command]
    private void OnBrowseItemScene()
    {
        CloseItemSceneDialog();
        _itemSceneDialog = new EditorFileDialog
        {
            FileMode = EditorFileDialog.FileModeEnum.OpenFile,
            Access = EditorFileDialog.AccessEnum.Resources,
            Title = "选择项场景",
        };
        _itemSceneDialog.Filters = ["*.tscn ; Scene"];
        _itemSceneDialog.FileSelected += OnItemSceneSelected;
        _itemSceneDialog.Canceled += OnItemSceneCanceled;
        EditorInterface.Singleton.PopupDialogCentered(_itemSceneDialog, new Vector2I(720, 480));
    }

    private void OnItemSceneSelected(string path)
    {
        ItemScene.Value = path;
        CloseItemSceneDialog();
    }

    private void OnItemSceneCanceled() => CloseItemSceneDialog();

    private void CloseItemSceneDialog()
    {
        if (_itemSceneDialog is null)
        {
            return;
        }

        var dialog = _itemSceneDialog;
        _itemSceneDialog = null;
        dialog.FileSelected -= OnItemSceneSelected;
        dialog.Canceled -= OnItemSceneCanceled;
        if (GodotObject.IsInstanceValid(dialog))
        {
            dialog.Free();
        }
    }

    private void OnSelectionChanged()
    {
        var selected = _selection?.GetSelectedNodes().OfType<Node>().FirstOrDefault();
        var view = ViewScriptResolver.FindTelepathView(selected);
        if (view is null || !ViewScriptResolver.TryResolve(view, out var viewType, out var viewModelType))
        {
            ClearTarget();
            return;
        }

        var sameView = ReferenceEquals(view, _view);
        _view = view;
        _viewType = viewType;
        _viewModelType = viewModelType;
        HasTarget.Value = true;
        Status.Value = $"{viewType.Name}  →  {viewModelType.Name}";
        if (!sameView)
        {
            IsAdding.Value = false;
            WithSuppressedSelection(() => SelectedSceneIndex.Value = -1);
        }

        Refresh();
    }

    private void ClearTarget()
    {
        _view = null;
        _viewType = null;
        _viewModelType = null;
        _controls = [];
        _members = [];
        _sceneBindings = [];
        HasTarget.Value = false;
        IsAdding.Value = false;
        Status.Value = "选中一个 Telepath View（或其子节点）。";
        WithSuppressedSelection(() =>
        {
            AttributeItems.Clear();
            AttributeCount.Value = 0;
            SceneItems.Clear();
            SceneCount.Value = 0;
            SelectedSceneIndex.Value = -1;
            ControlLabels.Clear();
            _controlPaths.Clear();
            SelectedControlIndex.Value = -1;
            MemberLabels.Clear();
            _memberNames.Clear();
            SelectedMemberIndex.Value = -1;
            ConverterLabels.Clear();
            _converterValues.Clear();
            SelectedConverterIndex.Value = 0;
            ParameterLabels.Clear();
            _parameterPaths.Clear();
            SelectedParameterIndex.Value = 0;
            ItemViewLabels.Clear();
            _itemViewValues.Clear();
            SelectedItemViewIndex.Value = 0;
            ItemScene.Value = "";
        });
        RefreshKindLabels();
    }

    private void Refresh()
    {
        if (_view is null || _viewType is null || _viewModelType is null)
        {
            return;
        }

        _controls = ControlCatalog.Collect(_view);
        _members = ViewModelMemberScanner.Scan(_viewModelType);
        _sceneBindings = SceneBindingSchema.Read(_view);

        WithSuppressedSelection(() =>
        {
            ReplaceItems(AttributeItems, AttributeBindingCatalog.Read(_viewType).Select(static entry => FormatEntry(entry, readOnly: true)));
            AttributeCount.Value = AttributeItems.Count;
            var selectedScene = SelectedSceneIndex.Value;
            ReplaceItems(SceneItems, _sceneBindings.Select(static entry => FormatEntry(entry, readOnly: false)));
            SceneCount.Value = SceneItems.Count;
            SelectedSceneIndex.Value = (uint)selectedScene < (uint)SceneItems.Count ? selectedScene : -1;

            ReplaceOption(
                ControlLabels,
                _controlPaths,
                _controls.Select(FormatControl).ToList(),
                _controls.Select(static control => control.Path).ToList(),
                SelectedControlIndex);

            ReplaceOption(
                MemberLabels,
                _memberNames,
                _members.Select(static member => member.Display).ToList(),
                _members.Select(static member => member.Name).ToList(),
                SelectedMemberIndex);

            var converters = ConverterCatalog.Scan();
            ReplaceOption(
                ConverterLabels,
                _converterValues,
                converters.Select(static type => type.Name).Prepend("（无）").ToList(),
                converters.Select(static type => type.FullName ?? type.Name).Prepend("").ToList(),
                SelectedConverterIndex);

            ReplaceOption(
                ParameterLabels,
                _parameterPaths,
                _controls.Select(FormatControl).Prepend("").ToList(),
                _controls.Select(static control => control.Path).Prepend("").ToList(),
                SelectedParameterIndex);

            var itemViews = ConverterCatalog.ScanItemViews();
            ReplaceOption(
                ItemViewLabels,
                _itemViewValues,
                itemViews.Select(static type => type.Name).Prepend("（无）").ToList(),
                itemViews.Select(static type => type.FullName ?? type.Name).Prepend("").ToList(),
                SelectedItemViewIndex);

            if (SelectedKindIndex.Value < 0 && KindLabels.Count > 0)
            {
                SelectedKindIndex.Value = 0;
            }
        });

        RefreshKindLabels();
    }

    private void OnSelectedSceneIndexChanged(long index)
    {
        if (_suppressSelection)
        {
            return;
        }

        if ((uint)index >= (uint)_sceneBindings.Count)
        {
            return;
        }

        IsAdding.Value = false;
        ApplySceneBindingToForm(index);
    }

    private void ApplySceneBindingToForm(long index)
    {
        if ((uint)index >= (uint)_sceneBindings.Count)
        {
            return;
        }

        var entry = _sceneBindings[(int)index];
        SelectedControlIndex.Value = IndexOf(_controlPaths, entry.Path);
        SelectedMemberIndex.Value = IndexOf(_memberNames, entry.Member);
        var kindIndex = Array.IndexOf(Kinds, entry.Kind);
        if (kindIndex >= 0)
        {
            SelectedKindIndex.Value = kindIndex;
        }

        SelectedConverterIndex.Value = IndexOf(_converterValues, entry.Converter ?? "");
        SelectedParameterIndex.Value = IndexOf(_parameterPaths, entry.Parameter ?? "");
        ItemScene.Value = entry.ItemScene ?? "";
        SelectedItemViewIndex.Value = IndexOf(_itemViewValues, entry.ItemView ?? "");
        RefreshKindLabels();
    }

    private bool TryReadForm(out SceneBindingEntry entry)
    {
        entry = null!;
        var path = ValueAt(_controlPaths, SelectedControlIndex.Value);
        var member = ValueAt(_memberNames, SelectedMemberIndex.Value);
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(member) || _view is null)
        {
            Status.Value = "请选择控件和 ViewModel 成员。";
            return false;
        }

        var kind = (uint)SelectedKindIndex.Value < (uint)Kinds.Length
            ? Kinds[(int)SelectedKindIndex.Value]
            : LinkKind.Auto;
        entry = new SceneBindingEntry
        {
            Path = path,
            Member = member,
            Kind = kind,
            Converter = EmptyToNull(ValueAt(_converterValues, SelectedConverterIndex.Value)),
            Parameter = EmptyToNull(ValueAt(_parameterPaths, SelectedParameterIndex.Value)),
            ItemView = EmptyToNull(ValueAt(_itemViewValues, SelectedItemViewIndex.Value)),
            ItemScene = EmptyToNull(ItemScene.Value.Trim()),
        };
        return true;
    }

    private void Commit(IReadOnlyList<SceneBindingEntry> entries, long selectIndex)
    {
        if (_view is null || _plugin is null)
        {
            return;
        }

        var undo = _plugin.GetUndoRedo();
        undo.CreateAction("更新 Telepath 绑定");
        if (entries.Count == 0)
        {
            undo.AddDoMethod(_view, Node.MethodName.RemoveMeta, SceneBindingSchema.MetaKey);
        }
        else
        {
            undo.AddDoMethod(_view, Node.MethodName.SetMeta, SceneBindingSchema.MetaKey, SceneBindingSchema.Encode(entries));
        }

        if (_view.HasMeta(SceneBindingSchema.MetaKey))
        {
            undo.AddUndoMethod(_view, Node.MethodName.SetMeta, SceneBindingSchema.MetaKey, _view.GetMeta(SceneBindingSchema.MetaKey));
        }
        else
        {
            undo.AddUndoMethod(_view, Node.MethodName.RemoveMeta, SceneBindingSchema.MetaKey);
        }

        undo.CommitAction();
        _sceneBindings = SceneBindingSchema.Read(_view);
        WithSuppressedSelection(() =>
        {
            ReplaceItems(SceneItems, _sceneBindings.Select(static entry => FormatEntry(entry, readOnly: false)));
            SceneCount.Value = SceneItems.Count;
            SelectedSceneIndex.Value = (uint)selectIndex < (uint)SceneItems.Count ? selectIndex : -1;
        });
        ApplySceneBindingToForm(SelectedSceneIndex.Value);
    }

    private void PrefillControlFromEditorSelection()
    {
        if (_view is null)
        {
            return;
        }

        var selected = _selection?.GetSelectedNodes().OfType<Node>().FirstOrDefault();
        if (selected is null || ReferenceEquals(selected, _view))
        {
            return;
        }

        var path = selected.UniqueNameInOwner
            ? "%" + selected.Name
            : _view.GetPathTo(selected).ToString();
        var index = IndexOf(_controlPaths, path);
        if (index >= 0)
        {
            SelectedControlIndex.Value = index;
        }
    }

    private void RefreshKindLabels()
    {
        LinkKind? inferred = null;
        if (TryGetSelectedControlNode(out var node) && LinkKindInference.TryInfer(node, out var kind))
        {
            inferred = kind;
        }

        for (var i = 0; i < Kinds.Length; i++)
        {
            var text = Kinds[i] == LinkKind.Auto && inferred is { } value
                ? $"Auto → {value}"
                : Kinds[i].ToString();
            if (i < KindLabels.Count)
            {
                if (KindLabels[i] != text)
                {
                    KindLabels[i] = text;
                }
            }
            else
            {
                KindLabels.Add(text);
            }
        }
    }

    private LinkKind ResolveFormKind()
    {
        var kind = (uint)SelectedKindIndex.Value < (uint)Kinds.Length
            ? Kinds[(int)SelectedKindIndex.Value]
            : LinkKind.Auto;
        if (kind != LinkKind.Auto)
        {
            return kind;
        }

        return TryGetSelectedControlNode(out var node) && LinkKindInference.TryInfer(node, out var inferred)
            ? inferred
            : LinkKind.Auto;
    }

    private bool TryGetSelectedControlNode(out Node node)
    {
        node = null!;
        if (_view is null)
        {
            return false;
        }

        var path = ValueAt(_controlPaths, SelectedControlIndex.Value);
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        node = _view.GetNodeOrNull(path)!;
        return node is not null;
    }

    private void WithSuppressedSelection(Action action)
    {
        _suppressSelection = true;
        try
        {
            action();
        }
        finally
        {
            _suppressSelection = false;
        }
    }

    private static void ReplaceItems(ObservableList<string> list, IEnumerable<string> items)
    {
        list.Clear();
        foreach (var item in items)
        {
            list.Add(item);
        }
    }

    private static void ReplaceOption(
        ObservableList<string> labels,
        List<string> values,
        IReadOnlyList<string> nextLabels,
        IReadOnlyList<string> nextValues,
        BindableReactiveProperty<long> selected)
    {
        var previous = ValueAt(values, selected.Value);
        ReplaceItems(labels, nextLabels);
        values.Clear();
        values.AddRange(nextValues);
        selected.Value = IndexOf(values, previous);
    }

    private static string FormatControl(ControlInfo control)
        => control.HasUniqueName
            ? $"{control.Path}  ({control.TypeName})"
            : $"{control.Path}  ({control.TypeName}，无唯一名)";

    private static string FormatEntry(SceneBindingEntry entry, bool readOnly)
    {
        var prefix = readOnly ? "[attr] " : "";
        var extra = entry.Kind == LinkKind.Auto ? "" : $" · {entry.Kind}";
        return $"{prefix}{entry.Path} → {entry.Member}{extra}";
    }

    private static string ValueAt(IReadOnlyList<string> values, long index)
        => (uint)index < (uint)values.Count ? values[(int)index] : "";

    private static long IndexOf(IReadOnlyList<string> values, string value)
    {
        if (values.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
            {
                return i;
            }
        }

        return 0;
    }

    private static string? EmptyToNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
#endif
