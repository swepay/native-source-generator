namespace Native.SourceGenerator.Configuration.Models;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

/// <summary>
/// The source of configuration value.
/// </summary>
internal enum ConfigurationSource
{
    /// <summary>
    /// Value comes from environment variable.
    /// </summary>
    EnvironmentVariable,

    /// <summary>
    /// Value comes from appsettings.json (IConfiguration section/key).
    /// </summary>
    AppSettings
}

internal readonly record struct ConfigurationClassInfo(
    string Namespace,
    string ClassName,
    ImmutableArray<ConfigurationFieldInfo> Fields,
    Location Location);

internal readonly record struct ConfigurationFieldInfo(
    string FieldName,
    string FieldType,
    string FullyQualifiedFieldType,
    string ConfigurationKey,
    bool IsRequired,
    string? DefaultValue,
    ConfigurationSource Source,
    Location Location);
