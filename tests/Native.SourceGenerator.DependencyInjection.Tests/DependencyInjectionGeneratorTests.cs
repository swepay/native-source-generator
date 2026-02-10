namespace Native.SourceGenerator.DependencyInjection.Tests;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

public class DependencyInjectionGeneratorTests
{
    [Fact]
    public void Generator_WithRegisterAttribute_GeneratesConstructor()
    {
        var source = """
            using Native.SourceGenerator.DependencyInjection;

            namespace TestNamespace;

            public interface IMyService { }
            public interface ILogger { }

            [Register(typeof(IMyService), ServiceLifetime.Singleton)]
            public partial class MyService : IMyService
            {
                [Inject] private readonly ILogger _logger;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        var generatedTrees = compilation.SyntaxTrees.ToList();
        generatedTrees.Count.ShouldBeGreaterThan(1);

        SyntaxTree? constructorSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyService.Constructor"));

        constructorSource.ShouldNotBeNull();
        var code = constructorSource.ToString();
        code.ShouldContain("public MyService(");
        code.ShouldContain("_logger = logger;");
    }

    [Fact]
    public void Generator_WithMultipleFields_GeneratesConstructorWithAllParameters()
    {
        var source = """
            using Native.SourceGenerator.DependencyInjection;

            namespace TestNamespace;

            public interface IMyService { }
            public interface IRepository { }
            public interface ILogger { }
            public interface IClock { }

            [Register(typeof(IMyService), ServiceLifetime.Scoped)]
            public partial class MyService : IMyService
            {
                [Inject] private readonly IRepository _repository;
                [Inject] private readonly ILogger _logger;
                [Inject] private readonly IClock _clock;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        var generatedTrees = compilation.SyntaxTrees.ToList();
        SyntaxTree? constructorSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyService.Constructor"));

        constructorSource.ShouldNotBeNull();
        var code = constructorSource.ToString();
        code.ShouldContain("IRepository repository");
        code.ShouldContain("ILogger logger");
        code.ShouldContain("IClock clock");
        code.ShouldContain("_repository = repository;");
        code.ShouldContain("_logger = logger;");
        code.ShouldContain("_clock = clock;");
    }

    [Fact]
    public void Generator_WithGroup_GeneratesGroupedServiceRegistration()
    {
        var source = """
            using Native.SourceGenerator.DependencyInjection;

            namespace TestNamespace;

            public interface IMyService { }
            public interface ILogger { }

            [Register(typeof(IMyService), ServiceLifetime.Singleton, Group = "CoreServices")]
            public partial class MyService : IMyService
            {
                [Inject] private readonly ILogger _logger;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        var generatedTrees = compilation.SyntaxTrees.ToList();
        SyntaxTree? registrationSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NativeGeneratedServices"));

        registrationSource.ShouldNotBeNull();
        var code = registrationSource.ToString();
        code.ShouldContain("AddCoreServices");
    }

    [Fact]
    public void Generator_WithNoFields_DoesNotGenerateConstructor()
    {
        var source = """
            using Native.SourceGenerator.DependencyInjection;

            namespace TestNamespace;

            public interface IMyService { }

            [Register(typeof(IMyService))]
            public partial class MyService : IMyService
            {
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        var generatedTrees = compilation.SyntaxTrees.ToList();

        // Should not generate a constructor file
        SyntaxTree? constructorSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyService.Constructor"));

        constructorSource.ShouldBeNull();

        // But should still generate registration
        SyntaxTree? registrationSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NativeGeneratedServices"));

        registrationSource.ShouldNotBeNull();
    }

    [Fact]
    public void Generator_ProducesAttributes()
    {
        var source = """
            namespace TestNamespace;
            public class Empty { }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        var generatedTrees = compilation.SyntaxTrees.ToList();

        SyntaxTree? registerAttr = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("RegisterAttribute.g.cs"));

        registerAttr.ShouldNotBeNull();

        SyntaxTree? injectAttr = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("InjectAttribute.g.cs"));

        injectAttr.ShouldNotBeNull();
    }

    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new DependencyInjectionGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation? outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

        return (outputCompilation, diagnostics);
    }
}
