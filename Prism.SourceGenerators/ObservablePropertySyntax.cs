using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

/// <summary>
/// Roslyn syntax for <c>[ObservableProperty]</c> generated members (replaces string-built source in
/// <see cref="ObservablePropertyGenerator"/>).
/// </summary>
internal static class ObservablePropertySyntax
{
    public static CompilationUnitSyntax CreateCompilationUnit(
        PropertyGenerationInfo info,
        string accessModifier,
        string setterModifier)
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        ImmutableArray<MemberDeclarationSyntax>.Builder members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        members.Add(ParsePartialMethod($"partial void On{info.PropertyName}Changing({info.FieldType} value);", options));
        members.Add(ParsePartialMethod($"partial void On{info.PropertyName}Changing({info.FieldType} oldValue, {info.FieldType} newValue);", options));
        members.Add(ParsePartialMethod($"partial void On{info.PropertyName}Changed({info.FieldType} value);", options));
        members.Add(ParsePartialMethod($"partial void On{info.PropertyName}Changed({info.FieldType} oldValue, {info.FieldType} newValue);", options));

        string backingField = info.IsPartialProperty ? "field" : info.FieldName;
        string propertyHeader = info.IsPartialProperty
            ? $"{accessModifier} partial {info.FieldType} {info.PropertyName}"
            : $"{accessModifier} {info.FieldType} {info.PropertyName}";

        StringBuilder propertySource = new();
        foreach (string forwarded in info.ForwardedAttributes.AsImmutableArray())
        {
            propertySource.AppendLine(forwarded);
        }

        propertySource.AppendLine($"{propertyHeader}");
        propertySource.AppendLine("{");
        propertySource.AppendLine($"    get => {backingField};");
        propertySource.AppendLine($"    {setterModifier}set");
        propertySource.AppendLine("    {");
        propertySource.AppendLine($"        if (!global::System.Collections.Generic.EqualityComparer<{info.FieldType}>.Default.Equals({backingField}, value))");
        propertySource.AppendLine("        {");
        propertySource.AppendLine($"            {info.FieldType} oldValue = {backingField};");
        propertySource.AppendLine("            if (global::Prism.SourceGenerators.__Internals.FeatureSwitches.EnableINotifyPropertyChangingSupport)");
        propertySource.AppendLine("            {");
        propertySource.AppendLine($"                this.RaisePropertyChanging(nameof({info.PropertyName}));");
        propertySource.AppendLine("            }");
        propertySource.AppendLine();
        propertySource.AppendLine($"            On{info.PropertyName}Changing(value);");
        propertySource.AppendLine($"            On{info.PropertyName}Changing(oldValue, value);");
        propertySource.AppendLine($"            this.SetProperty(ref {backingField}, value, () =>");
        propertySource.AppendLine("            {");
        propertySource.AppendLine($"                On{info.PropertyName}Changed(value);");
        propertySource.AppendLine($"                On{info.PropertyName}Changed(oldValue, value);");
        propertySource.AppendLine("            });");

        foreach (string notifyProp in info.NotifyPropertyChangedFor.AsImmutableArray())
        {
            propertySource.AppendLine($"            this.RaisePropertyChanged(nameof({notifyProp}));");
        }

        foreach (string commandName in info.NotifyCanExecuteChangedFor.AsImmutableArray())
        {
            propertySource.AppendLine($"            {commandName}?.RaiseCanExecuteChanged();");
        }

        if (info.NotifyDataErrorInfo)
        {
            propertySource.AppendLine($"            ValidateProperty(value, nameof({info.PropertyName}));");
        }

        propertySource.AppendLine("        }");
        propertySource.AppendLine("    }");
        propertySource.AppendLine("}");

        members.Add(
            ParseMemberDeclaration(propertySource.ToString(), options: options)
            ?? throw new System.InvalidOperationException("Failed to parse generated observable property."));

        return info.Hierarchy.GetCompilationUnit(members.ToImmutable());
    }

    private static MemberDeclarationSyntax ParsePartialMethod(string text, CSharpParseOptions options)
    {
        MemberDeclarationSyntax? member = ParseMemberDeclaration(text, options: options);
        return member ?? throw new System.InvalidOperationException($"Failed to parse: {text}");
    }
}
