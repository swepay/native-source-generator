namespace NativeMediator.SourceGenerator.DependencyInjection.Parsing;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NativeMediator.SourceGenerator.DependencyInjection.Models;

internal static class HandlerParser
{
    private const string MediatorHandlerAttributeName = "MediatorHandler";
    private const string MediatorHandlerAttributeFullName = "NativeMediator.SourceGenerator.DependencyInjection.MediatorHandlerAttribute";

    public static bool HasMediatorHandlerAttribute(SyntaxNode node, CancellationToken cancellationToken)
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
                if (name is MediatorHandlerAttributeName or "MediatorHandlerAttribute")
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static HandlerRegistrationInfo? ParseHandlerRegistration(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        SemanticModel semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        AttributeData? attributeData = GetMediatorHandlerAttributeData(classSymbol);
        if (attributeData is null)
        {
            return null;
        }

        (string? lifetime, string? group) = ExtractAttributeValues(attributeData);

        // Find HandleAsync method to extract TRequest and TResponse
        (ITypeSymbol? requestType, ITypeSymbol? responseType) = FindHandleAsyncTypes(classSymbol);
        if (requestType is null || responseType is null)
        {
            return null;
        }

        ImmutableArray<InjectableFieldInfo> injectableFields = GetInjectableFields(classSymbol);

        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new HandlerRegistrationInfo(
            Namespace: namespaceName,
            ClassName: classSymbol.Name,
            FullyQualifiedClassName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            RequestType: requestType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            FullyQualifiedRequestType: requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ResponseType: responseType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            FullyQualifiedResponseType: responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Lifetime: lifetime,
            Group: group,
            InjectableFields: injectableFields,
            Location: classDeclaration.Identifier.GetLocation());
    }

    private static (ITypeSymbol? RequestType, ITypeSymbol? ResponseType) FindHandleAsyncTypes(INamedTypeSymbol classSymbol)
    {
        foreach (ISymbol member in classSymbol.GetMembers())
        {
            if (member is IMethodSymbol method && method.Name == "HandleAsync")
            {
                // HandleAsync(TRequest request, CancellationToken ct) => Task<TResponse>
                if (method.Parameters.Length >= 1 && method.ReturnType is INamedTypeSymbol returnType)
                {
                    ITypeSymbol requestType = method.Parameters[0].Type;

                    // Extract TResponse from Task<TResponse>
                    ITypeSymbol? responseType = null;
                    if (returnType.IsGenericType && returnType.Name == "Task" && returnType.TypeArguments.Length == 1)
                    {
                        responseType = returnType.TypeArguments[0];
                    }
                    else if (returnType.IsGenericType && returnType.Name == "ValueTask" && returnType.TypeArguments.Length == 1)
                    {
                        responseType = returnType.TypeArguments[0];
                    }

                    if (responseType != null)
                    {
                        return (requestType, responseType);
                    }
                }
            }
        }

        return (null, null);
    }

    private static AttributeData? GetMediatorHandlerAttributeData(INamedTypeSymbol classSymbol)
    {
        foreach (AttributeData attributeData in classSymbol.GetAttributes())
        {
            var attrName = attributeData.AttributeClass?.ToDisplayString();
            if (attrName == MediatorHandlerAttributeFullName)
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
