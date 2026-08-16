#if TOOLS
using Godot;
using Telepath.Godot;

namespace Telepath.Godot.Editor;

[Tool]
public partial class TelepathBindingDock : EditorDock
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

    private readonly EditorPlugin _plugin;
    private EditorSelection? _selection;
    private Node? _view;
    private Type? _viewType;
    private Type? _viewModelType;
    private IReadOnlyList<ControlInfo> _controls = [];
    private IReadOnlyList<ViewModelMember> _members = [];
    private IReadOnlyList<SceneBindingEntry> _sceneBindings = [];

    private Label _status = null!;
    private ItemList _attributeList = null!;
    private ItemList _sceneList = null!;
    private OptionButton _controlOption = null!;
    private OptionButton _memberOption = null!;
    private OptionButton _kindOption = null!;
    private OptionButton _converterOption = null!;
    private OptionButton _parameterOption = null!;
    private LineEdit _itemSceneEdit = null!;
    private OptionButton _itemViewOption = null!;
    private Control _root = null!;

    public TelepathBindingDock(EditorPlugin plugin)
    {
        _plugin = plugin;
        Title = "Telepath";
        DefaultSlot = DockSlot.RightUl;
        CustomMinimumSize = new Vector2(280, 240);
    }

    public override void _Ready()
    {
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(scroll);
        _root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_root);
        BuildUi();
        _selection = EditorInterface.Singleton.GetSelection();
        _selection.SelectionChanged += OnSelectionChanged;
        OnSelectionChanged();
    }

    public override void _ExitTree()
    {
        if (_selection is not null)
        {
            _selection.SelectionChanged -= OnSelectionChanged;
        }
    }

    private void BuildUi()
    {
        AddUi(new Label { Text = "Telepath Bindings" });
        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        AddUi(_status);

        AddUi(new Label { Text = "[BindTo] (read-only)" });
        _attributeList = CreateList();
        AddUi(_attributeList);

        AddUi(new Label { Text = "Scene bindings" });
        _sceneList = CreateList();
        _sceneList.ItemSelected += OnSceneBindingSelected;
        AddUi(_sceneList);

        _controlOption = AddLabeledOption("Control");
        _memberOption = AddLabeledOption("ViewModel member");
        _kindOption = AddLabeledOption("Kind");
        foreach (var kind in Kinds)
        {
            _kindOption.AddItem(kind.ToString());
        }

        _converterOption = AddLabeledOption("Converter");
        _parameterOption = AddLabeledOption("Command parameter");
        AddUi(new Label { Text = "Item scene" });
        _itemSceneEdit = new LineEdit { PlaceholderText = "res://.../Item.tscn" };
        AddUi(_itemSceneEdit);
        _itemViewOption = AddLabeledOption("Item view");

        var buttons = new HBoxContainer();
        AddUi(buttons);
        buttons.AddChild(MakeButton("Add", OnAdd));
        buttons.AddChild(MakeButton("Update", OnUpdate));
        buttons.AddChild(MakeButton("Remove", OnRemove));
    }

    private void AddUi(Control child) => _root.AddChild(child);

    private static ItemList CreateList()
    {
        var list = new ItemList
        {
            CustomMinimumSize = new Vector2(0, 72),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        return list;
    }

    private OptionButton AddLabeledOption(string label)
    {
        AddUi(new Label { Text = label });
        var option = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        AddUi(option);
        return option;
    }

    private static Button MakeButton(string text, Action pressed)
    {
        var button = new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.Pressed += pressed;
        return button;
    }

    private void OnSelectionChanged()
    {
        var selected = _selection?.GetSelectedNodes().OfType<Node>().FirstOrDefault();
        var view = ViewScriptResolver.FindTelepathView(selected);
        if (view is null || !ViewScriptResolver.TryResolve(view, out var viewType, out var viewModelType))
        {
            _view = null;
            _viewType = null;
            _viewModelType = null;
            _status.Text = "Select a Telepath view (or a child of one).";
            _attributeList.Clear();
            _sceneList.Clear();
            return;
        }

        _view = view;
        _viewType = viewType;
        _viewModelType = viewModelType;
        _status.Text = $"{viewType.Name}  →  {viewModelType.Name}";
        Refresh();
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

        FillAttributes();
        FillSceneList();
        FillControls(_controlOption, includeEmpty: false);
        FillControls(_parameterOption, includeEmpty: true);
        FillMembers();
        FillConverters();
        FillItemViews();
        if (_kindOption.ItemCount > 0 && _kindOption.Selected < 0)
        {
            _kindOption.Select(0);
        }
    }

    private void FillAttributes()
    {
        _attributeList.Clear();
        foreach (var entry in AttributeBindingCatalog.Read(_viewType!))
        {
            _attributeList.AddItem(FormatEntry(entry, readOnly: true));
        }
    }

    private void FillSceneList()
    {
        var selected = _sceneList.GetSelectedItems();
        var selectedIndex = selected.Length == 0 ? -1 : selected[0];
        _sceneList.Clear();
        foreach (var entry in _sceneBindings)
        {
            _sceneList.AddItem(FormatEntry(entry, readOnly: false));
        }

        if ((uint)selectedIndex < (uint)_sceneList.ItemCount)
        {
            _sceneList.Select(selectedIndex);
        }
    }

    private void FillControls(OptionButton option, bool includeEmpty)
    {
        var previous = SelectedMetadata(option);
        option.Clear();
        if (includeEmpty)
        {
            option.AddItem("");
        }

        foreach (var control in _controls)
        {
            var label = control.HasUniqueName
                ? $"{control.Path}  ({control.TypeName})"
                : $"{control.Path}  ({control.TypeName}, no unique name)";
            option.AddItem(label);
            option.SetItemMetadata(option.ItemCount - 1, control.Path);
        }

        SelectByMetadata(option, previous, includeEmpty);
    }

    private void FillMembers()
    {
        var previous = SelectedMetadata(_memberOption);
        _memberOption.Clear();
        foreach (var member in _members)
        {
            _memberOption.AddItem(member.Display);
            _memberOption.SetItemMetadata(_memberOption.ItemCount - 1, member.Name);
        }

        SelectByMetadata(_memberOption, previous, includeEmpty: false);
    }

    private void FillConverters()
    {
        var previous = SelectedMetadata(_converterOption);
        _converterOption.Clear();
        _converterOption.AddItem("(none)");
        _converterOption.SetItemMetadata(0, "");
        foreach (var type in ConverterCatalog.Scan())
        {
            var name = type.FullName ?? type.Name;
            _converterOption.AddItem(type.Name);
            _converterOption.SetItemMetadata(_converterOption.ItemCount - 1, name);
        }

        SelectByMetadata(_converterOption, previous, includeEmpty: true);
    }

    private void FillItemViews()
    {
        var previous = SelectedMetadata(_itemViewOption);
        _itemViewOption.Clear();
        _itemViewOption.AddItem("(none)");
        _itemViewOption.SetItemMetadata(0, "");
        foreach (var type in ConverterCatalog.ScanItemViews())
        {
            var name = type.FullName ?? type.Name;
            _itemViewOption.AddItem(type.Name);
            _itemViewOption.SetItemMetadata(_itemViewOption.ItemCount - 1, name);
        }

        SelectByMetadata(_itemViewOption, previous, includeEmpty: true);
    }

    private void OnSceneBindingSelected(long index)
    {
        if ((uint)index >= (uint)_sceneBindings.Count)
        {
            return;
        }

        var entry = _sceneBindings[(int)index];
        SelectByMetadata(_controlOption, entry.Path, includeEmpty: false);
        SelectByMetadata(_memberOption, entry.Member, includeEmpty: false);
        var kindIndex = Array.IndexOf(Kinds, entry.Kind);
        if (kindIndex >= 0)
        {
            _kindOption.Select(kindIndex);
        }

        SelectByMetadata(_converterOption, entry.Converter ?? "", includeEmpty: true);
        SelectByMetadata(_parameterOption, entry.Parameter ?? "", includeEmpty: true);
        _itemSceneEdit.Text = entry.ItemScene ?? "";
        SelectByMetadata(_itemViewOption, entry.ItemView ?? "", includeEmpty: true);
    }

    private void OnAdd()
    {
        if (!TryReadForm(out var entry))
        {
            return;
        }

        var next = _sceneBindings.ToList();
        next.Add(entry);
        Commit(next);
    }

    private void OnUpdate()
    {
        var selected = _sceneList.GetSelectedItems();
        if (selected.Length == 0 || !TryReadForm(out var entry))
        {
            return;
        }

        var next = _sceneBindings.ToList();
        next[selected[0]] = entry;
        Commit(next);
    }

    private void OnRemove()
    {
        var selected = _sceneList.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        var next = _sceneBindings.ToList();
        next.RemoveAt(selected[0]);
        Commit(next);
    }

    private bool TryReadForm(out SceneBindingEntry entry)
    {
        entry = null!;
        var path = SelectedMetadata(_controlOption);
        var member = SelectedMetadata(_memberOption);
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(member) || _view is null)
        {
            _status.Text = "Pick a control and a ViewModel member.";
            return false;
        }

        var kind = _kindOption.Selected >= 0 ? Kinds[_kindOption.Selected] : LinkKind.Auto;
        entry = new SceneBindingEntry
        {
            Path = path,
            Member = member,
            Kind = kind,
            Converter = EmptyToNull(SelectedMetadata(_converterOption)),
            Parameter = EmptyToNull(SelectedMetadata(_parameterOption)),
            ItemView = EmptyToNull(SelectedMetadata(_itemViewOption)),
            ItemScene = EmptyToNull(_itemSceneEdit.Text.Trim()),
        };
        return true;
    }

    private void Commit(IReadOnlyList<SceneBindingEntry> entries)
    {
        if (_view is null)
        {
            return;
        }

        var undo = _plugin.GetUndoRedo();
        undo.CreateAction("Update Telepath bindings");
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
        FillSceneList();
    }

    private static string FormatEntry(SceneBindingEntry entry, bool readOnly)
    {
        var prefix = readOnly ? "[attr] " : "";
        var extra = entry.Kind == LinkKind.Auto ? "" : $" · {entry.Kind}";
        return $"{prefix}{entry.Path} → {entry.Member}{extra}";
    }

    private static string SelectedMetadata(OptionButton option)
    {
        if (option.ItemCount == 0 || option.Selected < 0)
        {
            return "";
        }

        var metadata = option.GetItemMetadata(option.Selected);
        if (metadata.VariantType is Variant.Type.Nil)
        {
            return "";
        }

        return metadata.AsString();
    }

    private static void SelectByMetadata(OptionButton option, string value, bool includeEmpty)
    {
        for (var i = 0; i < option.ItemCount; i++)
        {
            var metadata = option.GetItemMetadata(i);
            var text = metadata.VariantType is Variant.Type.Nil ? "" : metadata.AsString();
            if (text == value)
            {
                option.Select(i);
                return;
            }
        }

        if (option.ItemCount > 0)
        {
            option.Select(includeEmpty || string.IsNullOrEmpty(value) ? 0 : 0);
        }
    }

    private static string? EmptyToNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
#endif
