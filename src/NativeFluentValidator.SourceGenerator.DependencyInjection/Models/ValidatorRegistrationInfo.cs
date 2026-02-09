namespace NativeFluentValidator.SourceGenerator.DependencyInjection.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

internal readonly record struct ValidatorRegistrationInfo(
    string Namespace,
    string ClassName,
    string FullyQualifiedClassName,
    string ValidatedType,
    string FullyQualifiedValidatedType,
    string Lifetime,
    string? Group,
    ImmutableArray<InjectableFieldInfo> InjectableFields,
    Location Location);

internal readonly record struct InjectableFieldInfo(
    string FieldName,
    string FieldType,
    string FullyQualifiedFieldType,
    string ParameterName);
