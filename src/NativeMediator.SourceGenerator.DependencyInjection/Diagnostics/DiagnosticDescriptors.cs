namespace NativeMediator.SourceGenerator.DependencyInjection.Diagnostics;

using Microsoft.CodeAnalysis;

internal static class DiagnosticDescriptors
{
    private const string Category = "NativeMediator.DependencyInjection";

    public static readonly DiagnosticDescriptor MED001_ClassMustBePartial = new(
        id: "MED001",
        title: "Handler class must be partial",
        messageFormat: "Handler class '{0}' must be declared as partial to use [Register] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MED002_MustImplementIRequestHandler = new(
        id: "MED002",
        title: "Must implement IRequestHandler",
        messageFormat: "Class '{0}' must implement IRequestHandler<TRequest, TResponse> to be registered as a mediator handler",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MED003_NoInjectableFields = new(
        id: "MED003",
        title: "No injectable fields found",
        messageFormat: "Handler class '{0}' has [Register] attribute but no fields marked with [Inject]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MED004_InvalidServiceType = new(
        id: "MED004",
        title: "Invalid service type for handler",
        messageFormat: "Service type '{0}' must be RequestHandler<TRequest, TResponse> for mediator handlers",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MED005_GroupRequired = new(
        id: "MED005",
        title: "Group is required for handler registration",
        messageFormat: "Handler class '{0}' should specify a Group in the [Register] attribute for organized registration",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
