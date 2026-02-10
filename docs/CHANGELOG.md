# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2026-02-10

### Added

- **Native.SourceGenerator.Configuration**: New `[AppSettings]` attribute for reading configuration from `appsettings.json`
  - Supports nested configuration paths (e.g., `"Services:UrlBase"`, `"Database:ConnectionString"`)
  - Same options as `[EnvironmentConfig]`: `Required` and `DefaultValue`
  - Can be mixed with `[EnvironmentConfig]` in the same class
  - 8 new unit tests for AppSettings functionality

### Changed

- **Native.SourceGenerator.Configuration**: Internal model renamed `EnvironmentVariableName` to `ConfigurationKey` to support both configuration sources
- **Native.SourceGenerator.Configuration**: Updated diagnostic messages to reference both `[EnvironmentConfig]` and `[AppSettings]` attributes

## [1.0.2] - 2026-02-10

### Added

- Individual README.md files for each NuGet package
- Package documentation for NuGet.org display

### Changed

- Version bump to 1.0.2

## [1.0.1] - 2026-02-10

### Added

- Package icon (`native-source-generator.png`) for all NuGet packages
- Fixed CI/CD pipeline for Native AOT compatibility testing

### Fixed

- Fully qualified static method calls in `ServiceRegistrationEmitter` for extension methods
- CI workflow now uses existing `aot-test/` folder instead of creating inline test
- Package icon path using `$(MSBuildThisFileDirectory)` for correct resolution

## [1.0.0] - 2026-02-10

### Added

- **Native.SourceGenerator.DependencyInjection**: AOT-first source generator for dependency injection
  - `[Injectable]` attribute with `Lifetime` and `Group` support
  - Constructor generation without reflection
  - Service registration generation
  - Grouping support for modular registration

- **Native.SourceGenerator.Configuration**: AOT-first source generator for configuration binding
  - `[EnvironmentConfig]` attribute for environment variable binding
  - Support for multiple types: `string`, `int`, `bool`, `double`, `decimal`, `TimeSpan`, `Uri`
  - `Required` and `DefaultValue` options

- **NativeMediator.SourceGenerator.DependencyInjection**: AOT-first source generator for MediatR handler registration
  - Automatic discovery of `IRequestHandler<,>` implementations
  - Compile-time registration without assembly scanning

- **NativeFluentValidator.SourceGenerator.DependencyInjection**: AOT-first source generator for FluentValidation
  - Automatic discovery of `AbstractValidator<T>` implementations
  - Compile-time registration without assembly scanning

### Technical Details

- All generators use `IIncrementalGenerator` for optimal performance
- Zero reflection - 100% Native AOT compatible
- Compile-time error detection with detailed diagnostics
- 22 unit tests with full coverage
