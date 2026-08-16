using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Telepath.SourceGenerator.Tests;

public sealed class TelepathViewModelGeneratorTests
{
    [Fact]
    public void GeneratesBindableCommandAndDerivedMembers()
    {
        const string source = """
            using R3;
            using Telepath.Core;

            namespace Demo;

            public sealed partial class CounterViewModel : ViewModel
            {
                [Bindable]
                private int _count = 1;

                [Bindable(nameof(Count))]
                private string GetCountText(int count) => $"Count: {count}";

                [Command(CanExecute = nameof(CanIncrement))]
                private void OnIncrement() => Count.Value++;

                private Observable<bool> CanIncrement() => Count.Select(static c => c < 10);
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("public global::R3.BindableReactiveProperty<int> @Count", generated);
        Assert.Contains("new global::R3.BindableReactiveProperty<int>(@_count)", generated);
        Assert.Contains("public global::R3.BindableReactiveProperty<string> @CountText", generated);
        Assert.Contains("@Count.Select(@GetCountText)", generated);
        Assert.Contains("public global::R3.ReactiveCommand @IncrementCommand", generated);
        Assert.Contains("@CanIncrement().ToReactiveCommand(_ => @OnIncrement())", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesParameterizedCommand()
    {
        const string source = """
            using R3;
            using Telepath.Core;

            namespace Demo;

            public sealed partial class SearchViewModel : ViewModel
            {
                [Command(CanExecute = nameof(CanSearch))]
                private void OnSearch(string query) { }

                private Observable<bool> CanSearch() => Observable.Return(true);
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("public global::R3.ReactiveCommand<string> @SearchCommand", generated);
        Assert.Contains("@CanSearch().ToReactiveCommand<string>(arg => @OnSearch(arg))", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesAsyncCommand()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using R3;
            using Telepath.Core;

            namespace Demo;

            public sealed partial class SearchViewModel : ViewModel
            {
                [Command(CanExecute = nameof(CanSearch))]
                private async Task OnSearch(string query, CancellationToken cancellationToken)
                {
                    await Task.CompletedTask;
                }

                private Observable<bool> CanSearch() => Observable.Return(true);
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("public global::R3.ReactiveCommand<string> @SearchCommand", generated);
        Assert.Contains(
            "AsyncCommand<string>(async (arg, ct) => await @OnSearch(arg, ct), @CanSearch())",
            generated);
        Assert.DoesNotContain("Track(AsyncCommand", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void GeneratesParameterlessAsyncCommandWithCancellationToken()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Telepath.Core;

            public sealed partial class SampleViewModel : ViewModel
            {
                [Command]
                private ValueTask OnSave(CancellationToken cancellationToken) => ValueTask.CompletedTask;
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("public global::R3.ReactiveCommand @SaveCommand", generated);
        Assert.Contains("AsyncCommand(async ct => await @OnSave(ct))", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsTaskResultCommand()
    {
        const string source = """
            using System.Threading.Tasks;
            using Telepath.Core;

            public sealed partial class SampleViewModel : ViewModel
            {
                [Command]
                private Task<int> OnGo() => Task.FromResult(1);
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPM004", diagnostic.Id);
        Assert.Contains("void, Task, or ValueTask", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsAsyncVoidCommand()
    {
        const string source = """
            using Telepath.Core;

            public sealed partial class SampleViewModel : ViewModel
            {
                [Command]
                private async void OnGo() { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPM004", diagnostic.Id);
        Assert.Contains("async void", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void CommandNameOverrideSkipsCommandSuffix()
    {
        const string source = """
            using Telepath.Core;

            public sealed partial class SampleViewModel : ViewModel
            {
                [Command(Name = "Go")]
                private void OnSearch(string query) { }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("public global::R3.ReactiveCommand<string> @Go", generated);
        Assert.DoesNotContain("GoCommand", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsCommandWithTooManyParameters()
    {
        const string source = """
            using Telepath.Core;

            public sealed partial class SampleViewModel : ViewModel
            {
                [Command]
                private void OnGo(string a, int b) { }
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPM004", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsInvalidFromMember()
    {
        const string source = """
            using Telepath.Core;

            public sealed partial class SampleViewModel : ViewModel
            {
                [Bindable(nameof(Missing))]
                private string GetLabel(int value) => value.ToString();
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPM003", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsNonPartialViewModel()
    {
        const string source = """
            using Telepath.Core;

            public sealed class SampleViewModel : ViewModel
            {
                [Bindable]
                private int _count;
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPM001", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void GeneratesLazyObservableListProperty()
    {
        const string source = """
            using ObservableCollections;
            using Telepath.Core;

            namespace Demo;

            public sealed partial class ListViewModel : ViewModel
            {
                [Bindable]
                private ObservableList<string>? _items;
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains(
            "public global::ObservableCollections.ObservableList<string> @Items",
            generated);
        Assert.Contains(
            "@_items ??= new global::ObservableCollections.ObservableList<string>();",
            generated);
        Assert.DoesNotContain("BindableReactiveProperty", generated);
        Assert.DoesNotContain("Track(", generated);
        AssertNoCompilationErrors(result.OutputCompilation);
    }

    [Fact]
    public void ReportsReadonlyObservableListBindable()
    {
        const string source = """
            using ObservableCollections;
            using Telepath.Core;

            public sealed partial class SampleViewModel : ViewModel
            {
                [Bindable]
                private readonly ObservableList<string>? _items;
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPM002", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsFromOnObservableListBindable()
    {
        const string source = """
            using ObservableCollections;
            using Telepath.Core;

            public sealed partial class SampleViewModel : ViewModel
            {
                [Bindable("Count")]
                private ObservableList<string>? _items;
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPM002", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReportsInvalidCanExecute()
    {
        const string source = """
            using Telepath.Core;

            public sealed partial class SampleViewModel : ViewModel
            {
                [Command(CanExecute = nameof(CanGo))]
                private void OnGo() { }

                private bool CanGo() => true;
            }
            """;

        var result = RunGenerator(source);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TPM005", diagnostic.Id);
        Assert.Empty(result.GeneratedSources);
    }

    private static GeneratorRunResult RunGenerator(string viewModelSource)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(RuntimeStubs, parseOptions),
            CSharpSyntaxTree.ParseText(viewModelSource, parseOptions),
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

            public abstract class ViewModel : IViewModel
            {
                public bool IsDisposed => false;

                public void Dispose() { }

                protected T Track<T>(T disposable)
                    where T : System.IDisposable
                    => disposable;

                protected R3.ReactiveCommand AsyncCommand(
                    System.Func<System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> execute,
                    R3.Observable<bool>? canExecute = null)
                    => new(_ => { });

                protected R3.ReactiveCommand<T> AsyncCommand<T>(
                    System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> execute,
                    R3.Observable<bool>? canExecute = null)
                    => new(_ => { });
            }

            [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Method)]
            public sealed class BindableAttribute : System.Attribute
            {
                public BindableAttribute() : this(System.Array.Empty<string>()) { }

                public BindableAttribute(params string[] from)
                {
                    From = from;
                }

                public string[] From { get; }
                public string? Name { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class CommandAttribute : System.Attribute
            {
                public string? CanExecute { get; set; }
                public string? Name { get; set; }
            }
        }

        namespace R3
        {
            public class Observable<T>
            {
                public Observable<TResult> Select<TResult>(System.Func<T, TResult> selector) => new();
            }

            public class BindableReactiveProperty<T> : Observable<T>, System.IDisposable
            {
                public BindableReactiveProperty(T value) => Value = value;
                public T Value { get; set; }
                public void Dispose() { }
            }

            public static class BindableReactivePropertyExtensions
            {
                public static BindableReactiveProperty<T> ToBindableReactiveProperty<T>(
                    this Observable<T> source,
                    T initialValue)
                    => new(initialValue);
            }

            public readonly struct Unit
            {
            }

            public class ReactiveCommand : System.IDisposable
            {
                public ReactiveCommand(System.Action<Unit> execute) { }
                public void Dispose() { }
            }

            public class ReactiveCommand<T> : System.IDisposable
            {
                public ReactiveCommand(System.Action<T> execute) { }
                public void Dispose() { }
            }

            public static class ReactiveCommandExtensions
            {
                public static ReactiveCommand ToReactiveCommand(
                    this Observable<bool> source,
                    System.Action<Unit> execute)
                    => new(execute);

                public static ReactiveCommand<T> ToReactiveCommand<T>(
                    this Observable<bool> source,
                    System.Action<T> execute)
                    => new(execute);
            }

            public static class Observable
            {
                public static Observable<T> Return<T>(T value) => new();

                public static Observable<TResult> CombineLatest<T1, T2, TResult>(
                    Observable<T1> source1,
                    Observable<T2> source2,
                    System.Func<T1, T2, TResult> selector)
                    => new();
            }
        }

        namespace ObservableCollections
        {
            public class ObservableList<T>
            {
            }
        }
        """;
}
