namespace Native.SourceGenerator.Configuration.Parsing;

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Native.SourceGenerator.Configuration.Models;

internal static class ConfigurationParser
{
    public const string EnvironmentConfigAttributeFullName = "Native.SourceGenerator.Configuration.EnvironmentConfigAttribute";

    public static bool HasEnvironmentConfigFields(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        foreach (var member in classDeclaration.Members)
        {
            if (member is not FieldDeclarationSyntax fieldDeclaration)
            {
                continue;
            }

            foreach (var attributeList in fieldDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (name is "EnvironmentConfig" or "EnvironmentConfigAttribute")
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static ConfigurationClassInfo? ParseConfigurationClass(
        GeneratorSyntaxContext context,
        CancellationToken cancellationToken)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        var fields = ParseConfigurationFields(classSymbol);
        if (fields.IsEmpty)
        {
            return null;
        }

        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new ConfigurationClassInfo(
            Namespace: namespaceName,
            ClassName: classSymbol.Name,
            Fields: fields,
            Location: classDeclaration.Identifier.GetLocation());
    }

    private static ImmutableArray<ConfigurationFieldInfo> ParseConfigurationFields(INamedTypeSymbol classSymbol)
    {
        var builder = ImmutableArray.CreateBuilder<ConfigurationFieldInfo>();

        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol)
            {
                continue;
            }

            AttributeData? configAttribute = null;
            foreach (var attribute in fieldSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == EnvironmentConfigAttributeFullName)
                {
                    configAttribute = attribute;
                    break;
                }
            }

            if (configAttribute is null)
            {
                continue;
            }

            var environmentVariableName = string.Empty;
            var isRequired = true;
            string? defaultValue = null;

            // Get environment variable name from constructor argument
            if (configAttribute.ConstructorArguments.Length > 0 &&
                configAttribute.ConstructorArguments[0].Value is string envVarName)
            {
                environmentVariableName = envVarName;
            }

            // Get named arguments
            foreach (var namedArg in configAttribute.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Required" when namedArg.Value.Value is bool requiredValue:
                        isRequired = requiredValue;
                        break;
                    case "DefaultValue" when namedArg.Value.Value is string defaultVal:
                        defaultValue = defaultVal;
                        break;
                }
            }

            builder.Add(new ConfigurationFieldInfo(
                FieldName: fieldSymbol.Name,
                FieldType: fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                FullyQualifiedFieldType: fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                EnvironmentVariableName: environmentVariableName,
                IsRequired: isRequired,
                DefaultValue: defaultValue,
                Location: fieldSymbol.Locations[0]));
        }

        return builder.ToImmutable();
    }
}
