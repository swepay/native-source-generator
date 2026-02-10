namespace NativeMediator.SourceGenerator.DependencyInjection.Tests;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

public class MediatorDependencyInjectionGeneratorTests
{
    [Fact]
    public void Generator_ProducesAttributes()
    {
        var source = """
            namespace TestNamespace;
            public class Empty { }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);
        var generatedTrees = compilation.SyntaxTrees.ToList();

        SyntaxTree? handlerAttr = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MediatorHandlerAttribute.g.cs"));

        handlerAttr.ShouldNotBeNull();
    }

    [Fact]
    public void Generator_WithMediatorHandler_GeneratesRegistration()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            using NativeMediator.SourceGenerator.DependencyInjection;

            namespace TestNamespace;

            public record GetUserQuery(int Id);
            public record UserDto(int Id, string Name);

            [MediatorHandler]
            public class GetUserQueryHandler
            {
                public Task<UserDto> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
                {
                    return Task.FromResult(new UserDto(query.Id, "Test"));
                }
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);
        var generatedTrees = compilation.SyntaxTrees.ToList();

        SyntaxTree? registrationSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NativeMediatorServices.g.cs"));

        registrationSource.ShouldNotBeNull();
        var code = registrationSource.ToString();
        code.ShouldContain("AddNativeMediatorHandlers");
        code.ShouldContain("GetUserQueryHandler");
    }

    [Fact]
    public void Generator_WithMultipleHandlers_GeneratesAllRegistrations()
    {
        var source = """
            using System.Threading;
            using System.Threading.Tasks;
            using NativeMediator.SourceGenerator.DependencyInjection;

            namespace TestNamespace;

            public record Query1(int Id);
            public record Query2(string Name);
            public record Result1(int Value);
            public record Result2(string Value);

            [MediatorHandler]
            public class Handler1
            {
                public Task<Result1> HandleAsync(Query1 query, CancellationToken ct)
                    => Task.FromResult(new Result1(1));
            }

            [MediatorHandler]
            public class Handler2
            {
                public Task<Result2> HandleAsync(Query2 query, CancellationToken ct)
                    => Task.FromResult(new Result2("test"));
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);
        var generatedTrees = compilation.SyntaxTrees.ToList();

        SyntaxTree? registrationSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NativeMediatorServices.g.cs"));

        registrationSource.ShouldNotBeNull();
        var code = registrationSource.ToString();
        code.ShouldContain("Handler1");
        code.ShouldContain("Handler2");
    }

    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MediatorDependencyInjectionGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation? outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

        return (outputCompilation, diagnostics);
    }
}
