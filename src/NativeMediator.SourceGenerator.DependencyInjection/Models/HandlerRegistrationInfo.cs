namespace NativeMediator.SourceGenerator.DependencyInjection.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

internal readonly record struct HandlerRegistrationInfo(
    string Namespace,
    string ClassName,
    string FullyQualifiedClassName,
    string RequestType,
    string FullyQualifiedRequestType,
    string ResponseType,
    string FullyQualifiedResponseType,
    string Lifetime,
    string? Group,
    ImmutableArray<InjectableFieldInfo> InjectableFields,
    Location Location);

internal readonly record struct InjectableFieldInfo(
    string FieldName,
    string FieldType,
    string FullyQualifiedFieldType,
    string ParameterName);
