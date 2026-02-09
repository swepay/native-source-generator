namespace NativeFluentValidator.SourceGenerator.DependencyInjection;

using System.Linq;
using Microsoft.CodeAnalysis;
using NativeFluentValidator.SourceGenerator.DependencyInjection.Attributes;
using NativeFluentValidator.SourceGenerator.DependencyInjection.Emitters;
using NativeFluentValidator.SourceGenerator.DependencyInjection.Parsing;

/// <summary>
/// Incremental source generator for AOT-compatible NativeFluentValidator registration.
/// Generates constructors and validator registration code without reflection.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ValidatorDependencyInjectionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("NativeValidatorAttribute.g.cs", ValidatorAttributes.NativeValidatorAttributeSource);
        });

        // Find all classes with [NativeValidator] attribute
        var validatorDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, ct) => ValidatorParser.HasNativeValidatorAttribute(node, ct),
                transform: static (ctx, ct) => ValidatorParser.ParseValidatorRegistration(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        // Generate constructors for each validator
        context.RegisterSourceOutput(validatorDeclarations, static (ctx, info) =>
        {
            var constructorSource = ValidatorConstructorEmitter.EmitConstructor(info);
            if (!string.IsNullOrEmpty(constructorSource))
            {
                ctx.AddSource($"{info.ClassName}.Constructor.g.cs", constructorSource);
            }
        });

        // Collect all validators and generate registration extensions
        var allValidators = validatorDeclarations.Collect();
        context.RegisterSourceOutput(allValidators, static (ctx, validators) =>
        {
            var registrationSource = ValidatorRegistrationEmitter.EmitValidatorRegistrations(validators);
            if (!string.IsNullOrEmpty(registrationSource))
            {
                ctx.AddSource("NativeFluentValidatorServices.g.cs", registrationSource);
            }
        });
    }
}
