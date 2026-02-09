namespace Native.SourceGenerator.DependencyInjection.Diagnostics;

using Microsoft.CodeAnalysis;

internal static class DiagnosticDescriptors
{
    private const string Category = "Native.DependencyInjection";

    public static readonly DiagnosticDescriptor DI001_ClassMustBePartial = new(
        id: "DI001",
        title: "Class must be partial",
        messageFormat: "Class '{0}' must be declared as partial to use [Register] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DI002_NoInjectableFields = new(
        id: "DI002",
        title: "No injectable fields found",
        messageFormat: "Class '{0}' has [Register] attribute but no fields marked with [Inject]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DI003_FieldMustBeReadonly = new(
        id: "DI003",
        title: "Injectable field must be readonly",
        messageFormat: "Field '{0}' marked with [Inject] must be readonly",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DI004_InvalidServiceType = new(
        id: "DI004",
        title: "Invalid service type",
        messageFormat: "Service type '{0}' is not implemented by class '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DI005_ClassMustNotBeStatic = new(
        id: "DI005",
        title: "Class must not be static",
        messageFormat: "Class '{0}' cannot be static when using [Register] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DI006_ClassMustNotBeAbstract = new(
        id: "DI006",
        title: "Class must not be abstract",
        messageFormat: "Class '{0}' cannot be abstract when using [Register] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DI007_DuplicateConstructor = new(
        id: "DI007",
        title: "Duplicate constructor",
        messageFormat: "Class '{0}' already has a constructor with the same signature that would be generated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
