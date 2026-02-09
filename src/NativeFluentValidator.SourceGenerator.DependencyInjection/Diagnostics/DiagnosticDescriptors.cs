namespace NativeFluentValidator.SourceGenerator.DependencyInjection.Diagnostics;

using Microsoft.CodeAnalysis;

internal static class DiagnosticDescriptors
{
    private const string Category = "NativeFluentValidator.DependencyInjection";

    public static readonly DiagnosticDescriptor VAL001_ClassMustBePartial = new(
        id: "VAL001",
        title: "Validator class must be partial",
        messageFormat: "Validator class '{0}' must be declared as partial to use [Register] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VAL002_MustInheritNativeValidator = new(
        id: "VAL002",
        title: "Must inherit NativeValidator",
        messageFormat: "Class '{0}' must inherit from NativeValidator<T> to be registered as a validator",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VAL003_InvalidServiceType = new(
        id: "VAL003",
        title: "Invalid service type for validator",
        messageFormat: "Service type '{0}' must be NativeValidator<T> for validator registration",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VAL004_GroupRequired = new(
        id: "VAL004",
        title: "Group is required for validator registration",
        messageFormat: "Validator class '{0}' should specify a Group in the [Register] attribute for organized registration",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VAL005_DuplicateValidatorForType = new(
        id: "VAL005",
        title: "Duplicate validator for type",
        messageFormat: "Multiple validators found for type '{0}': '{1}' and '{2}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
