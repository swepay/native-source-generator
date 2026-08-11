# native-source-generator

**Pacotes (v1.0.3):**
- Native.SourceGenerator.DependencyInjection
- Native.SourceGenerator.Configuration
- NativeMediator.SourceGenerator.DependencyInjection
- NativeFluentValidator.SourceGenerator.DependencyInjection

**Tipo:** Roslyn Source Generators - DI & Configuration  
**AOT-Safe:** Sim (compile-time code generation)  
**Linguagem:** C# 12+

## O que é

Quatro source generators que geram código em compile-time para:
1. **Dependency Injection:** Registrar serviços via `[Register]` attribute
2. **Configuration:** Bind environment variables/appsettings via `[EnvironmentConfig]`
3. **NativeMediator:** Auto-registrar handlers/behaviors em `IMediator`
4. **NativeFluentValidator:** Auto-registrar validators em `INativeValidator`

**Sem reflection scanning** - Tudo é gerado no build.

## 4 Pacotes & Responsabilidades

### 1. Native.SourceGenerator.DependencyInjection

Registra serviços via `[Register]` attribute em classes `partial`.

```csharp
[Register(typeof(IUserService), typeof(UserService), ServiceLifetime.Singleton)]
public partial class UserService : IUserService
{
    public async Task<User> GetUserAsync(string id) => ...
}

// Gera:
// services.AddSingleton<IUserService, UserService>();
```

**Lifecycle:** `Singleton`, `Transient`, `Scoped`

**Group:** Agrupar registros para seletivamente habilitar/desabilitar.

```csharp
[Register(typeof(IFeatureA), typeof(FeatureA), ServiceLifetime.Transient, Group = "FeatureA")]
public partial class FeatureA : IFeatureA { }

// Registrar seletivamente:
services.AddGeneratedServices(group: "FeatureA");
```

### 2. Native.SourceGenerator.Configuration

Bind environment variables e appsettings via `[EnvironmentConfig]` e `[AppSettings]`.

```csharp
public partial class AppConfig
{
    [EnvironmentConfig("DATABASE_URL")]
    public string DatabaseUrl { get; set; }
    
    [EnvironmentConfig("LOG_LEVEL", DefaultValue = "Information")]
    public string LogLevel { get; set; }
    
    [AppSettings("Jwt:Secret")]
    public string JwtSecret { get; set; }
}

// Gera configuração type-safe em compile-time
var config = new AppConfig
{
    DatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL"),
    LogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information",
    JwtSecret = appsettings["Jwt:Secret"]
};
```

### 3. NativeMediator.SourceGenerator.DependencyInjection

Auto-registra handlers `IRequestHandler<,>` e behaviors `IPipelineBehavior<,>`.

```csharp
public record CreateUserCommand(string Name) : IRequest<CreateUserResponse>;

public partial class CreateUserCommandHandler 
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    public async ValueTask<CreateUserResponse> Handle(
        CreateUserCommand request, 
        CancellationToken cancellationToken) => ...
}

// Gera:
// services.AddSingleton<IRequestHandler<CreateUserCommand, CreateUserResponse>,
//     CreateUserCommandHandler>();
```

### 4. NativeFluentValidator.SourceGenerator.DependencyInjection

Auto-registra validators `INativeValidator<T>`.

```csharp
public partial class CreateUserCommandValidator 
    : NativeValidator<CreateUserCommand>
{
    public override async ValueTask<ValidationResult> ValidateAsync(...) => ...
}

// Gera:
// services.AddSingleton<INativeValidator<CreateUserCommand>,
//     CreateUserCommandValidator>();
```

## API Pública Principal

### [Register]
Marca classe para ser registrada em DI.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class RegisterAttribute : Attribute
{
    public RegisterAttribute(
        Type interfaceType,
        Type implementationType,
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        string group = null);
}

public enum ServiceLifetime
{
    Transient = 0,
    Scoped = 1,
    Singleton = 2
}
```

### [Inject]
Marca campo readonly para injeção de dependência.

```csharp
[Register(...)]
public partial class MyService
{
    [Inject]
    private readonly IUserRepository _userRepository;
    
    [Inject]
    private readonly ILogger<MyService> _logger;
    
    // Gera ctor:
    // public MyService(IUserRepository userRepository, ILogger<MyService> logger)
    // {
    //     _userRepository = userRepository;
    //     _logger = logger;
    // }
}
```

### [EnvironmentConfig]
Marca propriedade para bind de env var.

```csharp
[EnvironmentConfig("DATABASE_URL", DefaultValue = "localhost")]
public string DatabaseUrl { get; set; }
```

### [AppSettings]
Marca propriedade para bind de appsettings.json.

```csharp
[AppSettings("Jwt:Secret")]
public string JwtSecret { get; set; }
```

## Como Usar

### Passo 1: Marcar Classes com [Register]

```csharp
// Services/UserService.cs
[Register(typeof(IUserService), typeof(UserService), ServiceLifetime.Singleton)]
public partial class UserService : IUserService
{
    [Inject]
    private readonly IUserRepository _repository;
    
    [Inject]
    private readonly ILogger<UserService> _logger;

    public async Task<User> GetUserAsync(string id)
    {
        _logger.LogInformation("Getting user {Id}", id);
        return await _repository.GetAsync(id);
    }
}

// Repositories/UserRepository.cs
[Register(typeof(IUserRepository), typeof(UserRepository), ServiceLifetime.Singleton)]
public partial class UserRepository : IUserRepository
{
    [Inject]
    private readonly IDbConnection _connection;

    public async Task<User> GetAsync(string id)
    {
        return await _connection.QueryAsync<User>("SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }
}
```

### Passo 2: Gerar Extension Method

Após build, o source generator cria:

```csharp
// Generated/ServiceCollectionExtensions.cs (gerado automaticamente)
namespace MyProject.Generated
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGeneratedServices(
            this IServiceCollection services,
            string group = null)
        {
            if (string.IsNullOrEmpty(group) || group == "Default")
            {
                services.AddSingleton<IUserService, UserService>();
                services.AddSingleton<IUserRepository, UserRepository>();
            }

            return services;
        }
    }
}
```

### Passo 3: Registrar no Startup

```csharp
// Program.cs
var services = new ServiceCollection();

// Registrar todos os serviços gerados
services.AddGeneratedServices();

// Ou registrar apenas um grupo
services.AddGeneratedServices(group: "SpecialFeature");

var provider = services.BuildServiceProvider();
```

### Passo 4: Usar DI

```csharp
var userService = provider.GetRequiredService<IUserService>();
var user = await userService.GetUserAsync("user-123");
```

### Passo 5: Configuration com EnvironmentConfig

```csharp
public partial class AppSettings
{
    [EnvironmentConfig("ASPNETCORE_ENVIRONMENT")]
    public string Environment { get; set; }

    [EnvironmentConfig("DATABASE_URL")]
    public string DatabaseUrl { get; set; }

    [EnvironmentConfig("API_KEY")]
    public string ApiKey { get; set; }

    [EnvironmentConfig("LOG_LEVEL", DefaultValue = "Information")]
    public string LogLevel { get; set; }

    [AppSettings("Jwt:Secret")]
    public string JwtSecret { get; set; }

    [AppSettings("Jwt:Issuer")]
    public string JwtIssuer { get; set; }
}

// Gerado:
var settings = new AppSettings
{
    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    DatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL"),
    ApiKey = Environment.GetEnvironmentVariable("API_KEY"),
    LogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information",
    JwtSecret = appsettings["Jwt:Secret"],
    JwtIssuer = appsettings["Jwt:Issuer"]
};
```

## Diagnósticos Compile-Time

O source generator gera diagnósticos se houver erros:

| Code | Descrição | Exemplo |
|------|-----------|---------|
| DI001 | Classe não é `partial` | `[Register] public class MyService` (falta `partial`) |
| DI002 | Tipo de interface não encontrado | `[Register(typeof(NoExist), ...)]` |
| DI003 | Campo `[Inject]` não é `private readonly` | `[Inject] public string Property` |
| DI004 | Tipo injected não está registrado | `[Inject] private readonly INotRegistered _dep;` |
| DI005 | Cíclica dependency | Class A -> B -> A |
| DI006 | Lifetime incompatível | Singleton depende de Transient |
| CONFIG001 | `[EnvironmentConfig]` sem DefaultValue em valor type | `[EnvironmentConfig("VAR")] public int Count` |
| CONFIG002 | Property não tem setter | `[EnvironmentConfig("VAR")] public string Value { get; }` |

## Premissas

- **Classe `partial` obrigatória:** Source generator modifica via `partial`
- **Atributos ao nível de classe:** `[Register]` no início da classe
- **Campos readonly com `[Inject]`:** Apenas `private readonly`
- **Sem reflexão:** Tudo resolve em compile-time
- **Namespace:** `Native.SourceGenerator.*`
- **Target:** `net8.0`; PublishAot = true

## Workflow Completo

```
1. Marcar classe com [Register]
2. Marcar deps com [Inject]
3. dotnet build
4. Source generator gera ServiceCollectionExtensions.cs
5. Import gerado e chamar AddGeneratedServices()
6. DI automático via ctor injection
```

## Limitações

- Sem registração runtime (tudo é compile-time)
- Sem scanning automático (registro explícito obrigatório)
- `[Inject]` apenas em `private readonly` fields (não properties)
- Não suporta factory delegates (use ServiceProvider.GetRequiredService)
- Não suporta open generics (registre cada tipo concreto)
