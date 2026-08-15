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
    public void ReportsMissingOnBindWhenNoLinkTo()
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
    public void GeneratesLinkToReadyAndBindWithoutOnBind()
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
                [LinkTo("%CountLabel", nameof(CounterViewModel.CountText))]
                private Godot.Label _countLabel = null!;

                [LinkTo("%IncrementButton", nameof(CounterViewModel.Increment))]
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
        Assert.Contains("bindings.BindText(vm.@CountText, @_countLabel)", generated);
        Assert.Contains("bindings.BindCommand(vm.@Increment, @_incrementButton)", generated);
        Assert.Contains("using Telepath.Godot;", generated);
        Assert.Contains("global::Telepath.Core.BindingSet bindings", generated);
        Assert.DoesNotContain("OnBind(vm, bindings)", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsUnsupportedLinkToControl()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [LinkTo("%Root", "Value")]
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
                [LinkTo("%Title", nameof(FormViewModel.Title))]
                private Godot.Label _title = null!;

                [LinkTo("%Body", nameof(FormViewModel.Body))]
                private Godot.RichTextLabel _body = null!;

                [LinkTo("%Name", nameof(FormViewModel.Name))]
                private Godot.LineEdit _name = null!;

                [LinkTo("%Notes", nameof(FormViewModel.Notes))]
                private Godot.TextEdit _notes = null!;

                [LinkTo("%Enabled", nameof(FormViewModel.Enabled))]
                private Godot.CheckBox _enabled = null!;

                [LinkTo("%Featured", nameof(FormViewModel.Featured))]
                private Godot.CheckButton _featured = null!;

                [LinkTo("%Choice", nameof(FormViewModel.Choice))]
                private Godot.OptionButton _choice = null!;

                [LinkTo("%Volume", nameof(FormViewModel.Volume))]
                private Godot.Slider _volume = null!;

                [LinkTo("%Progress", nameof(FormViewModel.Progress))]
                private Godot.ProgressBar _progress = null!;

                [LinkTo("%Save", nameof(FormViewModel.Save))]
                private Godot.Button _save = null!;

                public override partial void _Notification(int what);

                private FormViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("bindings.BindText(vm.@Title, @_title)", generated);
        Assert.Contains("bindings.BindText(vm.@Body, @_body)", generated);
        Assert.Contains("bindings.BindText(vm.@Name, @_name)", generated);
        Assert.Contains("bindings.BindText(vm.@Notes, @_notes)", generated);
        Assert.Contains("bindings.BindToggle(vm.@Enabled, @_enabled)", generated);
        Assert.Contains("bindings.BindToggle(vm.@Featured, @_featured)", generated);
        Assert.Contains("bindings.BindSelected(vm.@Choice, @_choice)", generated);
        Assert.Contains("bindings.BindValue(vm.@Volume, @_volume)", generated);
        Assert.Contains("bindings.BindValue(vm.@Progress, @_progress)", generated);
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
                [LinkTo("%Panel", nameof(PanelViewModel.ShowPanel), Kind = LinkKind.Visible)]
                private Godot.Control _panel = null!;

                [LinkTo("%Lock", nameof(PanelViewModel.Locked), Kind = LinkKind.Disabled)]
                private Godot.Button _lock = null!;

                [LinkTo("%Weird", nameof(PanelViewModel.DoThing), Kind = LinkKind.Command)]
                private Godot.CheckBox _weird = null!;

                public override partial void _Notification(int what);

                private PanelViewModel CreateViewModel() => new();
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("bindings.BindVisible(vm.@ShowPanel, @_panel)", generated);
        Assert.Contains("bindings.BindDisabled(vm.@Locked, @_lock)", generated);
        Assert.Contains("bindings.BindCommand(vm.@DoThing, @_weird)", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsIncompatibleLinkKind()
    {
        const string source = """
            using Telepath.Godot;

            [TelepathView<TestViewModel>]
            public partial class TestView : Godot.Control
            {
                [LinkTo("%Title", "Value", Kind = LinkKind.Toggle)]
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
        Assert.Equal("TPV009", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
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

            public sealed class BindingSet
            {
                public void BindText(object source, object target) { }
                public void BindCommand(object command, object button) { }
                public void BindToggle(object source, object button) { }
                public void BindValue(object source, object range) { }
                public void BindSelected(object source, object button) { }
                public void BindVisible(object source, object node) { }
                public void BindDisabled(object source, object button) { }
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
            }

            public class RichTextLabel : Control
            {
            }

            public class LineEdit : Control
            {
            }

            public class TextEdit : Control
            {
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
            }

            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class TelepathViewAttribute<TViewModel> : System.Attribute
                where TViewModel : class, Telepath.Core.IViewModel
            {
            }

            [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
            public sealed class LinkToAttribute : System.Attribute
            {
                public LinkToAttribute(string nodePath, string member)
                {
                    NodePath = nodePath;
                    Member = member;
                }

                public string NodePath { get; }
                public string Member { get; }
                public LinkKind Kind { get; set; }
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
        }
        """;
}
