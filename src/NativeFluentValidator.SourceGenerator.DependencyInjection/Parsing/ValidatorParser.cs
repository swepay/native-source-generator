namespace NativeFluentValidator.SourceGenerator.DependencyInjection.Parsing;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NativeFluentValidator.SourceGenerator.DependencyInjection.Models;

internal static class ValidatorParser
{
    private const string NativeValidatorAttributeName = "NativeValidator";
    private const string NativeValidatorAttributeFullName = "NativeFluentValidator.SourceGenerator.DependencyInjection.NativeValidatorAttribute";

    public static bool HasNativeValidatorAttribute(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
        {
            foreach (AttributeSyntax attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name is NativeValidatorAttributeName or "NativeValidatorAttribute")
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static ValidatorRegistrationInfo? ParseValidatorRegistration(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        AttributeData? attributeData = GetNativeValidatorAttributeData(classSymbol);
        if (attributeData is null)
        {
            return null;
        }

        (string? lifetime, string? group) = ExtractAttributeValues(attributeData);

        // Find the validated type from base class or interface
        ITypeSymbol? validatedType = FindValidatedType(classSymbol);
        if (validatedType is null)
        {
            // Fall back to using the class name to infer validated type
            // e.g., UserValidator -> User
            var className = classSymbol.Name;
            if (className.EndsWith("Validator"))
            {
                var typeName = className.Substring(0, className.Length - "Validator".Length);
                // Return with the inferred name
                return CreateRegistrationInfo(classDeclaration, classSymbol, typeName, typeName, lifetime, group);
            }
            return null;
        }

        ImmutableArray<InjectableFieldInfo> injectableFields = GetInjectableFields(classSymbol);

        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ValidatorRegistrationInfo(
            Namespace: namespaceName,
            ClassName: classSymbol.Name,
            FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ValidatedType: validatedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            FullyQualifiedValidatedType: validatedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Lifetime: lifetime,
            Group: group,
            InjectableFields: injectableFields,
            Location: classDeclaration.Identifier.GetLocation());
    }

    private static ValidatorRegistrationInfo CreateRegistrationInfo(
        ClassDeclarationSyntax classDeclaration,
        INamedTypeSymbol classSymbol,
        string validatedType,
        string fullyQualifiedValidatedType,
        string lifetime,
        string? group)
    {
        ImmutableArray<InjectableFieldInfo> injectableFields = GetInjectableFields(classSymbol);
        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ValidatorRegistrationInfo(
            Namespace: namespaceName,
            ClassName: classSymbol.Name,
            FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ValidatedType: validatedType,
            FullyQualifiedValidatedType: fullyQualifiedValidatedType,
            Lifetime: lifetime,
            Group: group,
            InjectableFields: injectableFields,
            Location: classDeclaration.Identifier.GetLocation());
    }

    private static ITypeSymbol? FindValidatedType(INamedTypeSymbol classSymbol)
    {
        // Check interfaces for IValidator<T> pattern
        foreach (INamedTypeSymbol iface in classSymbol.AllInterfaces)
        {
            if (iface.IsGenericType &&
                (iface.Name.Contains("Validator") || iface.Name == "IValidator") &&
                iface.TypeArguments.Length == 1)
            {
                return iface.TypeArguments[0];
            }
        }

        // Check base classes for AbstractValidator<T> or similar pattern
        INamedTypeSymbol? baseType = classSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType &&
                baseType.Name.Contains("Validator") &&
                baseType.TypeArguments.Length == 1)
            {
                return baseType.TypeArguments[0];
            }
            baseType = baseType.BaseType;
        }

        return null;
    }

    private static AttributeData? GetNativeValidatorAttributeData(INamedTypeSymbol classSymbol)
    {
        foreach (AttributeData attributeData in classSymbol.GetAttributes())
        {
            var attrName = attributeData.AttributeClass?.ToDisplayString();
            if (attrName == NativeValidatorAttributeFullName)
            {
                return attributeData;
            }
        }

        return null;
    }

    private static (string Lifetime, string? Group) ExtractAttributeValues(AttributeData attributeData)
    {
        var lifetime = "Scoped";
        string? group = null;

        // Named arguments
        foreach (KeyValuePair<string, TypedConstant> namedArg in attributeData.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Group" when namedArg.Value.Value is string groupValue:
                    group = groupValue;
                    break;
                case "Lifetime" when namedArg.Value.Value is int lv:
                    lifetime = lv switch
                    {
                        0 => "Singleton",
                        1 => "Scoped",
                        2 => "Transient",
                        _ => "Scoped"
                    };
                    break;
            }
        }

        return (lifetime, group);
    }

    private static ImmutableArray<InjectableFieldInfo> GetInjectableFields(INamedTypeSymbol classSymbol)
    {
        ImmutableArray<InjectableFieldInfo>.Builder builder = ImmutableArray.CreateBuilder<InjectableFieldInfo>();

        foreach (ISymbol member in classSymbol.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol)
            {
                continue;
            }

            // Check for [Inject] attribute - look for any attribute named "Inject"
            var hasInjectAttribute = false;
            foreach (AttributeData attribute in fieldSymbol.GetAttributes())
            {
                var attrName = attribute.AttributeClass?.Name;
                if (attrName is "InjectAttribute" or "Inject")
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
}
