
# Native Source Generators – Master Prompt (.NET 10 + Native AOT)

Este documento define o **prompt oficial** para a criação das bibliotecas:

- Native.SourceGenerator.DependencyInjection
- Native.SourceGenerator.Configuration
- NativeMediator.SourceGenerator.DependencyInjection
- NativeFluentValidator.SourceGenerator.DependencyInjection

Todas **AOT-first**, sem reflection, sem runtime scanning.

---

## Base arquitetural

As bibliotecas **NativeMediator** e **NativeFluentValidator** **devem ser construídas sobre as abstrações e utilitários**
de:

- Native.SourceGenerator.DependencyInjection
- Native.SourceGenerator.Configuration

Reuso obrigatório de:
- Attribute parsing
- Diagnostic helpers
- Type parsing
- Constructor generation
- Service registration emission

---

## 1. Native.SourceGenerator.DependencyInjection

### Antes

```csharp
services.AddSingleton<IJwtService>(sp =>
    new JwtService(
        sp.GetRequiredService<ISigningKeyRepository>(),
        sp.GetRequiredService<IClockService>(),
        sp.GetRequiredService<IIdGenerator>(),
        sp.GetRequiredService<ILogger<JwtService>>(),
        issuerBaseUrl));
```

### Depois

```csharp
[Register(typeof(IJwtService), ServiceLifetime.Singleton)]
public partial sealed class JwtService : IJwtService
{
    [Inject] private readonly ISigningKeyRepository _signingKeyRepository;
    [Inject] private readonly IClockService _clockService;
    [Inject] private readonly IIdGenerator _idGenerator;
    [Inject] private readonly ILogger<JwtService> _logger;
}
```

### Código gerado

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

public static class NativeGeneratedServices
{
    public static void AddGeneratedServices(this IServiceCollection services)
    {
        services.AddSingleton<IJwtService, JwtService>();
    }
}
```

---

## 2. Native.SourceGenerator.Configuration

### Antes

```csharp
var issuer = Environment.GetEnvironmentVariable("ISSUER_BASE_URL") ?? throw new Exception();
```

### Depois

```csharp
public partial class DatabaseService
{
    [EnvironmentConfig("ISSUER_BASE_URL")]
    private readonly string issuerBaseUrl;
}
```

### Código gerado

```csharp
partial class DatabaseService
{
    private void __InjectConfiguration()
    {
        issuerBaseUrl =
            Environment.GetEnvironmentVariable("ISSUER_BASE_URL")
            ?? throw new InvalidOperationException("Missing config: ISSUER_BASE_URL");
    }
}
```

---

## 3. NativeMediator.SourceGenerator.DependencyInjection

(Baseado em Native.SourceGenerator.DependencyInjection)

### Antes

```csharp
options.AddHandler<LogoutCommand, LogoutCommandResponse, LogoutCommandHandler>();
```

### Depois

```csharp
[Register(typeof(RequestHandler<LogoutCommand, LogoutCommandResponse>),
          ServiceLifetime.Singleton,
          Group = "AuthenticationHandlers")]
public partial sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand, LogoutCommandResponse>
{
    [Inject] private readonly IRealmRepository _realmRepository;
    [Inject] private readonly ILogger<LogoutCommandHandler> _logger;
}
```

### Código gerado

```csharp
public static class NativeMediatorAuthenticationHandlersExtensions
{
    public static void AddAuthenticationHandlers(this INativeMediatorOptions options)
    {
        options.AddHandler<LogoutCommand, LogoutCommandResponse, LogoutCommandHandler>(ServiceLifetime.Singleton);
    }
}
```

---

## 4. NativeFluentValidator.SourceGenerator.DependencyInjection

(Baseado em Native.SourceGenerator.DependencyInjection)

### Antes

```csharp
builder.AddValidator<CreateUserRequest, CreateUserRequestValidator>();
```

### Depois

```csharp
[Register(typeof(NativeValidator<CreateUserRequest>),
          ServiceLifetime.Singleton,
          Group = "Validations")]
public sealed partial class CreateUserRequestValidator
    : NativeValidator<CreateUserRequest>
{
}
```

### Código gerado

```csharp
public static class NativeFluentValidationGeneratedExtensions
{
    public static void AddValidations(this INativeFluentValidationBuilder builder)
    {
        builder.AddValidator<CreateUserRequest, CreateUserRequestValidator>();
    }
}
```

---

## CI/CD

O pipeline **deve ser baseado** no arquivo:

```
C:\Users\Alex\source\repos\swepay\native-open-api\.github\workflows\dotnet.yml
```

Requisitos:
- build
- test
- pack
- publish NuGet
- Native AOT compatibility

---

## Estrutura do Repositório

```
.github/
 └─ workflows/
    └─ dotnet.yml

src/
 ├─ NativeMediator.SourceGenerator.DependencyInjection/
 ├─ NativeFluentValidator.SourceGenerator.DependencyInjection/
 ├─ Native.SourceGenerator.DependencyInjection/
 └─ Native.SourceGenerator.Configuration/

tests/
 ├─ NativeMediator.SourceGenerator.DependencyInjection.Tests/
 ├─ NativeFluentValidator.SourceGenerator.DependencyInjection.Tests/
 ├─ Native.SourceGenerator.DependencyInjection.Tests/
 └─ Native.SourceGenerator.Configuration.Tests/

Native.SourceGenerators.slnx
README.md
```

---

Fim do prompt.
