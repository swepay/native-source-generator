namespace Native.SourceGenerator.Configuration.Diagnostics;

using Microsoft.CodeAnalysis;

internal static class DiagnosticDescriptors
{
    private const string Category = "Native.Configuration";

    public static readonly DiagnosticDescriptor CONFIG001_ClassMustBePartial = new(
        id: "CONFIG001",
        title: "Class must be partial",
        messageFormat: "Class '{0}' must be declared as partial to use [EnvironmentConfig] or [AppSettings] attribute on fields",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CONFIG002_UnsupportedFieldType = new(
        id: "CONFIG002",
        title: "Unsupported field type",
        messageFormat: "Field '{0}' has unsupported type '{1}' for configuration. Supported types: string, int, long, bool, double, decimal, TimeSpan, Uri",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CONFIG003_FieldMustNotBeStatic = new(
        id: "CONFIG003",
        title: "Field must not be static",
        messageFormat: "Field '{0}' marked with [EnvironmentConfig] or [AppSettings] must not be static",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CONFIG004_DuplicateConfigurationKey = new(
        id: "CONFIG004",
        title: "Duplicate configuration key",
        messageFormat: "Configuration key '{0}' is already mapped to another field",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CONFIG005_EmptyConfigurationKey = new(
        id: "CONFIG005",
        title: "Empty configuration key",
        messageFormat: "Configuration key cannot be empty or whitespace",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
