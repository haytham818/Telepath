using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Telepath.SourceGenerator.Tests;

public sealed class TelepathViewGeneratorTests
{
    [Fact]
    public void GeneratesLifecycleBridgeWithoutGodotInternalGlue()
    {
        const string source = """
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
        }

        namespace Godot
        {
            public class Control
            {
                public virtual void _Notification(int what) { }
            }
        }

        namespace Telepath.Godot
        {
            [System.AttributeUsage(System.AttributeTargets.Class)]
            public sealed class TelepathViewAttribute<TViewModel> : System.Attribute
                where TViewModel : class, Telepath.Core.IViewModel
            {
            }

            public sealed class BindingSet
            {
            }

            public sealed class ViewLifecycle<TViewModel>
                where TViewModel : class, Telepath.Core.IViewModel
            {
                public ViewLifecycle(
                    global::Godot.Control owner,
                    System.Action onReady,
                    System.Func<TViewModel> createViewModel,
                    System.Action<TViewModel, BindingSet> onBind)
                {
                }

                public TViewModel? ViewModel { get; set; }

                public void HandleNotification(int what) { }
            }
        }
        """;
}
