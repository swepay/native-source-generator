namespace Native.SourceGenerator.Configuration.Parsing;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Native.SourceGenerator.Configuration.Models;

internal static class ConfigurationParser
{
    public const string EnvironmentConfigAttributeFullName = "Native.SourceGenerator.Configuration.EnvironmentConfigAttribute";
    public const string AppSettingsAttributeFullName = "Native.SourceGenerator.Configuration.AppSettingsAttribute";

    public static bool HasEnvironmentConfigFields(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        foreach (MemberDeclarationSyntax member in classDeclaration.Members)
        {
            if (member is not FieldDeclarationSyntax fieldDeclaration)
            {
                continue;
            }

            foreach (AttributeListSyntax attributeList in fieldDeclaration.AttributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (name is "EnvironmentConfig" or "EnvironmentConfigAttribute"
                             or "AppSettings" or "AppSettingsAttribute")
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
        SemanticModel semanticModel = context.SemanticModel;

        if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        ImmutableArray<ConfigurationFieldInfo> fields = ParseConfigurationFields(classSymbol);
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
        ImmutableArray<ConfigurationFieldInfo>.Builder builder = ImmutableArray.CreateBuilder<ConfigurationFieldInfo>();

        foreach (ISymbol member in classSymbol.GetMembers())
        {
            if (member is not IFieldSymbol fieldSymbol)
            {
                continue;
            }

            AttributeData? configAttribute = null;
            ConfigurationSource source = ConfigurationSource.EnvironmentVariable;

            foreach (AttributeData attribute in fieldSymbol.GetAttributes())
            {
                var attrName = attribute.AttributeClass?.ToDisplayString();
                if (attrName == EnvironmentConfigAttributeFullName)
                {
                    configAttribute = attribute;
                    source = ConfigurationSource.EnvironmentVariable;
                    break;
                }

                if (attrName == AppSettingsAttributeFullName)
                {
                    configAttribute = attribute;
                    source = ConfigurationSource.AppSettings;
                    break;
                }
            }

            if (configAttribute is null)
            {
                continue;
            }

            var configurationKey = string.Empty;
            var isRequired = true;
            string? defaultValue = null;

            // Get configuration key from constructor argument
            if (configAttribute.ConstructorArguments.Length > 0 &&
                configAttribute.ConstructorArguments[0].Value is string keyValue)
            {
                configurationKey = keyValue;
            }

            // Get named arguments
            foreach (KeyValuePair<string, TypedConstant> namedArg in configAttribute.NamedArguments)
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
                ConfigurationKey: configurationKey,
                IsRequired: isRequired,
                DefaultValue: defaultValue,
                Source: source,
                Location: fieldSymbol.Locations[0]));
        }

        return builder.ToImmutable();
    }
}
