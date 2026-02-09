namespace Native.SourceGenerator.Configuration.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

internal readonly record struct ConfigurationClassInfo(
    string Namespace,
    string ClassName,
    ImmutableArray<ConfigurationFieldInfo> Fields,
    Location Location);

internal readonly record struct ConfigurationFieldInfo(
    string FieldName,
    string FieldType,
    string FullyQualifiedFieldType,
    string EnvironmentVariableName,
    bool IsRequired,
    string? DefaultValue,
    Location Location);
