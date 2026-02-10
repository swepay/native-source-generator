namespace NativeMediator.SourceGenerator.DependencyInjection;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using NativeMediator.SourceGenerator.DependencyInjection.Attributes;
using NativeMediator.SourceGenerator.DependencyInjection.Emitters;
using NativeMediator.SourceGenerator.DependencyInjection.Models;
using NativeMediator.SourceGenerator.DependencyInjection.Parsing;

/// <summary>
/// Incremental source generator for AOT-compatible NativeMediator handler registration.
/// Generates constructors and handler registration code without reflection.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MediatorDependencyInjectionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the attribute source
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("MediatorHandlerAttribute.g.cs", MediatorAttributes.MediatorHandlerAttributeSource);
        });

        // Find all classes with [MediatorHandler] attribute
        IncrementalValuesProvider<HandlerRegistrationInfo> handlerDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, ct) => HandlerParser.HasMediatorHandlerAttribute(node, ct),
                transform: static (ctx, ct) => HandlerParser.ParseHandlerRegistration(ctx, ct))
            .Where(static info => info is not null)
            .Select(static (info, _) => info!.Value);

        // Generate constructors for each handler
        context.RegisterSourceOutput(handlerDeclarations, static (ctx, info) =>
        {
            var constructorSource = HandlerConstructorEmitter.EmitConstructor(info);
            if (!string.IsNullOrEmpty(constructorSource))
            {
                ctx.AddSource($"{info.ClassName}.Constructor.g.cs", constructorSource);
            }
        });

        // Collect all handlers and generate registration extensions
        IncrementalValueProvider<ImmutableArray<HandlerRegistrationInfo>> allHandlers = handlerDeclarations.Collect();
        context.RegisterSourceOutput(allHandlers, static (ctx, handlers) =>
        {
            var registrationSource = HandlerRegistrationEmitter.EmitHandlerRegistrations(handlers);
            if (!string.IsNullOrEmpty(registrationSource))
            {
                ctx.AddSource("NativeMediatorServices.g.cs", registrationSource);
            }
        });
    }
}
