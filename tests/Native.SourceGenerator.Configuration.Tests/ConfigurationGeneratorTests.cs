namespace Native.SourceGenerator.Configuration.Tests;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Xunit;

public class ConfigurationGeneratorTests
{
    #region EnvironmentConfig Tests

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

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

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

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

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

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

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

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

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

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

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

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

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

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("public void __InjectConfiguration");
        generatedCode.ShouldContain("IConfiguration configuration");
        generatedCode.ShouldContain("configuration[");
    }

    #endregion

    #region AppSettings Tests

    [Fact]
    public void Generator_WithAppSettingsAttribute_GeneratesInjectConfigurationMethod()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class ServiceSettings
            {
                [AppSettings("Services:UrlBase")]
                private readonly string _urlBase;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        generatedTrees.Count.ShouldBeGreaterThan(1);

        var generatedCode = generatedTrees[^1].ToString();
        generatedCode.ShouldContain("__InjectConfiguration");
        generatedCode.ShouldContain("Services:UrlBase");
    }

    [Fact]
    public void Generator_WithAppSettingsAttribute_GeneratesConfigurationAccess()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class DatabaseConfig
            {
                [AppSettings("Database:ConnectionString")]
                private readonly string _connectionString;

                [AppSettings("Database:MaxPoolSize")]
                private readonly int _maxPoolSize;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("configuration[\"Database:ConnectionString\"]");
        generatedCode.ShouldContain("configuration[\"Database:MaxPoolSize\"]");
        generatedCode.ShouldContain("int.Parse");
    }

    [Fact]
    public void Generator_WithAppSettingsRequired_GeneratesThrowOnMissing()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class ApiSettings
            {
                [AppSettings("Api:BaseUrl", Required = true)]
                private readonly string _baseUrl;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("InvalidOperationException");
        generatedCode.ShouldContain("Missing required configuration: Api:BaseUrl");
    }

    [Fact]
    public void Generator_WithAppSettingsDefaultValue_GeneratesDefaultAssignment()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class CacheSettings
            {
                [AppSettings("Cache:ExpirationMinutes", Required = false, DefaultValue = "30")]
                private readonly int _expirationMinutes;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("int.Parse(\"30\")");
    }

    [Fact]
    public void Generator_WithAppSettingsNestedPath_GeneratesCorrectAccess()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class NestedSettings
            {
                [AppSettings("Logging:LogLevel:Default")]
                private readonly string _defaultLogLevel;

                [AppSettings("Authentication:Jwt:Secret")]
                private readonly string _jwtSecret;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("configuration[\"Logging:LogLevel:Default\"]");
        generatedCode.ShouldContain("configuration[\"Authentication:Jwt:Secret\"]");
    }

    [Fact]
    public void Generator_WithMixedAttributes_GeneratesBothConfigurations()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class MixedSettings
            {
                [EnvironmentConfig("API_KEY")]
                private readonly string _apiKey;

                [AppSettings("Services:UrlBase")]
                private readonly string _urlBase;

                [EnvironmentConfig("DEBUG_MODE", Required = false, DefaultValue = "false")]
                private readonly bool _debugMode;

                [AppSettings("Cache:Enabled", Required = false, DefaultValue = "true")]
                private readonly bool _cacheEnabled;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        // Environment config
        generatedCode.ShouldContain("configuration[\"API_KEY\"]");
        generatedCode.ShouldContain("configuration[\"DEBUG_MODE\"]");

        // AppSettings
        generatedCode.ShouldContain("configuration[\"Services:UrlBase\"]");
        generatedCode.ShouldContain("configuration[\"Cache:Enabled\"]");

        // All fields
        generatedCode.ShouldContain("_apiKey");
        generatedCode.ShouldContain("_urlBase");
        generatedCode.ShouldContain("_debugMode");
        generatedCode.ShouldContain("_cacheEnabled");
    }

    [Fact]
    public void Generator_WithAppSettingsBoolType_GeneratesBoolParse()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class FeatureFlags
            {
                [AppSettings("Features:EnableNewDashboard")]
                private readonly bool _enableNewDashboard;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> diagnostics) = RunGenerator(source);

        diagnostics.ShouldBeEmpty();

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var generatedCode = generatedTrees[^1].ToString();

        generatedCode.ShouldContain("bool.Parse");
        generatedCode.ShouldContain("Features:EnableNewDashboard");
    }

    [Fact]
    public void Generator_AppSettingsAttributeIsGenerated()
    {
        var source = """
            using Native.SourceGenerator.Configuration;

            namespace TestNamespace;

            public partial class TestClass
            {
                [AppSettings("Test:Value")]
                private readonly string _value;
            }
            """;

        (Compilation? compilation, ImmutableArray<Diagnostic> _) = RunGenerator(source);

        var generatedTrees = compilation.SyntaxTrees.ToList();
        var appSettingsAttributeSource = generatedTrees
            .FirstOrDefault(t => t.ToString().Contains("AppSettingsAttribute"))
            ?.ToString();

        appSettingsAttributeSource.ShouldNotBeNull();
        appSettingsAttributeSource.ShouldContain("class AppSettingsAttribute");
        appSettingsAttributeSource.ShouldContain("public string Key { get; }");
        appSettingsAttributeSource.ShouldContain("public bool Required { get; set; }");
        appSettingsAttributeSource.ShouldContain("public string? DefaultValue { get; set; }");
    }

    #endregion

    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        PortableExecutableReference[] references = new[]
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
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation? outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

        return (outputCompilation, diagnostics);
    }
}
