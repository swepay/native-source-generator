---
name: developer
archetype: support-library
model: claude-sonnet-5
tools: [Read, Write, Edit, Bash, Grep, Glob]
description: >
  Implemente features seguindo o shared kernel e as convenções de código.
---

# Developer Agent - native-source-generator

**Modelo:** Claude Sonnet 4  
**Ferramentas:** read, write, bash, edit, grep, glob  
**Foco:** Usar source generators para DI, configuration, handlers & validators

## Responsabilidades

1. **Marcar serviços com `[Register]`** para auto-registration
2. **Injetar dependências com `[Inject]`** em readonly fields
3. **Bind env vars com `[EnvironmentConfig]`** e `[AppSettings]`
4. **Manter classes `partial`** para source generators
5. **Chamar `AddGeneratedServices()`** no bootstrap

## Fluxo de Trabalho

### 1. Criar Serviço com [Register]

```csharp
// src/Services/UserService.cs
[Register(typeof(IUserService), typeof(UserService), ServiceLifetime.Singleton)]
public partial class UserService : IUserService
{
    [Inject]
    private readonly IUserRepository _repository;

    [Inject]
    private readonly ILogger<UserService> _logger;

    public async Task<User> GetUserAsync(string id)
    {
        _logger.LogInformation("Fetching user {Id}", id);
        var user = await _repository.GetAsync(id);
        
        if (user == null)
            _logger.LogWarning("User {Id} not found", id);
        
        return user;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User { Id = Guid.NewGuid().ToString(), Email = request.Email };
        await _repository.SaveAsync(user);
        _logger.LogInformation("Created user {Id}", user.Id);
        return user;
    }
}
```

### 2. Registrar Repository

```csharp
// src/Repositories/UserRepository.cs
[Register(typeof(IUserRepository), typeof(UserRepository), ServiceLifetime.Singleton)]
public partial class UserRepository : IUserRepository
{
    [Inject]
    private readonly IDbConnection _connection;

    [Inject]
    private readonly ILogger<UserRepository> _logger;

    public async Task<User> GetAsync(string id)
    {
        _logger.LogDebug("Querying user {Id} from database", id);
        const string sql = "SELECT * FROM Users WHERE Id = @Id";
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        // ... execute query
        
        return user;
    }

    public async Task SaveAsync(User user)
    {
        _logger.LogDebug("Saving user {Id} to database", user.Id);
        const string sql = "INSERT INTO Users (Id, Email, CreatedAt) VALUES (@Id, @Email, @CreatedAt)";
        
        // ... execute insert
        _logger.LogInformation("User {Id} saved successfully", user.Id);
    }
}
```

### 3. Criar Config com [EnvironmentConfig]

```csharp
// src/Configuration/AppConfig.cs
public partial class AppConfig
{
    [EnvironmentConfig("ASPNETCORE_ENVIRONMENT")]
    public string Environment { get; set; }

    [EnvironmentConfig("DATABASE_URL")]
    public string DatabaseUrl { get; set; }

    [EnvironmentConfig("DATABASE_USER")]
    public string DatabaseUser { get; set; }

    [EnvironmentConfig("DATABASE_PASSWORD")]
    public string DatabasePassword { get; set; }

    [EnvironmentConfig("API_KEY")]
    public string ApiKey { get; set; }

    [EnvironmentConfig("LOG_LEVEL", DefaultValue = "Information")]
    public string LogLevel { get; set; }

    [EnvironmentConfig("API_VERSION", DefaultValue = "1.0.0")]
    public string ApiVersion { get; set; }

    [AppSettings("Jwt:Secret")]
    public string JwtSecret { get; set; }

    [AppSettings("Jwt:Issuer")]
    public string JwtIssuer { get; set; }

    [AppSettings("Jwt:ExpiryMinutes")]
    public int JwtExpiryMinutes { get; set; }

    [AppSettings("Features:EnableCaching", DefaultValue = "true")]
    public bool EnableCaching { get; set; }
}
```

### 4. Usar Source Generator para Mediator Handler

```csharp
// src/Handlers/CreateUserCommandHandler.cs
public record CreateUserCommand(string Name, string Email) 
    : IRequest<CreateUserResponse>;

public record CreateUserResponse(string UserId);

// Source generator detecta IRequestHandler e registra automaticamente
public partial class CreateUserCommandHandler 
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    [Inject]
    private readonly IUserService _userService;

    [Inject]
    private readonly IValidator<CreateUserCommand> _validator;

    public async ValueTask<CreateUserResponse> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
            throw new ValidationException("Invalid request");

        var user = await _userService.CreateUserAsync(
            new CreateUserRequest { Name = request.Name, Email = request.Email });

        return new CreateUserResponse(user.Id);
    }
}
```

### 5. Usar Source Generator para Validator

```csharp
// src/Validators/CreateUserCommandValidator.cs
public partial class CreateUserCommandValidator 
    : NativeValidator<CreateUserCommand>
{
    [Inject]
    private readonly IUserRepository _repository;

    public override CascadeMode CascadeMode => CascadeMode.Stop;

    public override async ValueTask<ValidationResult> ValidateAsync(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationFailure>();

        // Validate Name
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(new ValidationFailure("Name", "Name is required"));
        else if (request.Name.Length < 3)
            errors.Add(new ValidationFailure("Name", "Name must be at least 3 characters"));

        // Validate Email
        if (string.IsNullOrEmpty(request.Email))
            errors.Add(new ValidationFailure("Email", "Email is required"));
        else if (!request.Email.Contains("@"))
            errors.Add(new ValidationFailure("Email", "Email format is invalid"));
        else
        {
            // Async validation - check if email already exists
            var exists = await _repository.EmailExistsAsync(request.Email, cancellationToken);
            if (exists)
                errors.Add(new ValidationFailure("Email", "Email already in use"));
        }

        return new ValidationResult(errors);
    }
}
```

### 6. Registrar Tudo no Bootstrap

```csharp
// Program.cs (ou Lambda bootstrap)
var services = new ServiceCollection();

// Add core services
services.AddSingleton<IDbConnection>(sp =>
{
    var config = new AppConfig();
    // ... configure connection
});

// Add GENERATED services (handles all [Register], IRequestHandler, INativeValidator)
services.AddGeneratedServices();

// Add NativeMediator
services.AddSingleton<IMediator, Mediator>();

// Add logging
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Information);
    b.AddConsole();
});

var provider = services.BuildServiceProvider();

// Usar no handler
var mediator = provider.GetRequiredService<IMediator>();
var result = await mediator.Send(
    new CreateUserCommand("John Doe", "john@example.com"));
```

### 7. Build & Verify Generated Code

```bash
# Build projeto
dotnet build

# Verificar código gerado (está em .generated.cs)
ls bin/Debug/net8.0/Generated/

# Ver conteúdo
cat bin/Debug/net8.0/Generated/ServiceCollectionExtensions.cs
```

**Arquivo gerado (exemplo):**
```csharp
// Generated/ServiceCollectionExtensions.cs
namespace MyProject.Generated
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGeneratedServices(
            this IServiceCollection services,
            string group = null)
        {
            if (string.IsNullOrEmpty(group))
            {
                // [Register] services
                services.AddSingleton<IUserService, UserService>();
                services.AddSingleton<IUserRepository, UserRepository>();

                // IRequestHandler auto-register
                services.AddSingleton<IRequestHandler<CreateUserCommand, CreateUserResponse>,
                    CreateUserCommandHandler>();

                // INativeValidator auto-register
                services.AddSingleton<INativeValidator<CreateUserCommand>,
                    CreateUserCommandValidator>();
            }

            return services;
        }
    }
}
```

## Checklist Antes de Submeter

- [ ] Todas as classes com `[Register]` são `partial`
- [ ] Dependências injetadas via `[Inject]` em `private readonly` fields
- [ ] `AppConfig` usa `[EnvironmentConfig]` e `[AppSettings]`
- [ ] `dotnet build` sem diagnósticos DI/CONFIG erros
- [ ] `.generated.cs` criado com extensão de DI
- [ ] `AddGeneratedServices()` chamado no bootstrap
- [ ] Todos IRequestHandler/INativeValidator têm `[Register]` ou são auto-detectados
- [ ] DI funcionando em runtime (nenhuma `null` exception)
- [ ] Testes xUnit com `ServiceCollection` injetado

## Boas Práticas

### Estrutura de Projeto

```
src/
├── Configuration/
│   └── AppConfig.cs          // [EnvironmentConfig], [AppSettings]
├── Services/
│   ├── IUserService.cs
│   └── UserService.cs        // [Register], [Inject]
├── Repositories/
│   ├── IUserRepository.cs
│   └── UserRepository.cs     // [Register], [Inject]
├── Handlers/
│   └── CreateUserCommandHandler.cs // IRequestHandler
├── Validators/
│   └── CreateUserCommandValidator.cs // INativeValidator
└── Program.cs                        // AddGeneratedServices()
```

### Nomeação Consistente

```csharp
// ✅ Bom
[Register(typeof(IUserService), typeof(UserService), ServiceLifetime.Singleton)]
public partial class UserService : IUserService { }

// ❌ Ruim
[Register(typeof(IService), typeof(Service), ServiceLifetime.Singleton)]
public partial class Service : IService { }
```

### Ciclos de Dependência

```csharp
// ❌ ERRO: UserService -> OrderService -> UserService
[Register(...)]
public partial class UserService
{
    [Inject]
    private readonly IOrderService _orderService;
}

[Register(...)]
public partial class OrderService
{
    [Inject]
    private readonly IUserService _userService;  // Ciclo!
}

// ✅ Correto: Usar interface + late binding
[Register(...)]
public partial class UserService { }

[Register(...)]
public partial class OrderService
{
    [Inject]
    private readonly IServiceProvider _serviceProvider;
    
    public void ProcessOrder(string userId)
    {
        var userService = _serviceProvider.GetRequiredService<IUserService>();
    }
}
```

### Lifetime Correto

```csharp
// ❌ ERRO: Scoped depende de Transient (pode resultar em estado inconsistente)
[Register(typeof(IRepository), typeof(Repository), ServiceLifetime.Scoped)]
public partial class Repository
{
    [Inject]
    private readonly ILogger<Repository> _logger; // Transient!
}

// ✅ Correto: Logger é Singleton
[Register(typeof(IRepository), typeof(Repository), ServiceLifetime.Scoped)]
public partial class Repository
{
    [Inject]
    private readonly ILogger<Repository> _logger; // Singleton (certo)
}
```

## Testes com Source Generator

```csharp
[Fact]
public void ServiceCollection_AddGeneratedServices_RegistersAllServices()
{
    var services = new ServiceCollection();
    services.AddGeneratedServices();
    
    var provider = services.BuildServiceProvider();
    
    // Verificar que serviços foram registrados
    var userService = provider.GetService<IUserService>();
    Assert.NotNull(userService);
    
    var userRepository = provider.GetService<IUserRepository>();
    Assert.NotNull(userRepository);
}

[Fact]
public void AppConfig_EnvironmentVariables_LoadedCorrectly()
{
    Environment.SetEnvironmentVariable("DATABASE_URL", "localhost:5432");
    Environment.SetEnvironmentVariable("API_KEY", "secret123");
    
    var config = new AppConfig();
    
    Assert.Equal("localhost:5432", config.DatabaseUrl);
    Assert.Equal("secret123", config.ApiKey);
    Assert.Equal("Information", config.LogLevel); // DefaultValue
}
```

## Links Úteis

- **CLAUDE.md:** Referência de API completa
- **Roslyn docs:** C# source generators
- **Diagnostic codes:** Erro compiling
