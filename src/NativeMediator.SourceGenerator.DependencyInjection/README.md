# NativeMediator.SourceGenerator.DependencyInjection

[![NuGet](https://img.shields.io/nuget/v/NativeMediator.SourceGenerator.DependencyInjection.svg)](https://www.nuget.org/packages/NativeMediator.SourceGenerator.DependencyInjection/)

**AOT-first Source Generator for MediatR Handler Registration** - Automatically registers all `IRequestHandler<,>` implementations without assembly scanning.

## Features

- ✅ **100% Native AOT Compatible** - No reflection, no assembly scanning
- ✅ **Compile-time Registration** - All handlers discovered at build time
- ✅ **Seamless MediatR Integration** - Works with existing MediatR patterns
- ✅ **IIncrementalGenerator** - Fast, incremental builds

## Installation

```bash
dotnet add package NativeMediator.SourceGenerator.DependencyInjection
dotnet add package MediatR
```

## Usage

### 1. Create your handlers as usual

```csharp
using MediatR;

public record GetUserQuery(int Id) : IRequest<User>;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new User { Id = request.Id, Name = "John Doe" });
    }
}
```

### 2. Register with generated method

```csharp
using Microsoft.Extensions.DependencyInjection;
using NativeMediator.SourceGenerator.DependencyInjection.Generated;

var services = new ServiceCollection();
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

// Generated method - registers all IRequestHandler implementations
services.AddNativeMediatorHandlers();
```

### Generated Code

The generator discovers all `IRequestHandler<TRequest, TResponse>` implementations and generates:

```csharp
// Auto-generated
namespace NativeMediator.SourceGenerator.DependencyInjection.Generated
{
    public static class NativeMediatorServiceCollectionExtensions
    {
        public static IServiceCollection AddNativeMediatorHandlers(this IServiceCollection services)
        {
            // Scoped by default, matching MediatR conventions
            services.AddScoped<IRequestHandler<GetUserQuery, User>, GetUserHandler>();
            services.AddScoped<IRequestHandler<CreateOrderCommand, OrderResult>, CreateOrderHandler>();
            // ... all discovered handlers
            return services;
        }
    }
}
```

## Handler Discovery

The generator automatically finds:

- Classes implementing `IRequestHandler<TRequest, TResponse>`
- Non-abstract, non-generic classes only
- Both sync and async handlers

## Diagnostics

| Code | Description |
|------|-------------|
| `MED001` | Handler must be a non-abstract class |
| `MED002` | Handler must have a public constructor |

## Requirements

- .NET 10 SDK
- C# 13+
- `MediatR` package (v12+)

## Why Use This?

MediatR's default registration uses `Assembly.GetTypes()` which is incompatible with Native AOT. This generator eliminates that by discovering handlers at compile-time and generating explicit registrations.

## License

MIT
