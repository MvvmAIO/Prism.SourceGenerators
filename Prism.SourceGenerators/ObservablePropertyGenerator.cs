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
/// A source generator that generates observable properties for classes inheriting from <c>Prism.Mvvm.BindableBase</c>.
/// <para>
/// Attributes are supplied by the <c>MvvmAIO.Prism.Core</c> assembly (referenced by the <c>MvvmAIO.Prism.Prism.SourceGenerators</c> NuGet package).
/// Supports two usage modes:
/// <list type="bullet">
/// <item><b>Field target</b> (all C# versions): Apply <c>[ObservableProperty]</c> to a private field to generate
/// a public property that calls <c>SetProperty</c> in the setter.</item>
/// <item><b>Partial property target</b> (C# 13+): Apply <c>[ObservableProperty]</c> to a <c>partial</c> property
/// to generate the implementing declaration using the <c>field</c> keyword (semi-auto property).</item>
/// </list>
/// </para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ObservablePropertyGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Prism.SourceGenerators.ObservablePropertyAttribute";
    private const string NotifyPropertyChangedForAttributeName = "Prism.SourceGenerators.NotifyPropertyChangedForAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // --- Pipeline 1: Field targets (traditional, all C# versions) ---
        IncrementalValuesProvider<Result<PropertyGenerationInfo>> fieldInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    static (node, _) => node is VariableDeclaratorSyntax
                    {
                        Parent: VariableDeclarationSyntax
                        {
                            Parent: FieldDeclarationSyntax
                            {
                                Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                            }
                        }
                    },
                    static (context, token) => ExtractFieldInfo(context, token));

        RegisterDiagnosticsAndSource(context, fieldInfos);

        // --- Pipeline 2: Property targets (partial property + field keyword, C# 13+) ---
        IncrementalValuesProvider<Result<PropertyGenerationInfo>> propertyInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    static (node, _) => node is PropertyDeclarationSyntax
                    {
                        Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                    },
                    static (context, token) => ExtractPropertyInfo(context, token));

        RegisterDiagnosticsAndSource(context, propertyInfos);
    }

    private static void RegisterDiagnosticsAndSource(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<Result<PropertyGenerationInfo>> infos)
    {
        // Report diagnostics
        context.RegisterSourceOutput(
            infos.Where(static item => !item.Errors.IsEmpty),
            static (context, result) =>
            {
                foreach (DiagnosticInfo diagnostic in result.Errors.AsImmutableArray())
                {
                    context.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            });

        // Generate source for valid items
        context.RegisterSourceOutput(
            infos
                .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
                .Select(static (item, _) => item.Value!),
            static (context, info) =>
            {
                string source = GeneratePropertySource(info);

                context.AddSource(
                    $"{info.Hierarchy.FilenameHint}.{info.PropertyName}.g.cs",
                    source);
            });
    }

    private static Result<PropertyGenerationInfo> ExtractFieldInfo(
        GeneratorAttributeSyntaxContext context, System.Threading.CancellationToken token)
    {
        IFieldSymbol fieldSymbol = (IFieldSymbol)context.TargetSymbol;
        INamedTypeSymbol containingType = fieldSymbol.ContainingType;

        bool isPartial = containingType.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(token))
            .OfType<TypeDeclarationSyntax>()
            .Any(static t => t.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isPartial)
        {
            return new Result<PropertyGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.NonPartialClassWithObservableProperty,
                        containingType,
                        containingType.Name)));
        }

        string fieldName = fieldSymbol.Name;
        string propertyName = GetPropertyName(fieldName);
        string fieldType = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        HierarchyInfo hierarchy = HierarchyInfo.From(containingType);
        ImmutableArray<string> notifyProps = CollectNotifyPropertyChangedFor(fieldSymbol);

        return new Result<PropertyGenerationInfo>(
            new PropertyGenerationInfo(hierarchy, fieldName, propertyName, fieldType,
                IsPartialProperty: false, Accessibility.Public, Accessibility.NotApplicable, notifyProps),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    private static Result<PropertyGenerationInfo> ExtractPropertyInfo(
        GeneratorAttributeSyntaxContext context, System.Threading.CancellationToken token)
    {
        IPropertySymbol propertySymbol = (IPropertySymbol)context.TargetSymbol;
        INamedTypeSymbol containingType = propertySymbol.ContainingType;

        // Check containing type is partial
        bool isTypePartial = containingType.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(token))
            .OfType<TypeDeclarationSyntax>()
            .Any(static t => t.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isTypePartial)
        {
            return new Result<PropertyGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.NonPartialClassWithObservableProperty,
                        containingType,
                        containingType.Name)));
        }

        // Check property is partial
        PropertyDeclarationSyntax propertySyntax = (PropertyDeclarationSyntax)context.TargetNode;
        bool isPropertyPartial = propertySyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

        if (!isPropertyPartial)
        {
            return new Result<PropertyGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.NonPartialPropertyWithObservableProperty,
                        propertySymbol,
                        propertySymbol.Name)));
        }

        string propertyName = propertySymbol.Name;
        string fieldType = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        HierarchyInfo hierarchy = HierarchyInfo.From(containingType);

        Accessibility setterAccessibility = propertySymbol.SetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable;
        if (setterAccessibility == propertySymbol.DeclaredAccessibility)
            setterAccessibility = Accessibility.NotApplicable;

        ImmutableArray<string> notifyProps = CollectNotifyPropertyChangedFor(propertySymbol);

        return new Result<PropertyGenerationInfo>(
            new PropertyGenerationInfo(hierarchy, propertyName, propertyName, fieldType,
                IsPartialProperty: true, propertySymbol.DeclaredAccessibility, setterAccessibility, notifyProps),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    private static ImmutableArray<string> CollectNotifyPropertyChangedFor(ISymbol symbol)
    {
        ImmutableArray<string>.Builder? builder = null;

        foreach (AttributeData attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != NotifyPropertyChangedForAttributeName)
                continue;

            if (attr.ConstructorArguments.Length >= 1 && attr.ConstructorArguments[0].Value is string propName)
            {
                builder ??= ImmutableArray.CreateBuilder<string>();
                builder.Add(propName);

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

    private static string GeneratePropertySource(PropertyGenerationInfo info)
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

        string indent = ns is not "" ? "    " : "";

        // Open type hierarchy
        foreach (Models.TypeInfo typeInfo in info.Hierarchy.Hierarchy.AsImmutableArray().Reverse())
        {
            string keyword = typeInfo.IsRecord ? "record" : typeInfo.Kind switch
            {
                TypeKind.Class => "class",
                TypeKind.Struct => "struct",
                _ => "class"
            };
            sb.AppendLine($"{indent}partial {keyword} {typeInfo.QualifiedName}");
            sb.AppendLine($"{indent}{{");
            indent += "    ";
        }

        // OnChanged partial method declarations
        sb.AppendLine($"{indent}partial void On{info.PropertyName}Changed({info.FieldType} value);");
        sb.AppendLine($"{indent}partial void On{info.PropertyName}Changed({info.FieldType} oldValue, {info.FieldType} newValue);");
        sb.AppendLine();

        string backingField = info.IsPartialProperty ? "field" : info.FieldName;
        string accessModifier = GetAccessModifierString(info.DeclaredAccessibility);
        string setterModifier = info.SetterAccessibility != Accessibility.NotApplicable
            ? GetAccessModifierString(info.SetterAccessibility) + " "
            : "";

        if (info.IsPartialProperty)
        {
            // Partial property implementation
            sb.AppendLine($"{indent}{accessModifier} partial {info.FieldType} {info.PropertyName}");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    get => field;");
            sb.AppendLine($"{indent}    {setterModifier}set");
        }
        else
        {
            // Field-backed property
            sb.AppendLine($"{indent}public {info.FieldType} {info.PropertyName}");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    get => {backingField};");
            sb.AppendLine($"{indent}    set");
        }

        // Setter body
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        if (!global::System.Collections.Generic.EqualityComparer<{info.FieldType}>.Default.Equals({backingField}, value))");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            {info.FieldType} oldValue = {backingField};");
        sb.AppendLine($"{indent}            {backingField} = value;");
        sb.AppendLine($"{indent}            On{info.PropertyName}Changed(value);");
        sb.AppendLine($"{indent}            On{info.PropertyName}Changed(oldValue, value);");
        sb.AppendLine($"{indent}            this.RaisePropertyChanged(nameof({info.PropertyName}));");

        // NotifyPropertyChangedFor
        foreach (string notifyProp in info.NotifyPropertyChangedFor.AsImmutableArray())
        {
            sb.AppendLine($"{indent}            this.RaisePropertyChanged(nameof({notifyProp}));");
        }

        sb.AppendLine($"{indent}        }}");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");

        // Close type hierarchy
        indent = indent.Substring(0, indent.Length - 4);
        foreach (Models.TypeInfo _ in info.Hierarchy.Hierarchy.AsImmutableArray())
        {
            sb.AppendLine($"{indent}}}");
            if (indent.Length >= 4) indent = indent.Substring(0, indent.Length - 4);
        }

        if (ns is not "")
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string GetAccessModifierString(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => "public"
        };
    }

    private static string GetPropertyName(string fieldName)
    {
        if (fieldName.StartsWith("m_") && fieldName.Length > 2)
            return char.ToUpperInvariant(fieldName[2]) + fieldName.Substring(3);
        if (fieldName.StartsWith("_") && fieldName.Length > 1)
            return char.ToUpperInvariant(fieldName[1]) + fieldName.Substring(2);
        return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
    }
}

