using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Native.SourceGenerator.DependencyInjection;
using Native.SourceGenerator.DependencyInjection.Generated;
using Native.SourceGenerator.Configuration;

// Build configuration
var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

// Build service collection
var services = new ServiceCollection();
services.AddGeneratedServices();

var provider = services.BuildServiceProvider();

// Test DI
var myService = provider.GetRequiredService<IMyService>();
Console.WriteLine($"MyService created: {myService.GetType().Name}");

// Test Configuration
var appSettings = new AppSettings();
appSettings.__InjectConfiguration(configuration);
Console.WriteLine($"AppName: {appSettings.AppName}");
Console.WriteLine($"MaxRetries: {appSettings.MaxRetries}");

Console.WriteLine("AOT test passed!");

// Sample interfaces and implementations
public interface IMyService { }
public interface ILogger { }

public class ConsoleLogger : ILogger { }

[Register(typeof(IMyService))]
public partial class MyService : IMyService
{
    [Inject] private readonly ILogger _logger;

    public void DoSomething() => Console.WriteLine("Doing something...");
}

[Register(typeof(ILogger))]
public partial class LoggerService : ILogger { }

// Sample configuration class
public partial class AppSettings
{
    [EnvironmentConfig("APP_NAME")]
    private string _appName = "DefaultApp";
    public string AppName => _appName;

    [EnvironmentConfig("MAX_RETRIES")]
    private int _maxRetries = 3;
    public int MaxRetries => _maxRetries;

    [EnvironmentConfig("ENABLE_FEATURE")]
    private bool _enableFeature = false;
    public bool EnableFeature => _enableFeature;
}
