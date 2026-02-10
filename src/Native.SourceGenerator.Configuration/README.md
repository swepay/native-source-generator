# Native.SourceGenerator.Configuration

[![NuGet](https://img.shields.io/nuget/v/Native.SourceGenerator.Configuration.svg)](https://www.nuget.org/packages/Native.SourceGenerator.Configuration/)

**AOT-first Source Generator for Configuration Binding** - Binds configuration values (environment variables and appsettings.json) to fields without reflection or `ConfigurationBinder`.

## Features

- ✅ **100% Native AOT Compatible** - No reflection, no `ConfigurationBinder`
- ✅ **Compile-time Binding Generation** - All errors caught at build time
- ✅ **Two Configuration Sources**:
  - `[EnvironmentConfig]` - Read from environment variables
  - `[AppSettings]` - Read from appsettings.json (IConfiguration)
- ✅ **Type-safe Configuration** - Supports `string`, `int`, `bool`, `double`, `decimal`, `TimeSpan`, `DateTime`, `Guid`, `Uri`
- ✅ **IIncrementalGenerator** - Fast, incremental builds

## Installation

```bash
dotnet add package Native.SourceGenerator.Configuration
```

## Usage

### Option 1: Using `[EnvironmentConfig]` for Environment Variables

Mark your class as `partial` and fields with `[EnvironmentConfig]`:

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

### Option 2: Using `[AppSettings]` for appsettings.json

For configuration stored in `appsettings.json`, use the `[AppSettings]` attribute with the configuration key path:

**appsettings.json:**
```json
{
    "Services": {
        "UrlBase": "http://localhost:3000",
        "Timeout": 30
    },
    "Database": {
        "ConnectionString": "Server=localhost;Database=mydb",
        "MaxPoolSize": 100
    },
    "Features": {
        "EnableNewDashboard": true
    }
}
```

**C# Configuration Class:**
```csharp
using Native.SourceGenerator.Configuration;

public partial class ServiceSettings
{
    [AppSettings("Services:UrlBase")]
    private string _urlBase;
    public string UrlBase => _urlBase;

    [AppSettings("Services:Timeout", Required = false, DefaultValue = "60")]
    private int _timeout;
    public int Timeout => _timeout;

    [AppSettings("Database:ConnectionString")]
    private string _connectionString;
    public string ConnectionString => _connectionString;

    [AppSettings("Features:EnableNewDashboard", Required = false, DefaultValue = "false")]
    private bool _enableNewDashboard;
    public bool EnableNewDashboard => _enableNewDashboard;
}
```

### Option 3: Mixing Both Attributes

You can use both attributes in the same class for hybrid configuration:

```csharp
using Native.SourceGenerator.Configuration;

public partial class HybridSettings
{
    // From environment variable (typically secrets)
    [EnvironmentConfig("API_SECRET_KEY")]
    private string _secretKey;
    public string SecretKey => _secretKey;

    // From appsettings.json (application settings)
    [AppSettings("Api:BaseUrl")]
    private string _baseUrl;
    public string BaseUrl => _baseUrl;

    [AppSettings("Api:Timeout", Required = false, DefaultValue = "30")]
    private int _timeout;
    public int Timeout => _timeout;
}
```

### Injecting Configuration

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var settings = new ServiceSettings();
settings.__InjectConfiguration(configuration);

Console.WriteLine($"URL Base: {settings.UrlBase}");
Console.WriteLine($"Timeout: {settings.Timeout}");
```

### Generated Code

The generator creates an `__InjectConfiguration` method:

```csharp
// Auto-generated
partial class ServiceSettings
{
    public void __InjectConfiguration(IConfiguration configuration)
    {
        _urlBase = configuration["Services:UrlBase"]
            ?? throw new InvalidOperationException("Missing required configuration: Services:UrlBase");

        _timeout = configuration["Services:Timeout"] is { Length: > 0 } __timeoutValue
            ? int.Parse(__timeoutValue)
            : int.Parse("60");

        _connectionString = configuration["Database:ConnectionString"]
            ?? throw new InvalidOperationException("Missing required configuration: Database:ConnectionString");

        _enableNewDashboard = configuration["Features:EnableNewDashboard"] is { Length: > 0 } __enableNewDashboardValue
            ? bool.Parse(__enableNewDashboardValue)
            : bool.Parse("false");
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

### `[AppSettings]`

| Parameter | Type | Description |
|-----------|------|-------------|
| `key` | `string` | The configuration key path (e.g., `"Section:SubSection:Key"`) |
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
| `Uri` | `new Uri()` |

Nullable versions (`int?`, `bool?`, etc.) are also supported.

## Diagnostics

| Code | Description |
|------|-------------|
| `CONFIG001` | Class must be partial |
| `CONFIG002` | Unsupported field type |
| `CONFIG003` | Field must not be static |
| `CONFIG004` | Duplicate configuration key |
| `CONFIG005` | Empty configuration key |

## Requirements

- .NET 10 SDK
- C# 13+
- `Microsoft.Extensions.Configuration` package
- `Microsoft.Extensions.Configuration.Json` package (for appsettings.json)
- `Microsoft.Extensions.Configuration.EnvironmentVariables` package (for environment variables)

## License

MIT
