# NativeFluentValidator.SourceGenerator.DependencyInjection

[![NuGet](https://img.shields.io/nuget/v/NativeFluentValidator.SourceGenerator.DependencyInjection.svg)](https://www.nuget.org/packages/NativeFluentValidator.SourceGenerator.DependencyInjection/)

**AOT-first Source Generator for FluentValidation Validator Registration** - Automatically registers all `AbstractValidator<T>` implementations without assembly scanning.

## Features

- ✅ **100% Native AOT Compatible** - No reflection, no assembly scanning
- ✅ **Compile-time Registration** - All validators discovered at build time
- ✅ **Seamless FluentValidation Integration** - Works with existing FluentValidation patterns
- ✅ **IIncrementalGenerator** - Fast, incremental builds

## Installation

```bash
dotnet add package NativeFluentValidator.SourceGenerator.DependencyInjection
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

## Usage

### 1. Create your validators as usual

```csharp
using FluentValidation;

public class User
{
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Age).InclusiveBetween(0, 150);
    }
}
```

### 2. Register with generated method

```csharp
using Microsoft.Extensions.DependencyInjection;
using NativeFluentValidator.SourceGenerator.DependencyInjection.Generated;

var services = new ServiceCollection();

// Generated method - registers all AbstractValidator implementations
services.AddNativeValidators();
```

### Generated Code

The generator discovers all `AbstractValidator<T>` implementations and generates:

```csharp
// Auto-generated
namespace NativeFluentValidator.SourceGenerator.DependencyInjection.Generated
{
    public static class NativeValidatorServiceCollectionExtensions
    {
        public static IServiceCollection AddNativeValidators(this IServiceCollection services)
        {
            // Scoped by default, matching FluentValidation conventions
            services.AddScoped<IValidator<User>, UserValidator>();
            services.AddScoped<IValidator<Order>, OrderValidator>();
            // ... all discovered validators
            return services;
        }
    }
}
```

## Validator Discovery

The generator automatically finds:

- Classes inheriting from `AbstractValidator<T>`
- Non-abstract, non-generic classes only
- Supports validators in any namespace

## Diagnostics

| Code | Description |
|------|-------------|
| `FV001` | Validator must be a non-abstract class |
| `FV002` | Validator must have a public constructor |

## Requirements

- .NET 10 SDK
- C# 13+
- `FluentValidation` package (v11+)

## Why Use This?

FluentValidation's `AddValidatorsFromAssembly()` uses reflection to scan assemblies, which is incompatible with Native AOT. This generator eliminates that by discovering validators at compile-time and generating explicit registrations.

## Example: Full Setup

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NativeFluentValidator.SourceGenerator.DependencyInjection.Generated;

var services = new ServiceCollection();
services.AddNativeValidators();

var provider = services.BuildServiceProvider();
var validator = provider.GetRequiredService<IValidator<User>>();

var user = new User { Name = "", Email = "invalid", Age = -1 };
var result = validator.Validate(user);

foreach (var error in result.Errors)
{
    Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
}
```

## License

MIT
