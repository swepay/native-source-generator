namespace NativeFluentValidator.SourceGenerator.DependencyInjection.Tests;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

public class ValidatorDependencyInjectionGeneratorTests
{
    [Fact]
    public void Generator_ProducesAttributes()
    {
        var source = """
            namespace TestNamespace;
            public class Empty { }
            """;

        var (compilation, diagnostics) = RunGenerator(source);
        var generatedTrees = compilation.SyntaxTrees.ToList();

        var validatorAttr = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NativeValidatorAttribute.g.cs"));

        validatorAttr.ShouldNotBeNull();
    }

    [Fact]
    public void Generator_WithNativeValidatorAttribute_GeneratesRegistration()
    {
        var source = """
            using NativeFluentValidator.SourceGenerator.DependencyInjection;

            namespace TestNamespace;

            public record CreateUserRequest(string Name, string Email);

            public abstract class AbstractValidator<T> { }

            [NativeValidator]
            public partial class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
            {
            }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        var generatedTrees = compilation.SyntaxTrees.ToList();

        var registrationSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NativeFluentValidatorServices.g.cs"));

        registrationSource.ShouldNotBeNull();
        var code = registrationSource.ToString();
        code.ShouldContain("AddNativeFluentValidators");
        code.ShouldContain("CreateUserRequestValidator");
    }

    [Fact]
    public void Generator_WithMultipleValidators_GeneratesAllRegistrations()
    {
        var source = """
            using NativeFluentValidator.SourceGenerator.DependencyInjection;

            namespace TestNamespace;

            public record Request1(string Value);
            public record Request2(int Value);

            public abstract class AbstractValidator<T> { }

            [NativeValidator]
            public partial class Request1Validator : AbstractValidator<Request1> { }

            [NativeValidator]
            public partial class Request2Validator : AbstractValidator<Request2> { }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        var generatedTrees = compilation.SyntaxTrees.ToList();

        var registrationSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NativeFluentValidatorServices.g.cs"));

        registrationSource.ShouldNotBeNull();
        var code = registrationSource.ToString();
        code.ShouldContain("Request1Validator");
        code.ShouldContain("Request2Validator");
    }

    [Fact]
    public void Generator_WithGroup_GeneratesGroupedMethod()
    {
        var source = """
            using NativeFluentValidator.SourceGenerator.DependencyInjection;

            namespace TestNamespace;

            public record UserRequest(string Name);

            public abstract class AbstractValidator<T> { }

            [NativeValidator(Group = "UserValidations")]
            public partial class UserRequestValidator : AbstractValidator<UserRequest> { }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        var generatedTrees = compilation.SyntaxTrees.ToList();

        var registrationSource = generatedTrees
            .FirstOrDefault(t => t.FilePath.Contains("NativeFluentValidatorServices.g.cs"));

        registrationSource.ShouldNotBeNull();
        var code = registrationSource.ToString();
        code.ShouldContain("AddUserValidations");
    }

    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ValidatorDependencyInjectionGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return (outputCompilation, diagnostics);
    }
}
