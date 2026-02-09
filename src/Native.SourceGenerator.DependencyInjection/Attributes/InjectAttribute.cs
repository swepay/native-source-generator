namespace Native.SourceGenerator.DependencyInjection;

using System;

/// <summary>
/// Marks a field for dependency injection via constructor generation.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class InjectAttribute : Attribute
{
}
