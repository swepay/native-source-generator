# Native.SourceGenerator.DependencyInjection

[![NuGet](https://img.shields.io/nuget/v/Native.SourceGenerator.DependencyInjection.svg)](https://www.nuget.org/packages/Native.SourceGenerator.DependencyInjection/)

**AOT-first Source Generator for Dependency Injection** - Generates constructors and service registrations without reflection.

## Features

- ✅ **100% Native AOT Compatible** - No reflection, no `Activator.CreateInstance`
- ✅ **Compile-time Constructor Generation** - All errors caught at build time
- ✅ **Automatic Service Registration** - No manual `services.AddSingleton<T>()` calls
- ✅ **IIncrementalGenerator** - Fast, incremental builds

## Installation

```bash
dotnet add package Native.SourceGenerator.DependencyInjection
```

## Usage

### 1. Mark your class with `[Register]` and fields with `[Inject]`

```csharp
using Native.SourceGenerator.DependencyInjection;

[Register(typeof(IJwtService), ServiceLifetime.Singleton)]
public partial class JwtService : IJwtService
{
    [Inject] private readonly ISigningKeyRepository _signingKeyRepository;
    [Inject] private readonly IClockService _clockService;
    [Inject] private readonly IIdGenerator _idGenerator;
    [Inject] private readonly ILogger<JwtService> _logger;

    public string GenerateToken(User user) { /* ... */ }
}
```

### 2. Register services in Startup

```csharp
var services = new ServiceCollection();
services.AddGeneratedServices();
```

### Generated Code

The generator creates a constructor for your class:

```csharp
// Auto-generated
public partial class JwtService
{
    public JwtService(
        ISigningKeyRepository signingKeyRepository,
        IClockService clockService,
        IIdGenerator idGenerator,
        ILogger<JwtService> logger)
    {
        _signingKeyRepository = signingKeyRepository;
        _clockService = clockService;
        _idGenerator = idGenerator;
        _logger = logger;
    }
}
```

And a registration extension method:

```csharp
// Auto-generated
public static class NativeGeneratedServices
{
    public static IServiceCollection AddGeneratedServices(this IServiceCollection services)
    {
        services.AddSingleton<IJwtService, JwtService>();
        return services;
    }
}
```

## Attributes

### `[Register]`

| Parameter | Type | Description |
|-----------|------|-------------|
| `serviceType` | `Type` | The interface/base type to register (optional) |
| `lifetime` | `ServiceLifetime` | `Singleton`, `Scoped`, or `Transient` (default: `Singleton`) |
| `Group` | `string` | Group name for organizing registrations (optional) |

### `[Inject]`

Marks a field for dependency injection. The field must be `readonly`.

## Grouping Services

```csharp
[Register(typeof(IAuthService), Group = "Authentication")]
public partial class AuthService : IAuthService { /* ... */ }

[Register(typeof(ITokenService), Group = "Authentication")]
public partial class TokenService : ITokenService { /* ... */ }

// In Startup:
services.AddAuthentication(); // Only adds services in "Authentication" group
```

## Diagnostics

| Code | Description |
|------|-------------|
| `DI001` | Class must be partial |
| `DI002` | No injectable fields found |
| `DI003` | Injectable field must be readonly |
| `DI005` | Class must not be static |
| `DI006` | Class must not be abstract |

## Requirements

- .NET 10 SDK
- C# 13+

## License

MIT
