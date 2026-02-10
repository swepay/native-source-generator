namespace Native.SourceGenerator.DependencyInjection.Parsing;

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Native.SourceGenerator.DependencyInjection.Diagnostics;
using Native.SourceGenerator.DependencyInjection.Models;

internal static class AttributeParser
{
    public const string RegisterAttributeFullName = "Native.SourceGenerator.DependencyInjection.RegisterAttribute";
    public const string InjectAttributeFullName = "Native.SourceGenerator.DependencyInjection.InjectAttribute";

    public static bool HasRegisterAttribute(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name is "Register" or "RegisterAttribute")
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static ServiceRegistrationInfo? ParseServiceRegistration(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var registerAttributeData = GetRegisterAttributeData(classSymbol);
        if (registerAttributeData is null)
        {
            return null;
        }

        var (serviceType, lifetime, group) = ExtractAttributeValues(registerAttributeData);
        var injectableFields = GetInjectableFields(classSymbol);

        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ServiceRegistrationInfo(
            Namespace: namespaceName,
            ClassName: classSymbol.Name,
            FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ServiceTypeName: serviceType?.Name,
            FullyQualifiedServiceTypeName: serviceType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Lifetime: lifetime,
            Group: group,
            InjectableFields: injectableFields,
            Location: classDeclaration.Identifier.GetLocation());
    }

    private static AttributeData? GetRegisterAttributeData(INamedTypeSymbol classSymbol)
    {
        foreach (var attributeData in classSymbol.GetAttributes())
        {
            var attrClassName = attributeData.AttributeClass?.ToDisplayString();
            if (attrClassName == RegisterAttributeFullName ||
                attributeData.AttributeClass?.Name == "RegisterAttribute")
            {
                return attributeData;
            }
        }

        return null;
    }

    private static (INamedTypeSymbol? ServiceType, string Lifetime, string? Group) ExtractAttributeValues(
        AttributeData attributeData)
    {
        INamedTypeSymbol? serviceType = null;
        var lifetime = "Singleton";
        string? group = null;

        // Constructor arguments
        if (attributeData.ConstructorArguments.Length > 0)
        {
            if (attributeData.ConstructorArguments[0].Value is INamedTypeSymbol type)
            {
                serviceType = type;
            }
        }

        if (attributeData.ConstructorArguments.Length > 1)
        {
            if (attributeData.ConstructorArguments[1].Value is int lifetimeValue)
            {
                lifetime = lifetimeValue switch
                {
                    0 => "Singleton",
                    1 => "Scoped",
                    2 => "Transient",
                    _ => "Singleton"
                };
            }
        }

        // Named arguments
        foreach (var namedArg in attributeData.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Group" when namedArg.Value.Value is string groupValue:
                    group = groupValue;
                    break;
                case "Lifetime" when namedArg.Value.Value is int lifetimeValue:
                    lifetime = lifetimeValue switch
                    {
                        0 => "Singleton",
                        1 => "Scoped",
                        2 => "Transient",
                        _ => "Singleton"
                    };
                    break;
            }
        }

        return (serviceType, lifetime, group);
    }

    private static ImmutableArray<InjectableFieldInfo> GetInjectableFields(INamedTypeSymbol classSymbol)
    {
        var builder = ImmutableArray.CreateBuilder<InjectableFieldInfo>();

        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol)
            {
                continue;
            }

            var hasInjectAttribute = false;
            foreach (var attribute in fieldSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == InjectAttributeFullName)
                {
                    hasInjectAttribute = true;
                    break;
                }
            }

            if (!hasInjectAttribute)
            {
                continue;
            }

            var fieldName = fieldSymbol.Name;
            var parameterName = fieldName.TrimStart('_');
            if (parameterName.Length > 0)
            {
                parameterName = char.ToLowerInvariant(parameterName[0]) + parameterName.Substring(1);
            }

            builder.Add(new InjectableFieldInfo(
                FieldName: fieldName,
                FieldType: fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                FullyQualifiedFieldType: fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ParameterName: parameterName));
        }

        return builder.ToImmutable();
    }

    public static ImmutableArray<Diagnostic> ValidateServiceRegistration(
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol classSymbol,
        ServiceRegistrationInfo registrationInfo)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        // DI001: Must be partial
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.DI001_ClassMustBePartial,
                registrationInfo.Location,
                classSymbol.Name));
        }

        // DI005: Must not be static
        if (classSymbol.IsStatic)
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.DI005_ClassMustNotBeStatic,
                registrationInfo.Location,
                classSymbol.Name));
        }

        // DI006: Must not be abstract
        if (classSymbol.IsAbstract)
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.DI006_ClassMustNotBeAbstract,
                registrationInfo.Location,
                classSymbol.Name));
        }

        // DI002: Warn if no injectable fields
        if (registrationInfo.InjectableFields.IsEmpty)
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.DI002_NoInjectableFields,
                registrationInfo.Location,
                classSymbol.Name));
        }

        // DI003: Fields must be readonly
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol)
            {
                continue;
            }

            var hasInjectAttribute = false;
            foreach (var attribute in fieldSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == InjectAttributeFullName)
                {
                    hasInjectAttribute = true;
                    break;
                }
            }

            if (hasInjectAttribute && !fieldSymbol.IsReadOnly)
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.DI003_FieldMustBeReadonly,
                    fieldSymbol.Locations[0],
                    fieldSymbol.Name));
            }
        }

        return diagnostics.ToImmutable();
    }
}
