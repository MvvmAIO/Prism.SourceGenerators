using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Helpers;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// A source generator that generates <c>DelegateCommand</c> / <c>AsyncDelegateCommand</c> properties
/// from methods annotated with <c>[DelegateCommand]</c> or <c>[AsyncDelegateCommand]</c>.
/// <para>
/// For synchronous methods (<c>void</c>), generates <c>DelegateCommand</c> or <c>DelegateCommand&lt;T&gt;</c>.
/// For asynchronous methods (<c>Task</c>), generates <c>AsyncDelegateCommand</c> or <c>AsyncDelegateCommand&lt;T&gt;</c>.
/// For Prism versions prior to 9.0, use NuGet <c>MvvmAIO.Prism.SourceGenerators</c> and install <c>MvvmAIO.Prism.Bcl.Commands</c> manually for Prism.Core 8.1.97 (see diagnostic PSG3002).
/// </para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DelegateCommandGenerator : IIncrementalGenerator
{
    private const string DelegateCommandAttributeName = "Prism.SourceGenerators.DelegateCommandAttribute";
    private const string AsyncDelegateCommandAttributeName = "Prism.SourceGenerators.AsyncDelegateCommandAttribute";
    private const string ObservesPropertyAttributeName = "Prism.SourceGenerators.ObservesPropertyAttribute";
    private const string ObservablePropertyAttributeName = "Prism.SourceGenerators.ObservablePropertyAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // --- [DelegateCommand] pipeline ---
        IncrementalValuesProvider<Result<CommandGenerationInfo>> delegateCommandInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    DelegateCommandAttributeName,
                    static (node, _) => node is MethodDeclarationSyntax
                    {
                        Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                    },
                    static (context, token) => ExtractDelegateCommandInfo(context, token));

        RegisterDiagnosticsAndCommands(context, delegateCommandInfos);

        // --- [AsyncDelegateCommand] pipeline (AllowMultiple=true, one command per attribute) ---
        IncrementalValuesProvider<Result<CommandGenerationInfo>> asyncCommandInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AsyncDelegateCommandAttributeName,
                    static (node, _) => node is MethodDeclarationSyntax
                    {
                        Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                    },
                    static (context, token) => ExtractAsyncDelegateCommandInfos(context, token))
                .SelectMany(static (results, _) => results);

        RegisterDiagnosticsAndCommands(context, asyncCommandInfos);

        // Prior to Prism 9: AsyncDelegateCommand must come from MvvmAIO.Prism.Bcl.Commands (PSG3002).
        IncrementalValueProvider<bool> needsPolyfillFromDelegate = delegateCommandInfos
            .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
            .Select(static (item, _) => item.Value!)
            .Where(static item => item.IsAsync && !item.HasAsyncDelegateCommand)
            .Collect()
            .Select(static (items, _) => items.Length > 0);

        IncrementalValueProvider<bool> needsPolyfillFromAsync = asyncCommandInfos
            .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
            .Select(static (item, _) => item.Value!)
            .Where(static item => !item.HasAsyncDelegateCommand)
            .Collect()
            .Select(static (items, _) => items.Length > 0);

        IncrementalValueProvider<bool> needsAsyncDelegatePackage = needsPolyfillFromDelegate
            .Combine(needsPolyfillFromAsync)
            .Select(static (pair, _) => pair.Left || pair.Right);

        context.RegisterSourceOutput(needsAsyncDelegatePackage, static (context, needs) =>
        {
            if (needs)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AsyncDelegateCommandPackageRequired,
                    Location.None,
                    "MvvmAIO.Prism.SourceGenerators"));
            }
        });
    }

    private static void RegisterDiagnosticsAndCommands(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<Result<CommandGenerationInfo>> commandInfos)
    {
        // Report diagnostics
        context.RegisterSourceOutput(
            commandInfos.Where(static item => !item.Errors.IsEmpty),
            static (context, result) =>
            {
                foreach (DiagnosticInfo diagnostic in result.Errors.AsImmutableArray())
                {
                    context.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            });

        // Generate command properties
        context.RegisterSourceOutput(
            commandInfos
                .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
                .Select(static (item, _) => item.Value!),
            static (context, info) =>
            {
                string source = GenerateCommandSource(info);
                context.AddSource(
                    $"{info.Hierarchy.FilenameHint}.{info.CommandName}.g.cs",
                    source);
            });
    }

    private static Result<CommandGenerationInfo> ExtractDelegateCommandInfo(
        GeneratorAttributeSyntaxContext context, System.Threading.CancellationToken token)
    {
        IMethodSymbol methodSymbol = (IMethodSymbol)context.TargetSymbol;
        INamedTypeSymbol containingType = methodSymbol.ContainingType;
        Compilation compilation = context.SemanticModel.Compilation;

        if (!IsPartialType(containingType, token))
        {
            return CreateNonPartialDiagnostic(containingType);
        }

        bool isAsync = IsAsyncMethod(methodSymbol, compilation);
        ImmutableArray<DiagnosticInfo>.Builder diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        if (!IsValidDelegateCommandMethodSignature(methodSymbol, isAsync, compilation))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.InvalidDelegateCommandMethodSignature,
                methodSymbol,
                methodSymbol.Name));
        }

        string? parameterType = ExtractParameterType(methodSymbol, isAsync);
        string methodName = methodSymbol.Name;
        string? commandName = null;
        string? canExecute = null;

        foreach (AttributeData attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == DelegateCommandAttributeName)
            {
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "CommandName" && namedArg.Value.Value is string cn)
                        commandName = cn;
                    else if (namedArg.Key == "CanExecute" && namedArg.Value.Value is string ce)
                        canExecute = ce;
                }
            }
        }

        commandName ??= GetCommandName(methodName);

        ImmutableArray<string> observesProperties = CollectObservesProperties(methodSymbol);

        bool hasAsyncDelegateCommand = HasConsumerVisibleAsyncDelegateCommandTypes(compilation);

        HierarchyInfo hierarchy = HierarchyInfo.From(containingType);

        bool useFieldKeyword = SupportsFieldKeyword(context);

        if (canExecute is not null && !HasMember(containingType, canExecute))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.CanExecuteMemberNotFound,
                methodSymbol,
                canExecute,
                containingType.Name));
        }

        foreach (string observedProperty in observesProperties)
        {
            if (!HasPropertyInTypeHierarchy(containingType, observedProperty)
                && !IsObservableGeneratedProperty(containingType, observedProperty))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.ObservesPropertyNotFound,
                    methodSymbol,
                    observedProperty,
                    containingType.Name));
            }
        }

        return new Result<CommandGenerationInfo>(
            new CommandGenerationInfo(
                hierarchy,
                methodName,
                commandName,
                parameterType,
                isAsync,
                canExecute,
                hasAsyncDelegateCommand,
                CancelAfterMicroseconds: null,
                Catch: null,
                CatchType: null,
                CancellationTokenSourceFactory: null,
                EnableParallelExecution: false,
                ObservesProperties: observesProperties,
                UseFieldKeyword: useFieldKeyword),
            diagnostics.ToImmutable());
    }

    private static ImmutableArray<Result<CommandGenerationInfo>> ExtractAsyncDelegateCommandInfos(
        GeneratorAttributeSyntaxContext context, System.Threading.CancellationToken token)
    {
        IMethodSymbol methodSymbol = (IMethodSymbol)context.TargetSymbol;
        INamedTypeSymbol containingType = methodSymbol.ContainingType;

        if (!IsPartialType(containingType, token))
        {
            return ImmutableArray.Create(CreateNonPartialDiagnostic(containingType));
        }

        string? parameterType = ExtractParameterType(methodSymbol, isAsync: true);
        string methodName = methodSymbol.Name;
        ImmutableArray<string> observesProperties = CollectObservesProperties(methodSymbol);
        Compilation compilation = context.SemanticModel.Compilation;
        bool hasAsyncDelegateCommand = HasConsumerVisibleAsyncDelegateCommandTypes(compilation);
        HierarchyInfo hierarchy = HierarchyInfo.From(containingType);
        bool useFieldKeyword = SupportsFieldKeyword(context);

        ImmutableArray<Result<CommandGenerationInfo>>.Builder builder =
            ImmutableArray.CreateBuilder<Result<CommandGenerationInfo>>();

        foreach (AttributeData attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != AsyncDelegateCommandAttributeName)
                continue;

            string? commandName = null;
            string? canExecute = null;
            double? cancelAfterMicroseconds = null;
            string? catchHandler = null;
            string? catchType = null;
            string? cancellationTokenSourceFactory = null;
            bool enableParallelExecution = false;
            ImmutableArray<DiagnosticInfo>.Builder diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

            if (!IsValidAsyncDelegateCommandMethodSignature(methodSymbol, compilation))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    DiagnosticDescriptors.InvalidAsyncDelegateCommandMethodSignature,
                    methodSymbol,
                    methodSymbol.Name));
            }

            foreach (var namedArg in attr.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "CommandName" when namedArg.Value.Value is string cn:
                        commandName = cn;
                        break;
                    case "CanExecute" when namedArg.Value.Value is string ce:
                        canExecute = ce;
                        if (!HasMember(containingType, ce))
                        {
                            diagnostics.Add(DiagnosticInfo.Create(
                                DiagnosticDescriptors.CanExecuteMemberNotFound,
                                methodSymbol,
                                ce,
                                containingType.Name));
                        }
                        break;
                    case "CancelAfterMicroseconds" when namedArg.Value.Value is double ms:
                        cancelAfterMicroseconds = ms;
                        break;
                    case "Catch" when namedArg.Value.Value is string ch:
                        catchHandler = ch;
                        catchType = ResolveCatchType(containingType, ch, compilation);
                        if (!HasMember(containingType, ch))
                        {
                            diagnostics.Add(DiagnosticInfo.Create(
                                DiagnosticDescriptors.CatchHandlerNotFound,
                                methodSymbol,
                                ch,
                                containingType.Name));
                        }
                        else if (!IsValidCatchHandler(containingType, ch, compilation))
                        {
                            diagnostics.Add(DiagnosticInfo.Create(
                                DiagnosticDescriptors.CatchHandlerInvalidSignature,
                                methodSymbol,
                                ch,
                                containingType.Name));
                        }
                        break;
                    case "CancellationTokenSourceFactory" when namedArg.Value.Value is string ctsf:
                        cancellationTokenSourceFactory = ctsf;
                        break;
                    case "EnableParallelExecution" when namedArg.Value.Value is bool epe:
                        enableParallelExecution = epe;
                        break;
                }
            }

            commandName ??= GetCommandName(methodName);

            foreach (string observedProperty in observesProperties)
            {
                if (!HasPropertyInTypeHierarchy(containingType, observedProperty)
                    && !IsObservableGeneratedProperty(containingType, observedProperty))
                {
                    diagnostics.Add(DiagnosticInfo.Create(
                        DiagnosticDescriptors.ObservesPropertyNotFound,
                        methodSymbol,
                        observedProperty,
                        containingType.Name));
                }
            }

            builder.Add(new Result<CommandGenerationInfo>(
                new CommandGenerationInfo(
                    hierarchy,
                    methodName,
                    commandName,
                    parameterType,
                    IsAsync: true,
                    canExecute,
                    hasAsyncDelegateCommand,
                    cancelAfterMicroseconds,
                    catchHandler,
                    catchType,
                    cancellationTokenSourceFactory,
                    enableParallelExecution,
                    observesProperties,
                    useFieldKeyword),
                diagnostics.ToImmutable()));
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<string> CollectObservesProperties(IMethodSymbol methodSymbol)
    {
        ImmutableArray<string>.Builder? builder = null;

        foreach (AttributeData attr in methodSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != ObservesPropertyAttributeName)
                continue;

            // First constructor arg is propertyName
            if (attr.ConstructorArguments.Length >= 1 && attr.ConstructorArguments[0].Value is string propName)
            {
                builder ??= ImmutableArray.CreateBuilder<string>();
                builder.Add(propName);

                // params string[] otherPropertyNames
                if (attr.ConstructorArguments.Length >= 2 && !attr.ConstructorArguments[1].IsNull)
                {
                    foreach (var item in attr.ConstructorArguments[1].Values)
                    {
                        if (item.Value is string otherProp)
                            builder.Add(otherProp);
                    }
                }
            }
        }

        return builder?.ToImmutable() ?? ImmutableArray<string>.Empty;
    }

    private static bool IsPartialType(INamedTypeSymbol containingType, System.Threading.CancellationToken token)
    {
        return containingType.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(token))
            .OfType<TypeDeclarationSyntax>()
            .Any(static t => t.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static Result<CommandGenerationInfo> CreateNonPartialDiagnostic(INamedTypeSymbol containingType)
    {
        return new Result<CommandGenerationInfo>(
            default!,
            ImmutableArray.Create(
                DiagnosticInfo.Create(
                    DiagnosticDescriptors.NonPartialClassWithDelegateCommand,
                    containingType,
                    containingType.Name)));
    }

    private static string? ExtractParameterType(IMethodSymbol methodSymbol, bool isAsync)
    {
        if (isAsync && methodSymbol.Parameters.Length == 1)
        {
            IParameterSymbol param = methodSymbol.Parameters[0];
            if (!IsCancellationToken(param.Type))
            {
                return param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
        else if (isAsync && methodSymbol.Parameters.Length == 2
            && IsCancellationToken(methodSymbol.Parameters[1].Type))
        {
            return methodSymbol.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }
        else if (!isAsync && methodSymbol.Parameters.Length == 1)
        {
            return methodSymbol.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        return null;
    }

    private static bool IsAsyncMethod(IMethodSymbol method, Compilation compilation)
    {
        INamedTypeSymbol? taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        INamedTypeSymbol? taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

        if (taskType is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(method.ReturnType, taskType)
            || (method.ReturnType is INamedTypeSymbol namedReturn
                && namedReturn.IsGenericType
                && SymbolEqualityComparer.Default.Equals(namedReturn.ConstructedFrom, taskOfTType));
    }

    private static bool IsCancellationToken(ITypeSymbol type)
    {
        return type.ToDisplayString() == "System.Threading.CancellationToken";
    }

    private static bool IsValidDelegateCommandMethodSignature(IMethodSymbol methodSymbol, bool isAsync, Compilation compilation)
    {
        if (!isAsync)
        {
            return methodSymbol.ReturnsVoid && methodSymbol.Parameters.Length <= 1;
        }

        if (!IsNonGenericTaskReturnType(methodSymbol.ReturnType, compilation))
            return false;

        if (methodSymbol.Parameters.Length == 0)
            return true;

        if (methodSymbol.Parameters.Length == 1)
            return true;

        return methodSymbol.Parameters.Length == 2 && IsCancellationToken(methodSymbol.Parameters[1].Type);
    }

    private static bool IsValidAsyncDelegateCommandMethodSignature(IMethodSymbol methodSymbol, Compilation compilation)
    {
        if (!IsNonGenericTaskReturnType(methodSymbol.ReturnType, compilation))
            return false;

        if (methodSymbol.Parameters.Length == 0)
            return true;

        if (methodSymbol.Parameters.Length == 1)
            return true;

        return methodSymbol.Parameters.Length == 2 && IsCancellationToken(methodSymbol.Parameters[1].Type);
    }

    private static bool IsNonGenericTaskReturnType(ITypeSymbol returnType, Compilation compilation)
    {
        INamedTypeSymbol? expectedTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

        return expectedTask is not null
            && returnType is INamedTypeSymbol namedReturn
            && !namedReturn.IsGenericType
            && SymbolEqualityComparer.Default.Equals(namedReturn.OriginalDefinition, expectedTask);
    }

    private static bool HasMember(INamedTypeSymbol containingType, string memberName)
    {
        for (INamedTypeSymbol? current = containingType; current is not null; current = current.BaseType)
        {
            if (current.GetMembers(memberName).Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasPropertyInTypeHierarchy(INamedTypeSymbol containingType, string propertyName)
    {
        for (INamedTypeSymbol? current = containingType; current is not null; current = current.BaseType)
        {
            if (current.GetMembers(propertyName).OfType<IPropertySymbol>().Any())
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsObservableGeneratedProperty(INamedTypeSymbol containingType, string propertyName)
    {
        for (INamedTypeSymbol? current = containingType; current is not null; current = current.BaseType)
        {
            foreach (IFieldSymbol field in current.GetMembers().OfType<IFieldSymbol>())
            {
                if (HasAttribute(field, ObservablePropertyAttributeName)
                    && GetObservablePropertyName(field.Name) == propertyName)
                {
                    return true;
                }
            }

            foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (HasAttribute(property, ObservablePropertyAttributeName)
                    && property.Name == propertyName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == attributeName);

    private static string GetObservablePropertyName(string fieldName)
    {
        if (fieldName.StartsWith("m_") && fieldName.Length > 2)
            return char.ToUpperInvariant(fieldName[2]) + fieldName.Substring(3);
        if (fieldName.StartsWith("_") && fieldName.Length > 1)
            return char.ToUpperInvariant(fieldName[1]) + fieldName.Substring(2);
        return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
    }

    private static string? ResolveCatchType(INamedTypeSymbol containingType, string catchName, Compilation compilation)
    {
        INamedTypeSymbol? exceptionType = compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionType is null)
            return null;

        foreach (ISymbol member in containingType.GetMembers(catchName))
        {
            switch (member)
            {
                case IMethodSymbol { Parameters.Length: 1 } method:
                {
                    ITypeSymbol parameterType = method.Parameters[0].Type;
                    if (parameterType is ITypeParameterSymbol typeParameter)
                    {
                        ITypeSymbol? constrainedExceptionType = typeParameter.ConstraintTypes
                            .FirstOrDefault(constraint => IsExceptionTypeOrDerived(constraint, exceptionType));

                        if (constrainedExceptionType is not null
                            && !SymbolEqualityComparer.Default.Equals(constrainedExceptionType, exceptionType))
                        {
                            return constrainedExceptionType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        }

                        return null;
                    }

                    if (IsExceptionTypeOrDerived(parameterType, exceptionType)
                        && !SymbolEqualityComparer.Default.Equals(parameterType, exceptionType))
                    {
                        return parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }

                    break;
                }
                case IPropertySymbol property:
                {
                    if (TryGetActionArgumentType(property.Type, out ITypeSymbol actionArgType)
                        && IsExceptionTypeOrDerived(actionArgType, exceptionType)
                        && !SymbolEqualityComparer.Default.Equals(actionArgType, exceptionType))
                    {
                        return actionArgType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }

                    break;
                }
                case IFieldSymbol field:
                {
                    if (TryGetActionArgumentType(field.Type, out ITypeSymbol actionArgType)
                        && IsExceptionTypeOrDerived(actionArgType, exceptionType)
                        && !SymbolEqualityComparer.Default.Equals(actionArgType, exceptionType))
                    {
                        return actionArgType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }

                    break;
                }
            }
        }

        return null;
    }

    private static bool IsValidCatchHandler(INamedTypeSymbol containingType, string catchName, Compilation compilation)
    {
        INamedTypeSymbol? exceptionType = compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionType is null)
            return false;

        foreach (ISymbol member in containingType.GetMembers(catchName))
        {
            switch (member)
            {
                case IMethodSymbol { Parameters.Length: 1 } method:
                {
                    ITypeSymbol parameterType = method.Parameters[0].Type;
                    if (parameterType is ITypeParameterSymbol typeParameter)
                    {
                        if (typeParameter.ConstraintTypes.Any(constraint => IsExceptionTypeOrDerived(constraint, exceptionType)))
                        {
                            return true;
                        }
                    }
                    else if (IsExceptionTypeOrDerived(parameterType, exceptionType))
                    {
                        return true;
                    }

                    break;
                }
                case IPropertySymbol property:
                {
                    if (TryGetActionArgumentType(property.Type, out ITypeSymbol actionArgType)
                        && IsExceptionTypeOrDerived(actionArgType, exceptionType))
                    {
                        return true;
                    }

                    break;
                }
                case IFieldSymbol field:
                {
                    if (TryGetActionArgumentType(field.Type, out ITypeSymbol actionArgType)
                        && IsExceptionTypeOrDerived(actionArgType, exceptionType))
                    {
                        return true;
                    }

                    break;
                }
            }
        }

        return false;
    }

    private static bool TryGetActionArgumentType(ITypeSymbol type, out ITypeSymbol argumentType)
    {
        if (type is INamedTypeSymbol namedType
            && namedType.Name == "Action"
            && namedType.ContainingNamespace.ToDisplayString() == "System"
            && namedType.TypeArguments.Length == 1)
        {
            argumentType = namedType.TypeArguments[0];
            return true;
        }

        argumentType = null!;
        return false;
    }

    private static bool IsExceptionTypeOrDerived(ITypeSymbol type, INamedTypeSymbol exceptionType)
    {
        if (type is not INamedTypeSymbol namedType)
            return false;

        for (INamedTypeSymbol? current = namedType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, exceptionType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SupportsFieldKeyword(GeneratorAttributeSyntaxContext context)
    {
#if ROSLYN_4_12_0_OR_GREATER
        var parseOptions = (CSharpParseOptions)context.SemanticModel.SyntaxTree.Options;
        // field keyword is stable in C# 14 (1400), preview in C# 13 (Roslyn 4.12+)
        return (int)parseOptions.LanguageVersion >= 1400;
#else
        return false;
#endif
    }

    private static string GetCommandName(string methodName)
    {
        if (methodName.EndsWith("Async"))
            methodName = methodName.Substring(0, methodName.Length - 5);
        return methodName + "Command";
    }

    private static string GenerateCommandSource(CommandGenerationInfo info)
    {
        StringBuilder sb = new();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        string ns = info.Hierarchy.Namespace;
        if (ns is not "")
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        // Open type hierarchy
        foreach (Models.TypeInfo typeInfo in info.Hierarchy.Hierarchy.AsImmutableArray().Reverse())
        {
            string keyword = typeInfo.IsRecord ? "record" : typeInfo.Kind switch
            {
                TypeKind.Class => "class",
                TypeKind.Struct => "struct",
                _ => "class"
            };
            sb.AppendLine($"    partial {keyword} {typeInfo.QualifiedName}");
            sb.AppendLine("    {");
        }

        // Determine command type
        string commandType;
        string initialization;

        if (info.IsAsync)
        {
            if (info.ParameterType is not null)
            {
                commandType = $"global::Prism.Commands.AsyncDelegateCommand<{info.ParameterType}>";
                initialization = info.CanExecute is not null
                    ? $"new {commandType}({info.MethodName}, {info.CanExecute})"
                    : $"new {commandType}({info.MethodName})";
            }
            else
            {
                commandType = "global::Prism.Commands.AsyncDelegateCommand";
                initialization = info.CanExecute is not null
                    ? $"new {commandType}({info.MethodName}, {info.CanExecute})"
                    : $"new {commandType}({info.MethodName})";
            }
        }
        else
        {
            if (info.ParameterType is not null)
            {
                commandType = $"global::Prism.Commands.DelegateCommand<{info.ParameterType}>";
                initialization = info.CanExecute is not null
                    ? $"new {commandType}({info.MethodName}, {info.CanExecute})"
                    : $"new {commandType}({info.MethodName})";
            }
            else
            {
                commandType = "global::Prism.Commands.DelegateCommand";
                initialization = info.CanExecute is not null
                    ? $"new {commandType}({info.MethodName}, {info.CanExecute})"
                    : $"new {commandType}({info.MethodName})";
            }
        }

        // Append fluent builder calls for async commands
        bool hasFluentCalls = info.IsAsync && (
            info.EnableParallelExecution ||
            info.CancelAfterMicroseconds is not null ||
            info.CancellationTokenSourceFactory is not null ||
            info.Catch is not null);

        bool hasObservesProperty = info.ObservesProperties.AsImmutableArray().Length > 0;

        if (hasFluentCalls || hasObservesProperty)
        {
            StringBuilder fluentSb = new();
            fluentSb.Append(initialization);

            if (info.EnableParallelExecution)
                fluentSb.Append("\n                .EnableParallelExecution()");

            if (info.CancelAfterMicroseconds is { } microseconds)
            {
                string timespan = $"global::System.TimeSpan.FromMicroseconds({microseconds})";
                fluentSb.Append($"\n                .CancelAfter({timespan})");
            }

            if (info.CancellationTokenSourceFactory is { } ctsFactory)
                fluentSb.Append($"\n                .CancellationTokenSourceFactory({ctsFactory})");

            if (info.Catch is { } catchHandler)
            {
                if (info.CatchType is { } catchType)
                {
                    fluentSb.Append($"\n                .Catch<{catchType}>({catchHandler})");
                }
                else
                {
                    fluentSb.Append($"\n                .Catch({catchHandler})");
                }
            }

            foreach (string prop in info.ObservesProperties.AsImmutableArray())
            {
                fluentSb.Append($"\n                .ObservesProperty(() => {prop})");
            }

            initialization = fluentSb.ToString();
        }

        if (info.UseFieldKeyword)
        {
            sb.AppendLine($"        public {commandType} {info.CommandName} => field ??= {initialization};");
        }
        else
        {
            string fieldName = $"_{char.ToLowerInvariant(info.CommandName[0])}{info.CommandName.Substring(1)}";
            sb.AppendLine($"        private {commandType}? {fieldName};");
            sb.AppendLine();
            sb.AppendLine($"        public {commandType} {info.CommandName} => {fieldName} ??= {initialization};");
        }

        // Close type hierarchy
        foreach (Models.TypeInfo _ in info.Hierarchy.Hierarchy.AsImmutableArray())
        {
            sb.AppendLine("    }");
        }

        if (ns is not "")
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Prism assemblies may expose <c>AsyncDelegateCommand</c> metadata that is not callable from user code.
    /// Require both arity forms to be accessible from the compilation assembly (matches Prism 9 + MvvmAIO.Prism.Bcl.Commands).
    /// </summary>
    private static bool HasConsumerVisibleAsyncDelegateCommandTypes(Compilation compilation)
    {
        return compilation.HasAccessibleTypeWithMetadataName("Prism.Commands.AsyncDelegateCommand")
            && compilation.HasAccessibleTypeWithMetadataName("Prism.Commands.AsyncDelegateCommand`1");
    }
}

