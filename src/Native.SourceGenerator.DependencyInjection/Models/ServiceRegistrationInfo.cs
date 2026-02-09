namespace Native.SourceGenerator.DependencyInjection.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

internal readonly record struct ServiceRegistrationInfo(
    string Namespace,
    string ClassName,
    string FullyQualifiedClassName,
    string? ServiceTypeName,
    string? FullyQualifiedServiceTypeName,
    string Lifetime,
    string? Group,
    ImmutableArray<InjectableFieldInfo> InjectableFields,
    Location Location);

internal readonly record struct InjectableFieldInfo(
    string FieldName,
    string FieldType,
    string FullyQualifiedFieldType,
    string ParameterName);
