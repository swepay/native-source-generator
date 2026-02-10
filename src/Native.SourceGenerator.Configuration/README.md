# Native.SourceGenerator.Configuration

[![NuGet](https://img.shields.io/nuget/v/Native.SourceGenerator.Configuration.svg)](https://www.nuget.org/packages/Native.SourceGenerator.Configuration/)

**AOT-first Source Generator for Configuration Binding** - Binds environment variables to fields without reflection or `ConfigurationBinder`.

## Features

- ✅ **100% Native AOT Compatible** - No reflection, no `ConfigurationBinder`
- ✅ **Compile-time Binding Generation** - All errors caught at build time
- ✅ **Type-safe Configuration** - Supports `string`, `int`, `bool`, `double`, `decimal`, `TimeSpan`, `DateTime`, `Guid`
- ✅ **IIncrementalGenerator** - Fast, incremental builds

## Installation

```bash
dotnet add package Native.SourceGenerator.Configuration
```

## Usage

### 1. Mark your class as `partial` and fields with `[EnvironmentConfig]`

```csharp
using Native.SourceGenerator.Configuration;

public partial class DatabaseSettings
{
    [EnvironmentConfig("DATABASE_URL")]
    private string _connectionString;
    public string ConnectionString => _connectionString;

    [EnvironmentConfig("MAX_CONNECTIONS", Required = false, DefaultValue = "100")]
    private int _maxConnections;
    public int MaxConnections => _maxConnections;

    [EnvironmentConfig("ENABLE_SSL", Required = false, DefaultValue = "true")]
    private bool _enableSsl;
    public bool EnableSsl => _enableSsl;
}
```

### 2. Inject configuration

```csharp
var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var settings = new DatabaseSettings();
settings.__InjectConfiguration(configuration);

Console.WriteLine($"Connection: {settings.ConnectionString}");
Console.WriteLine($"Max Connections: {settings.MaxConnections}");
```

### Generated Code

The generator creates an `__InjectConfiguration` method:

```csharp
// Auto-generated
partial class DatabaseSettings
{
    public void __InjectConfiguration(IConfiguration configuration)
    {
        _connectionString = configuration["DATABASE_URL"]
            ?? throw new InvalidOperationException("Missing required configuration: DATABASE_URL");

        _maxConnections = configuration["MAX_CONNECTIONS"] is { Length: > 0 } __maxConnectionsValue
            ? int.Parse(__maxConnectionsValue)
            : 100;

        _enableSsl = configuration["ENABLE_SSL"] is { Length: > 0 } __enableSslValue
            ? bool.Parse(__enableSslValue)
            : true;
    }
}
```

## Attribute Options

### `[EnvironmentConfig]`

| Parameter | Type | Description |
|-----------|------|-------------|
| `environmentVariableName` | `string` | The name of the environment variable |
| `Required` | `bool` | Whether the value is required (default: `true`) |
| `DefaultValue` | `string` | Default value if not required and not set |

## Supported Types

| Type | Parse Method |
|------|--------------|
| `string` | Direct assignment |
| `int` | `int.Parse()` |
| `long` | `long.Parse()` |
| `double` | `double.Parse()` |
| `decimal` | `decimal.Parse()` |
| `bool` | `bool.Parse()` |
| `TimeSpan` | `TimeSpan.Parse()` |
| `DateTime` | `DateTime.Parse()` |
| `Guid` | `Guid.Parse()` |

Nullable versions (`int?`, `bool?`, etc.) are also supported.

## Diagnostics

| Code | Description |
|------|-------------|
| `CONFIG001` | Class must be partial |
| `CONFIG002` | Unsupported field type |

## Requirements

- .NET 10 SDK
- C# 13+
- `Microsoft.Extensions.Configuration` package

## License

MIT
