# Native Source Generators

[![.NET Build, Test & Publish](https://github.com/swepay/native-source-generator/actions/workflows/dotnet.yml/badge.svg)](https://github.com/swepay/native-source-generator/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/Native.SourceGenerator.DependencyInjection.svg)](https://www.nuget.org/packages/Native.SourceGenerator.DependencyInjection/)

**AOT-first Source Generators for .NET 10** - Zero reflection, compile-time code generation for dependency injection, configuration binding, mediator handlers, and validators.

## Features

- ✅ **100% Native AOT Compatible** - No reflection, no runtime scanning
- ✅ **Compile-time Code Generation** - All errors caught at build time
- ✅ **Zero Allocations** - Deterministic, explicit code
- ✅ **IIncrementalGenerator** - Fast, incremental builds

## Packages

| Package | Description |
|---------|-------------|
| `Native.SourceGenerator.DependencyInjection` | Constructor generation and service registration |
| `Native.SourceGenerator.Configuration` | Environment variable binding |
| `NativeMediator.SourceGenerator.DependencyInjection` | Mediator handler registration |
| `NativeFluentValidator.SourceGenerator.DependencyInjection` | Validator registration |

## Installation

```bash
dotnet add package Native.SourceGenerator.DependencyInjection
dotnet add package Native.SourceGenerator.Configuration
dotnet add package NativeMediator.SourceGenerator.DependencyInjection
dotnet add package NativeFluentValidator.SourceGenerator.DependencyInjection
```

## Usage

### 1. Dependency Injection

**Before** (manual registration):

```csharp
services.AddSingleton<IJwtService>(sp =>
    new JwtService(
        sp.GetRequiredService<ISigningKeyRepository>(),
        sp.GetRequiredService<IClockService>(),
        sp.GetRequiredService<IIdGenerator>(),
        sp.GetRequiredService<ILogger<JwtService>>()));
```

**After** (generated):

```csharp
[Register(typeof(IJwtService), ServiceLifetime.Singleton)]
public partial sealed class JwtService : IJwtService
{
    [Inject] private readonly ISigningKeyRepository _signingKeyRepository;
    [Inject] private readonly IClockService _clockService;
    [Inject] private readonly IIdGenerator _idGenerator;
    [Inject] private readonly ILogger<JwtService> _logger;
}

// In Startup:
services.AddGeneratedServices();
```

**Generated code:**

```csharp
public partial sealed class JwtService
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

### 2. Configuration Binding

**Before** (manual):

```csharp
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? throw new Exception("Missing DATABASE_URL");
```

**After** (generated):

```csharp
public partial class DatabaseService
{
    [EnvironmentConfig("DATABASE_URL")]
    private readonly string _connectionString;

    [EnvironmentConfig("MAX_CONNECTIONS", Required = false, DefaultValue = "100")]
    private readonly int _maxConnections;
}
```

**Generated code:**

```csharp
partial class DatabaseService
{
    private void __InjectConfiguration()
    {
        _connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? throw new InvalidOperationException("Missing required configuration: DATABASE_URL");

        _maxConnections = Environment.GetEnvironmentVariable("MAX_CONNECTIONS") is { Length: > 0 } __maxConnectionsValue
            ? int.Parse(__maxConnectionsValue)
            : int.Parse("100");
    }
}
```

### 3. Mediator Handlers

```csharp
[Register(typeof(RequestHandler<LogoutCommand, LogoutCommandResponse>),
          ServiceLifetime.Singleton,
          Group = "AuthenticationHandlers")]
public partial sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand, LogoutCommandResponse>
{
    [Inject] private readonly IRealmRepository _realmRepository;
    [Inject] private readonly ILogger<LogoutCommandHandler> _logger;

    public Task<LogoutCommandResponse> Handle(LogoutCommand request, CancellationToken ct) { ... }
}

// In Startup:
options.AddAuthenticationHandlers();
```

### 4. Validators

```csharp
[Register(typeof(NativeValidator<CreateUserRequest>),
          ServiceLifetime.Singleton,
          Group = "Validations")]
public sealed partial class CreateUserRequestValidator
    : NativeValidator<CreateUserRequest>
{
    public override ValidationResult Validate(CreateUserRequest request) { ... }
}

// In Startup:
builder.AddValidations();
```

## Diagnostics

All errors are reported at compile time:

| Code | Description |
|------|-------------|
| `DI001` | Class must be partial |
| `DI002` | No injectable fields found |
| `DI003` | Injectable field must be readonly |
| `DI005` | Class must not be static |
| `DI006` | Class must not be abstract |
| `CONFIG001` | Class must be partial |
| `CONFIG002` | Unsupported field type |
| `MED001` | Handler class must be partial |
| `MED002` | Must implement IRequestHandler |
| `VAL001` | Validator class must be partial |
| `VAL002` | Must inherit NativeValidator |

## Architecture

```
src/
├─ Native.SourceGenerator.DependencyInjection/    # Base DI generator
├─ Native.SourceGenerator.Configuration/          # Configuration generator
├─ NativeMediator.SourceGenerator.DependencyInjection/  # Built on base DI
└─ NativeFluentValidator.SourceGenerator.DependencyInjection/  # Built on base DI

tests/
├─ Native.SourceGenerator.DependencyInjection.Tests/
├─ Native.SourceGenerator.Configuration.Tests/
├─ NativeMediator.SourceGenerator.DependencyInjection.Tests/
└─ NativeFluentValidator.SourceGenerator.DependencyInjection.Tests/
```

## Requirements

- .NET 10 SDK
- C# 13+

## License

MIT
