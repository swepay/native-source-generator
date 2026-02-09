namespace Native.SourceGenerator.DependencyInjection;

using System;

/// <summary>
/// Marks a class for automatic service registration and constructor generation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RegisterAttribute : Attribute
{
    /// <summary>
    /// The service type to register. If null, the class itself is registered.
    /// </summary>
    public Type? ServiceType { get; }

    /// <summary>
    /// The service lifetime.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Optional group name for organizing service registrations.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Registers the class as itself with Singleton lifetime.
    /// </summary>
    public RegisterAttribute()
    {
        ServiceType = null;
        Lifetime = ServiceLifetime.Singleton;
    }

    /// <summary>
    /// Registers the class as the specified service type with Singleton lifetime.
    /// </summary>
    /// <param name="serviceType">The service type to register.</param>
    public RegisterAttribute(Type serviceType)
    {
        ServiceType = serviceType;
        Lifetime = ServiceLifetime.Singleton;
    }

    /// <summary>
    /// Registers the class as the specified service type with the specified lifetime.
    /// </summary>
    /// <param name="serviceType">The service type to register.</param>
    /// <param name="lifetime">The service lifetime.</param>
    public RegisterAttribute(Type serviceType, ServiceLifetime lifetime)
    {
        ServiceType = serviceType;
        Lifetime = lifetime;
    }
}

/// <summary>
/// Service lifetime options.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// A single instance is created and shared.
    /// </summary>
    Singleton = 0,

    /// <summary>
    /// A new instance is created for each scope.
    /// </summary>
    Scoped = 1,

    /// <summary>
    /// A new instance is created each time it is requested.
    /// </summary>
    Transient = 2
}
