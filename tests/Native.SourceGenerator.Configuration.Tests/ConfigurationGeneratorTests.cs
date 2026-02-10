namespace Native.SourceGenerator.Configuration.Tests;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

public class ConfigurationGeneratorTests
{
    [Fact]
    public void Generator_WithEnvironmentConfigAttribute_GeneratesInjectConfigurationMethod()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class DatabaseService
            {
                [EnvironmentConfig("DATABASE_CONNECTION_STRING")]
                private readonly string _connectionString;
            }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        generatedTrees.Count.ShouldBeGreaterThan(1);

        var generatedCode = generatedTrees[^1].ToString();
        generatedCode.ShouldContain("__InjectConfiguration");
        generatedCode.ShouldContain("DATABASE_CONNECTION_STRING");
    }

    [Fact]
    public void Generator_WithRequiredConfig_GeneratesThrowOnMissing()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class AppSettings
            {
                [EnvironmentConfig("API_KEY", Required = true)]
                private readonly string _apiKey;
            }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("InvalidOperationException");
        generatedCode.ShouldContain("Missing required configuration");
    }

    [Fact]
    public void Generator_WithDefaultValue_GeneratesDefaultAssignment()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class AppSettings
            {
                [EnvironmentConfig("LOG_LEVEL", Required = false, DefaultValue = "Information")]
                private readonly string _logLevel;
            }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("Information");
    }

    [Fact]
    public void Generator_WithIntType_GeneratesIntParse()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class AppSettings
            {
                [EnvironmentConfig("PORT")]
                private readonly int _port;
            }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("int.Parse");
    }

    [Fact]
    public void Generator_WithBoolType_GeneratesBoolParse()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class AppSettings
            {
                [EnvironmentConfig("ENABLE_FEATURE")]
                private readonly bool _enableFeature;
            }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("bool.Parse");
    }

    [Fact]
    public void Generator_WithMultipleFields_GeneratesAllAssignments()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class AppSettings
            {
                [EnvironmentConfig("API_URL")]
                private readonly string _apiUrl;

                [EnvironmentConfig("TIMEOUT")]
                private readonly int _timeout;

                [EnvironmentConfig("DEBUG_MODE")]
                private readonly bool _debugMode;
            }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("API_URL");
        generatedCode.ShouldContain("TIMEOUT");
        generatedCode.ShouldContain("DEBUG_MODE");
        generatedCode.ShouldContain("_apiUrl");
        generatedCode.ShouldContain("_timeout");
        generatedCode.ShouldContain("_debugMode");
    }

    [Fact]
    public void Generator_GeneratesPublicMethodWithIConfiguration()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class Settings
            {
                [EnvironmentConfig("VALUE")]
                private readonly string _value;
            }
            """;

        var (compilation, diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("public void __InjectConfiguration");
        generatedCode.ShouldContain("IConfiguration configuration");
        generatedCode.ShouldContain("configuration[");
    }

    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ConfigurationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return (outputCompilation, diagnostics);
    }
}
