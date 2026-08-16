using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Telepath.SourceGenerator.Tests;

public sealed class TelepathViewGeneratorTests
{
    [Fact]
    public void GeneratesLifecycleBridgeWithoutGodotInternalGlue()
    {
        const string source = """
            using Telepath.Core;
            using Telepath.Godot;

            namespace Demo;

            public sealed class CounterViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<CounterViewModel>]
            public partial class CounterView : Godot.Control
            {
                public override partial void _Notification(int what);

                private void OnReady() { }
                private CounterViewModel CreateViewModel() => new();
                private void OnBind(CounterViewModel vm, BindingSet bindings) { }
                public void Inject(CounterViewModel vm) => ViewModel = vm;
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("ViewLifecycle<global::Demo.CounterViewModel>", generated);
        Assert.Contains("public global::Demo.CounterViewModel? ViewModel", generated);
        Assert.Contains("public override partial void _Notification(int what)", generated);
        Assert.DoesNotContain("MethodName", generated);
        Assert.DoesNotContain("InvokeGodotClassMethod", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsMissingNotificationDeclaration()
    {
        const string source = """
            using Telepath.Core;
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                private TestViewModel CreateViewModel() => new();
                private void OnBind(TestViewModel vm, BindingSet bindings) { }
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV002", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsInvalidBindingCallback()
    {
        const string source = """
            using Telepath.Core;
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
                private void OnBind(object vm, BindingSet bindings) { }
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV004", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsMissingOnBindWhenNoBindTo()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV004", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void GeneratesNodeInjectAndBindToWithoutOnBind()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class CounterViewModel : Telepath.Core.IViewModel
            {
                public object CountText { get; } = new();
                public object Increment { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<CounterViewModel>]
            public partial class CounterView : Godot.Control
            {
                [NodeInject("%CountLabel")]
                [BindTo(nameof(CounterViewModel.CountText))]
                private Godot.Label _countLabel = null!;

                [NodeInject("%IncrementButton")]
                [BindTo(nameof(CounterViewModel.Increment))]
                private Godot.Button _incrementButton = null!;

                public override partial void _Notification(int what);

                private CounterViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("__TelepathOnReady", generated);
        Assert.Contains("__TelepathOnBind", generated);
        Assert.Contains("GetNode<global::Godot.Label>(\"%CountLabel\")", generated);
        Assert.Contains("GetNode<global::Godot.Button>(\"%IncrementButton\")", generated);
        Assert.Contains(
            "bindings.Bind(vm.@CountText, @_countLabel.Text(), global::Telepath.Core.ToStringConverter.Convert)",
            generated);
        Assert.Contains("bindings.BindCommand(vm.@Increment, @_incrementButton)", generated);
        Assert.Contains("using Telepath.Core;", generated);
        Assert.Contains("using Telepath.Godot;", generated);
        Assert.Contains("global::Telepath.Core.BindingSet bindings", generated);
        Assert.DoesNotContain("OnBind(vm, bindings)", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsUnsupportedBindToControl()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Root")]
                [BindTo("Value")]
                private Godot.Control _root = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV008", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void GeneratesInferredBindingsForSupportedControls()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class FormViewModel : Telepath.Core.IViewModel
            {
                public object Title { get; } = new();
                public object Body { get; } = new();
                public object Name { get; } = new();
                public object Notes { get; } = new();
                public object Enabled { get; } = new();
                public object Featured { get; } = new();
                public object Choice { get; } = new();
                public object Volume { get; } = new();
                public object Progress { get; } = new();
                public object Save { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<FormViewModel>]
            public partial class FormView : Godot.Control
            {
                [NodeInject("%Title")]
                [BindTo(nameof(FormViewModel.Title))]
                private Godot.Label _title = null!;

                [NodeInject("%Body")]
                [BindTo(nameof(FormViewModel.Body))]
                private Godot.RichTextLabel _body = null!;

                [NodeInject("%Name")]
                [BindTo(nameof(FormViewModel.Name))]
                private Godot.LineEdit _name = null!;

                [NodeInject("%Notes")]
                [BindTo(nameof(FormViewModel.Notes))]
                private Godot.TextEdit _notes = null!;

                [NodeInject("%Enabled")]
                [BindTo(nameof(FormViewModel.Enabled))]
                private Godot.CheckBox _enabled = null!;

                [NodeInject("%Featured")]
                [BindTo(nameof(FormViewModel.Featured))]
                private Godot.CheckButton _featured = null!;

                [NodeInject("%Choice")]
                [BindTo(nameof(FormViewModel.Choice))]
                private Godot.OptionButton _choice = null!;

                [NodeInject("%Volume")]
                [BindTo(nameof(FormViewModel.Volume))]
                private Godot.Slider _volume = null!;

                [NodeInject("%Progress")]
                [BindTo(nameof(FormViewModel.Progress))]
                private Godot.ProgressBar _progress = null!;

                [NodeInject("%Save")]
                [BindTo(nameof(FormViewModel.Save))]
                private Godot.Button _save = null!;

                public override partial void _Notification(int what);

                private FormViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains(
            "bindings.Bind(vm.@Title, @_title.Text(), global::Telepath.Core.ToStringConverter.Convert)",
            generated);
        Assert.Contains(
            "bindings.Bind(vm.@Body, @_body.Text(), global::Telepath.Core.ToStringConverter.Convert)",
            generated);
        Assert.Contains("bindings.Bind(vm.@Name, @_name.Text())", generated);
        Assert.Contains("bindings.Bind(vm.@Notes, @_notes.Text())", generated);
        Assert.Contains("bindings.Bind(vm.@Enabled, @_enabled.Toggle())", generated);
        Assert.Contains("bindings.Bind(vm.@Featured, @_featured.Toggle())", generated);
        Assert.Contains("bindings.Bind(vm.@Choice, @_choice.Selected())", generated);
        Assert.Contains("bindings.Bind(vm.@Volume, @_volume.Value())", generated);
        Assert.Contains("bindings.Bind(vm.@Progress, @_progress.Value())", generated);
        Assert.Contains("bindings.BindCommand(vm.@Save, @_save)", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesKindOverrides()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class PanelViewModel : Telepath.Core.IViewModel
            {
                public object ShowPanel { get; } = new();
                public object Locked { get; } = new();
                public object DoThing { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<PanelViewModel>]
            public partial class PanelView : Godot.Control
            {
                [NodeInject("%Panel")]
                [BindTo(nameof(PanelViewModel.ShowPanel), Kind = LinkKind.Visible)]
                private Godot.Control _panel = null!;

                [NodeInject("%Lock")]
                [BindTo(nameof(PanelViewModel.Locked), Kind = LinkKind.Disabled)]
                private Godot.Button _lock = null!;

                [NodeInject("%Weird")]
                [BindTo(nameof(PanelViewModel.DoThing), Kind = LinkKind.Command)]
                private Godot.CheckBox _weird = null!;

                public override partial void _Notification(int what);

                private PanelViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("bindings.Bind(vm.@ShowPanel, @_panel.Visible())", generated);
        Assert.Contains("bindings.Bind(vm.@Locked, @_lock.Disabled())", generated);
        Assert.Contains("bindings.BindCommand(vm.@DoThing, @_weird)", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesSelectedBindingForItemList()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Items")]
                [BindTo("Selected", Kind = LinkKind.Selected)]
                private Godot.ItemList _items = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Selected { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("bindings.Bind(vm.@Selected, @_items.Selected())", generated);
        Assert.Contains("ITelepathView<", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesBindItemsForItemListAuto()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Items")]
                [BindTo("Items")]
                private Godot.ItemList _items = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Items { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("bindings.BindItems(vm.@Items, @_items.Items());", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesBindItemsForOptionButton()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Choices")]
                [BindTo("Items", Kind = LinkKind.Items)]
                private Godot.OptionButton _choices = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Items { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("bindings.BindItems(vm.@Items, @_choices.Items());", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesBindItemsConverterForItemList()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class TitleConverter : Telepath.Core.IValueConverter<object, string>
            {
                public string Convert(object value) => value?.ToString() ?? string.Empty;
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Items { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Items")]
                [BindTo(nameof(TestViewModel.Items), Converter = typeof(TitleConverter))]
                private Godot.ItemList _items = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains(
            "bindings.BindItems(vm.@Items, @_items.Items(), new global::Demo.TitleConverter());",
            generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesContainerBindItemsWithItemTemplate()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class TodoItemViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            public sealed class TodoListViewModel : Telepath.Core.IViewModel
            {
                public object Items { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<TodoItemViewModel>]
            public partial class TodoItemView : Godot.Control
            {
                public override partial void _Notification(int what);

                private TodoItemViewModel CreateViewModel() => new();

                private void OnBind(TodoItemViewModel vm, Telepath.Core.BindingSet bindings) { }
            }

            [TelepathView<TodoListViewModel>]
            public partial class TodoListView : Godot.Control
            {
                public Godot.PackedScene ItemScene { get; set; } = null!;

                [NodeInject("%Items")]
                [BindTo(nameof(TodoListViewModel.Items), Kind = LinkKind.Items,
                    ItemView = typeof(TodoItemView), ItemScene = nameof(ItemScene))]
                private Godot.VBoxContainer _items = null!;

                public override partial void _Notification(int what);

                private TodoListViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.GeneratedSources.Count);
        var generated = CombinedGenerated(result);
        Assert.Contains(
            "bindings.BindItems(vm.@Items, @_items.Items<global::Demo.TodoItemView, global::Demo.TodoItemViewModel>(@ItemScene));",
            generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsMissingContainerItemTemplate()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Items")]
                [BindTo("Items", Kind = LinkKind.Items)]
                private Godot.VBoxContainer _items = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Items { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV010", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsItemViewOnItemList()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class ItemViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Items { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<ItemViewModel>]
            public partial class ItemView : Godot.Control
            {
                public override partial void _Notification(int what);

                private ItemViewModel CreateViewModel() => new();

                private void OnBind(ItemViewModel vm, Telepath.Core.BindingSet bindings) { }
            }

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                public Godot.PackedScene ItemScene { get; set; } = null!;

                [NodeInject("%Items")]
                [BindTo(nameof(TestViewModel.Items), ItemView = typeof(ItemView), ItemScene = nameof(ItemScene))]
                private Godot.ItemList _items = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Id == "TPV010");
        Assert.Equal("TPV010", diagnostic.Id);
    }

    [Fact]
    public void ReportsConverterOnContainerItemBinding()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class TitleConverter : Telepath.Core.IValueConverter<object, string>
            {
                public string Convert(object value) => value?.ToString() ?? string.Empty;
            }

            public sealed class ItemViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Items { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<ItemViewModel>]
            public partial class ItemView : Godot.Control
            {
                public override partial void _Notification(int what);

                private ItemViewModel CreateViewModel() => new();

                private void OnBind(ItemViewModel vm, Telepath.Core.BindingSet bindings) { }
            }

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                public Godot.PackedScene ItemScene { get; set; } = null!;

                [NodeInject("%Items")]
                [BindTo(nameof(TestViewModel.Items), Kind = LinkKind.Items,
                    ItemView = typeof(ItemView), ItemScene = nameof(ItemScene),
                    Converter = typeof(TitleConverter))]
                private Godot.VBoxContainer _items = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics, static d => d.Id == "TPV011");
        Assert.Equal("TPV011", diagnostic.Id);
    }

    [Fact]
    public void ReportsIncompatibleLinkKind()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Title")]
                [BindTo("Value", Kind = LinkKind.Toggle)]
                private Godot.Label _title = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV010", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void GeneratesCommandParameterGetter()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class SearchViewModel : Telepath.Core.IViewModel
            {
                public object Query { get; } = new();
                public object Search { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<SearchViewModel>]
            public partial class SearchView : Godot.Control
            {
                [NodeInject("%Query")]
                [BindTo(nameof(SearchViewModel.Query))]
                private Godot.LineEdit _query = null!;

                [NodeInject("%Search")]
                [BindTo(nameof(SearchViewModel.Search), Parameter = nameof(_query))]
                private Godot.Button _search = null!;

                public override partial void _Notification(int what);

                private SearchViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("bindings.Bind(vm.@Query, @_query.Text())", generated);
        Assert.Contains("bindings.BindCommand(vm.@Search, @_search, () => @_query.Text)", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesLineEditCommandWithoutParameter()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class SearchViewModel : Telepath.Core.IViewModel
            {
                public object Search { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<SearchViewModel>]
            public partial class SearchView : Godot.Control
            {
                [NodeInject("%Query")]
                [BindTo(nameof(SearchViewModel.Search), Kind = LinkKind.Command)]
                private Godot.LineEdit _query = null!;

                public override partial void _Notification(int what);

                private SearchViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("bindings.BindCommand(vm.@Search, @_query)", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesMultipleBindToOnSameField()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class SearchViewModel : Telepath.Core.IViewModel
            {
                public object Query { get; } = new();
                public object Search { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<SearchViewModel>]
            public partial class SearchView : Godot.Control
            {
                [NodeInject("%Query")]
                [BindTo(nameof(SearchViewModel.Query))]
                [BindTo(nameof(SearchViewModel.Search), Kind = LinkKind.Command)]
                private Godot.LineEdit _query = null!;

                [NodeInject("%Search")]
                [BindTo(nameof(SearchViewModel.Search), Parameter = nameof(_query))]
                private Godot.Button _search = null!;

                public override partial void _Notification(int what);

                private SearchViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Equal(1, CountOccurrences(generated, "@_query = GetNode<global::Godot.LineEdit>(\"%Query\")"));
        Assert.Contains("bindings.Bind(vm.@Query, @_query.Text())", generated);
        Assert.Contains("bindings.BindCommand(vm.@Search, @_query)", generated);
        Assert.Contains("bindings.BindCommand(vm.@Search, @_search, () => @_query.Text)", generated);
        Assert.DoesNotContain("OnBind(vm, bindings)", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsBindToWithoutNodeInject()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [BindTo("Query")]
                private Godot.LineEdit _query = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Query { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV010", diagnostic.Id);
        Assert.Contains("NodeInject", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsUnknownCommandParameter()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Search")]
                [BindTo("Search", Parameter = "Missing")]
                private Godot.Button _search = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV010", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsParameterOnNonCommand()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Title")]
                [BindTo("Title", Parameter = nameof(_title))]
                private Godot.Label _title = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV010", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void GeneratesBindToConverter()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class CountTextConverter : Telepath.Core.IValueConverter<int, string>
            {
                public string Convert(int value) => $"Count: {value}";
            }

            public sealed class CounterViewModel : Telepath.Core.IViewModel
            {
                public object Count { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<CounterViewModel>]
            public partial class CounterView : Godot.Control
            {
                [NodeInject("%CountLabel")]
                [BindTo(nameof(CounterViewModel.Count), Converter = typeof(CountTextConverter))]
                private Godot.Label _countLabel = null!;

                public override partial void _Notification(int what);

                private CounterViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains(
            "bindings.Bind(vm.@Count, @_countLabel.Text(), new global::Demo.CountTextConverter())",
            generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesBuiltInToStringConverter()
    {
        const string source = """
            using Telepath.Godot;

            namespace Demo;

            public sealed class CounterViewModel : Telepath.Core.IViewModel
            {
                public object Count { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<CounterViewModel>]
            public partial class CounterView : Godot.Control
            {
                [NodeInject("%CountLabel")]
                [BindTo(nameof(CounterViewModel.Count), Converter = typeof(Telepath.Core.ToStringConverter<int>))]
                private Godot.Label _countLabel = null!;

                public override partial void _Notification(int what);

                private CounterViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains(
            "bindings.Bind(vm.@Count, @_countLabel.Text(), new global::Telepath.Core.ToStringConverter<int>())",
            generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsConverterOnCommand()
    {
        const string source = """
            using Telepath.Godot;

            public sealed class CountTextConverter : Telepath.Core.IValueConverter<int, string>
            {
                public string Convert(int value) => value.ToString();
            }

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Increment")]
                [BindTo("Increment", Converter = typeof(CountTextConverter))]
                private Godot.Button _increment = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Increment { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV011", diagnostic.Id);
        Assert.Contains("command", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsConverterThatDoesNotImplementInterface()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Title")]
                [BindTo("Title", Converter = typeof(string))]
                private Godot.Label _title = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Title { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV011", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsConverterWithoutPublicParameterlessConstructor()
    {
        const string source = """
            using Telepath.Godot;

            public sealed class CountTextConverter : Telepath.Core.IValueConverter<int, string>
            {
                public CountTextConverter(int unused) { }
                public string Convert(int value) => value.ToString();
            }

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [NodeInject("%Title")]
                [BindTo("Title", Converter = typeof(CountTextConverter))]
                private Godot.Label _title = null!;

                public override partial void _Notification(int what);

                private TestViewModel CreateViewModel() => new();
            }

            public sealed class TestViewModel : Telepath.Core.IViewModel
            {
                public object Title { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPV011", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void GeneratesInjectOnlyWithOnBind()
    {
        const string source = """
            using Telepath.Core;
            using Telepath.Godot;

            namespace Demo;

            public sealed class ListViewModel : Telepath.Core.IViewModel
            {
                public object Items { get; } = new();
                public bool IsDisposed => false;
                public void Dispose() { }
            }

            [TelepathView<ListViewModel>]
            public partial class ListView : Godot.Control
            {
                [NodeInject("%Items")]
                private Godot.ItemList _items = null!;

                public override partial void _Notification(int what);

                private ListViewModel CreateViewModel() => new();

                private void OnBind(ListViewModel vm, BindingSet bindings) { }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("__TelepathOnReady", generated);
        Assert.Contains("GetNode<global::Godot.ItemList>(\"%Items\")", generated);
        Assert.DoesNotContain("__TelepathOnBind", generated);
        Assert.Contains("OnBind", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    private static GeneratorRunResult RunGenerator(string viewSource)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(RuntimeStubs, parseOptions),
            CSharpSyntaxTree.ParseText(viewSource, parseOptions),
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees,
            GetPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new TelepathIncrementalGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out _);

        var runResult = driver.GetRunResult();
        var generatorResult = Assert.Single(runResult.Results);
        return new GeneratorRunResult(
            generatorResult.Diagnostics,
            generatorResult.GeneratedSources,
            outputCompilation);
    }

    private static IEnumerable<MetadataReference> GetPlatformReferences()
    {
        var trustedAssemblies =
            (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path));
    }

    private static void AssertNoCompilationErrors(Compilation compilation)
    {
        var errors = compilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        Assert.Empty(errors);
    }

    private static string CombinedGenerated(GeneratorRunResult result)
        => string.Join("\n", result.GeneratedSources.Select(static source => source.SourceText.ToString()));

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = 0; (index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0; index += value.Length)
        {
            count++;
        }

        return count;
    }

    private sealed class GeneratorRunResult(
        IEnumerable<Diagnostic> diagnostics,
        IEnumerable<GeneratedSourceResult> generatedSources,
        Compilation outputCompilation)
    {
        public IReadOnlyList<Diagnostic> Diagnostics { get; } = diagnostics.ToArray();

        public IReadOnlyList<GeneratedSourceResult> GeneratedSources { get; } =
            generatedSources.ToArray();

        public Compilation OutputCompilation { get; } = outputCompilation;
    }

    private const string RuntimeStubs = """
        namespace Telepath.Core
        {
            public interface IViewModel : System.IDisposable
            {
                bool IsDisposed { get; }
            }

            public interface IValueConverter<in TSource, out TTarget>
            {
                TTarget Convert(TSource value);
            }

            public interface ITwoWayValueConverter<TSource, TTarget> : IValueConverter<TSource, TTarget>
            {
                TSource ConvertBack(TTarget value);
            }

            public static class ToStringConverter
            {
                public static string Convert<T>(T value) => value?.ToString() ?? string.Empty;
            }

            public sealed class ToStringConverter<T> : IValueConverter<T, string>
            {
                public string Convert(T value) => ToStringConverter.Convert(value);
            }

            public sealed class BindingSet
            {
                public void Bind(object source, object target) { }
                public void Bind(object source, object target, object converter) { }
                public void Bind(object source, object target, System.Func<object, string> convert) { }
                public void BindCommand(object command, object button) { }
                public void BindCommand<T>(object command, object button, System.Func<T> getParameter) { }
                public void BindItems(object source, object target) { }
                public void BindItems(object source, object target, object converter) { }
            }
        }

        namespace Godot
        {
            public class CanvasItem
            {
            }

            public class Control : CanvasItem
            {
                public virtual void _Notification(int what) { }
                public T GetNode<T>(string path) => default!;
            }

            public class Label : Control
            {
                public string Text { get; set; }
            }

            public class RichTextLabel : Control
            {
                public string Text { get; set; }
            }

            public class LineEdit : Control
            {
                public string Text { get; set; }
            }

            public class TextEdit : Control
            {
                public string Text { get; set; }
            }

            public class BaseButton : Control
            {
            }

            public class Button : BaseButton
            {
            }

            public class CheckBox : Button
            {
            }

            public class CheckButton : Button
            {
            }

            public class OptionButton : Button
            {
            }

            public class ItemList : Control
            {
            }

            public class Container : Control
            {
            }

            public class VBoxContainer : Container
            {
            }

            public class PackedScene
            {
            }

            public class Range : Control
            {
            }

            public class Slider : Range
            {
            }

            public class ProgressBar : Range
            {
            }

            public class SpinBox : Range
            {
            }
        }

        namespace Telepath.Godot
        {
            public enum LinkKind
            {
                Auto = 0,
                Text,
                Command,
                Toggle,
                Value,
                Selected,
                Visible,
                Disabled,
                Items,
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class TelepathViewAttribute<TViewModel> : System.Attribute
                where TViewModel : class, Telepath.Core.IViewModel
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false)]
            public sealed class NodeInjectAttribute : System.Attribute
            {
                public NodeInjectAttribute(string nodePath)
                {
                    NodePath = nodePath;
                }

                public string NodePath { get; }
            }

            [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
            public sealed class BindToAttribute : System.Attribute
            {
                public BindToAttribute(string member)
                {
                    Member = member;
                }

                public string Member { get; }
                public LinkKind Kind { get; set; }
                public string Parameter { get; set; }
                public System.Type Converter { get; set; }
                public System.Type ItemView { get; set; }
                public string ItemScene { get; set; }
            }

            public sealed class ViewLifecycle<TViewModel>
                where TViewModel : class, Telepath.Core.IViewModel
            {
                public ViewLifecycle(
                    global::Godot.Control owner,
                    System.Action onReady,
                    System.Func<TViewModel> createViewModel,
                    System.Action<TViewModel, global::Telepath.Core.BindingSet> onBind)
                {
                }

                public TViewModel? ViewModel { get; set; }

                public void HandleNotification(int what) { }
            }

            public static class GodotTargets
            {
                public static object Text(this global::Godot.Label label) => label;
                public static object Text(this global::Godot.RichTextLabel label) => label;
                public static object Text(this global::Godot.LineEdit edit) => edit;
                public static object Text(this global::Godot.TextEdit edit) => edit;
                public static object Toggle(this global::Godot.BaseButton button) => button;
                public static object Value(this global::Godot.Range range) => range;
                public static object Selected(this global::Godot.OptionButton button) => button;
                public static object Selected(this global::Godot.ItemList list) => list;
                public static object Visible(this global::Godot.CanvasItem node) => node;
                public static object Disabled(this global::Godot.BaseButton button) => button;
            }

            public static class GodotCollectionTargets
            {
                public static object Items(this global::Godot.ItemList list) => list;
                public static object Items(this global::Godot.OptionButton button) => button;
                public static object Items<TView, TViewModel>(
                    this global::Godot.Container container,
                    global::Godot.PackedScene scene)
                    => container;
            }

            public interface ITelepathView<TViewModel>
                where TViewModel : class, Telepath.Core.IViewModel
            {
                TViewModel? ViewModel { get; set; }
            }
        }
        """;
}
