using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

/// <summary>
/// Roslyn syntax for <c>[BindableValidator]</c> companion partials.
/// </summary>
internal static class BindableValidatorSyntax
{
    private static readonly Lazy<ImmutableArray<MemberDeclarationSyntax>> InlineFullMembersLazy = new(ParseInlineFullMembers);
    private static readonly Lazy<ImmutableArray<MemberDeclarationSyntax>> InlineValidationOnlyMembersLazy = new(ParseInlineValidationOnlyMembers);

    public static CompilationUnitSyntax CreateCompilationUnit(BindableValidatorGenerationInfo info)
    {
        return info.EmitMode switch
        {
            BindableValidatorEmitMode.InheritBindableValidator => CreateInherit(info.Hierarchy),
            BindableValidatorEmitMode.InlineFull => info.Hierarchy.GetCompilationUnit(
                InlineFullMembersLazy.Value,
                CreateInterfaceBaseList(inpc: true, indei: true)),
            BindableValidatorEmitMode.InlineValidationOnly => info.Hierarchy.GetCompilationUnit(
                InlineValidationOnlyMembersLazy.Value,
                CreateInterfaceBaseList(inpc: false, indei: true)),
            _ => throw new ArgumentOutOfRangeException(nameof(info)),
        };
    }

    private static CompilationUnitSyntax CreateInherit(HierarchyInfo hierarchy)
    {
        BaseListSyntax baseList = BaseList(
            SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(ParseTypeName("global::Prism.SourceGenerators.BindableValidator"))));

        return hierarchy.GetCompilationUnit(ImmutableArray<MemberDeclarationSyntax>.Empty, baseList);
    }

    private static BaseListSyntax CreateInterfaceBaseList(bool inpc, bool indei)
    {
        if (inpc && indei)
        {
            return BaseList(
                SeparatedList<BaseTypeSyntax>(
                    new SyntaxNodeOrToken[]
                    {
                        SimpleBaseType(ParseTypeName("global::System.ComponentModel.INotifyPropertyChanged")),
                        Token(SyntaxKind.CommaToken),
                        SimpleBaseType(ParseTypeName("global::System.ComponentModel.INotifyDataErrorInfo")),
                    }));
        }

        if (indei)
        {
            return BaseList(
                SingletonSeparatedList<BaseTypeSyntax>(
                    SimpleBaseType(ParseTypeName("global::System.ComponentModel.INotifyDataErrorInfo"))));
        }

        throw new ArgumentException("At least one interface must be requested.");
    }

    private static ImmutableArray<MemberDeclarationSyntax> ParseInlineFullMembers() =>
        ParseMembersFromClassBody(
            """
            private readonly global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>> __psg_errors = new();

            private int __psg_totalErrors;

            private global::System.ComponentModel.DataAnnotations.ValidationContext? __psg_vctx;

            private global::System.ComponentModel.DataAnnotations.ValidationContext __psg_ValidationContext =>
                __psg_vctx ??= new global::System.ComponentModel.DataAnnotations.ValidationContext(this);

            /// <inheritdoc cref="global::System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>
            public event global::System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

            /// <inheritdoc cref="global::System.ComponentModel.INotifyDataErrorInfo.ErrorsChanged"/>
            public event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>? ErrorsChanged;

            public bool HasErrors => __psg_totalErrors > 0;

            protected bool SetProperty<T>(ref T storage, T value, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            {
                if (global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(storage, value))
                {
                    return false;
                }

                storage = value;
                RaisePropertyChanged(propertyName);
                return true;
            }

            protected bool SetProperty<T>(ref T storage, T value, global::System.Action? onChanged, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            {
                if (global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(storage, value))
                {
                    return false;
                }

                storage = value;
                onChanged?.Invoke();
                RaisePropertyChanged(propertyName);
                return true;
            }

            protected void RaisePropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            {
                OnPropertyChanged(new global::System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }

            protected virtual void OnPropertyChanged(global::System.ComponentModel.PropertyChangedEventArgs args)
            {
                PropertyChanged?.Invoke(this, args);
            }

            public global::System.Collections.IEnumerable GetErrors(string? propertyName)
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    return __psg_GetAllErrors();
                }

                if (__psg_errors.TryGetValue(propertyName!, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? propertyErrors))
                {
                    return propertyErrors;
                }

                return global::System.Array.Empty<global::System.ComponentModel.DataAnnotations.ValidationResult>();
            }

            public void ValidateProperty(object? value, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            {
                if (propertyName is null)
                {
                    throw new global::System.ArgumentNullException(nameof(propertyName));
                }

                global::System.ComponentModel.DataAnnotations.ValidationContext ctx = __psg_ValidationContext;
                ctx.MemberName = propertyName;
                ctx.DisplayName = __psg_GetDisplayNameForProperty(propertyName);

                global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult> propertyErrors = new();
                bool isValid = global::System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(value, ctx, propertyErrors);

                bool hadErrors = __psg_errors.TryGetValue(propertyName, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? previousErrors)
                                 && previousErrors!.Count > 0;

                if (isValid)
                {
                    if (hadErrors)
                    {
                        __psg_errors.Remove(propertyName);
                        __psg_totalErrors--;
                        __psg_OnErrorsChanged(propertyName);
                    }
                }
                else
                {
                    if (!hadErrors)
                    {
                        __psg_totalErrors++;
                    }

                    __psg_errors[propertyName] = propertyErrors;
                    __psg_OnErrorsChanged(propertyName);
                }
            }

            public void ValidateAllProperties()
            {
                global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult> validationResults = new();
                bool isValid = global::System.ComponentModel.DataAnnotations.Validator.TryValidateObject(this, __psg_ValidationContext, validationResults, true);

                global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>> newErrors = new();
                foreach (global::System.ComponentModel.DataAnnotations.ValidationResult result in validationResults)
                {
                    foreach (string memberName in result.MemberNames)
                    {
                        if (!newErrors.TryGetValue(memberName, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? list))
                        {
                            list = new global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>();
                            newErrors[memberName] = list;
                        }

                        list.Add(result);
                    }
                }

                global::System.Collections.Generic.HashSet<string> allPropertyNames = new(__psg_errors.Keys);
                foreach (string key in newErrors.Keys)
                {
                    allPropertyNames.Add(key);
                }

                global::System.Collections.Generic.Dictionary<string, bool> hadErrorsSnapshot = new();
                foreach (string propName in allPropertyNames)
                {
                    hadErrorsSnapshot[propName] = __psg_errors.TryGetValue(propName, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? oldList) && oldList!.Count > 0;
                }

                __psg_errors.Clear();
                foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>> kvp in newErrors)
                {
                    __psg_errors[kvp.Key] = kvp.Value;
                }

                foreach (string propName in allPropertyNames)
                {
                    bool hadPropErrors = hadErrorsSnapshot[propName];
                    bool hasPropErrors = newErrors.TryGetValue(propName, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? newList) && newList!.Count > 0;

                    if (hasPropErrors && !hadPropErrors)
                    {
                        __psg_totalErrors++;
                        __psg_OnErrorsChanged(propName);
                    }
                    else if (!hasPropErrors && hadPropErrors)
                    {
                        __psg_totalErrors--;
                        __psg_OnErrorsChanged(propName);
                    }
                    else if (hadPropErrors && hasPropErrors)
                    {
                        __psg_OnErrorsChanged(propName);
                    }
                }
            }

            public void ClearAllErrors()
            {
                if (__psg_totalErrors == 0)
                {
                    return;
                }

                string[] propertyNames = __psg_errors.Keys.ToArray();
                __psg_errors.Clear();
                __psg_totalErrors = 0;

                foreach (string propName in propertyNames)
                {
                    __psg_OnErrorsChanged(propName);
                }
            }

            public void ClearErrors(string? propertyName = null)
            {
                if (propertyName is null)
                {
                    ClearAllErrors();
                    return;
                }

                if (__psg_errors.Remove(propertyName))
                {
                    __psg_totalErrors--;
                    __psg_OnErrorsChanged(propertyName);
                }
            }

            private void __psg_OnErrorsChanged(string propertyName)
            {
                ErrorsChanged?.Invoke(this, new global::System.ComponentModel.DataErrorsChangedEventArgs(propertyName));
                RaisePropertyChanged(nameof(HasErrors));
            }

            private string __psg_GetDisplayNameForProperty(string propertyName)
            {
                global::System.Type type = GetType();

                global::System.Reflection.PropertyInfo? propertyInfo = type.GetProperty(propertyName);
                if (propertyInfo is not null)
                {
                    global::System.ComponentModel.DataAnnotations.DisplayAttribute? displayAttribute =
                        (global::System.ComponentModel.DataAnnotations.DisplayAttribute?)global::System.Attribute.GetCustomAttribute(propertyInfo, typeof(global::System.ComponentModel.DataAnnotations.DisplayAttribute));
                    if (displayAttribute is not null)
                    {
                        return displayAttribute.GetName() ?? propertyName;
                    }
                }

                return propertyName;
            }

            private global::System.Collections.Generic.IEnumerable<global::System.ComponentModel.DataAnnotations.ValidationResult> __psg_GetAllErrors()
            {
                return __psg_errors.Values.SelectMany(static e => e);
            }
            """);

    private static ImmutableArray<MemberDeclarationSyntax> ParseInlineValidationOnlyMembers() =>
        ParseMembersFromClassBody(
            """
            private readonly global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>> __psg_errors = new();

            private int __psg_totalErrors;

            private global::System.ComponentModel.DataAnnotations.ValidationContext? __psg_vctx;

            private global::System.ComponentModel.DataAnnotations.ValidationContext __psg_ValidationContext =>
                __psg_vctx ??= new global::System.ComponentModel.DataAnnotations.ValidationContext(this);

            /// <inheritdoc cref="global::System.ComponentModel.INotifyDataErrorInfo.ErrorsChanged"/>
            public event global::System.EventHandler<global::System.ComponentModel.DataErrorsChangedEventArgs>? ErrorsChanged;

            public bool HasErrors => __psg_totalErrors > 0;

            public global::System.Collections.IEnumerable GetErrors(string? propertyName)
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    return __psg_GetAllErrors();
                }

                if (__psg_errors.TryGetValue(propertyName!, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? propertyErrors))
                {
                    return propertyErrors;
                }

                return global::System.Array.Empty<global::System.ComponentModel.DataAnnotations.ValidationResult>();
            }

            public void ValidateProperty(object? value, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
            {
                if (propertyName is null)
                {
                    throw new global::System.ArgumentNullException(nameof(propertyName));
                }

                global::System.ComponentModel.DataAnnotations.ValidationContext ctx = __psg_ValidationContext;
                ctx.MemberName = propertyName;
                ctx.DisplayName = __psg_GetDisplayNameForProperty(propertyName);

                global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult> propertyErrors = new();
                bool isValid = global::System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(value, ctx, propertyErrors);

                bool hadErrors = __psg_errors.TryGetValue(propertyName, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? previousErrors)
                                 && previousErrors!.Count > 0;

                if (isValid)
                {
                    if (hadErrors)
                    {
                        __psg_errors.Remove(propertyName);
                        __psg_totalErrors--;
                        __psg_OnErrorsChanged(propertyName);
                    }
                }
                else
                {
                    if (!hadErrors)
                    {
                        __psg_totalErrors++;
                    }

                    __psg_errors[propertyName] = propertyErrors;
                    __psg_OnErrorsChanged(propertyName);
                }
            }

            public void ValidateAllProperties()
            {
                global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult> validationResults = new();
                bool isValid = global::System.ComponentModel.DataAnnotations.Validator.TryValidateObject(this, __psg_ValidationContext, validationResults, true);

                global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>> newErrors = new();
                foreach (global::System.ComponentModel.DataAnnotations.ValidationResult result in validationResults)
                {
                    foreach (string memberName in result.MemberNames)
                    {
                        if (!newErrors.TryGetValue(memberName, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? list))
                        {
                            list = new global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>();
                            newErrors[memberName] = list;
                        }

                        list.Add(result);
                    }
                }

                global::System.Collections.Generic.HashSet<string> allPropertyNames = new(__psg_errors.Keys);
                foreach (string key in newErrors.Keys)
                {
                    allPropertyNames.Add(key);
                }

                global::System.Collections.Generic.Dictionary<string, bool> hadErrorsSnapshot = new();
                foreach (string propName in allPropertyNames)
                {
                    hadErrorsSnapshot[propName] = __psg_errors.TryGetValue(propName, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? oldList) && oldList!.Count > 0;
                }

                __psg_errors.Clear();
                foreach (global::System.Collections.Generic.KeyValuePair<string, global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>> kvp in newErrors)
                {
                    __psg_errors[kvp.Key] = kvp.Value;
                }

                foreach (string propName in allPropertyNames)
                {
                    bool hadPropErrors = hadErrorsSnapshot[propName];
                    bool hasPropErrors = newErrors.TryGetValue(propName, out global::System.Collections.Generic.List<global::System.ComponentModel.DataAnnotations.ValidationResult>? newList) && newList!.Count > 0;

                    if (hasPropErrors && !hadPropErrors)
                    {
                        __psg_totalErrors++;
                        __psg_OnErrorsChanged(propName);
                    }
                    else if (!hasPropErrors && hadPropErrors)
                    {
                        __psg_totalErrors--;
                        __psg_OnErrorsChanged(propName);
                    }
                    else if (hadPropErrors && hasPropErrors)
                    {
                        __psg_OnErrorsChanged(propName);
                    }
                }
            }

            public void ClearAllErrors()
            {
                if (__psg_totalErrors == 0)
                {
                    return;
                }

                string[] propertyNames = __psg_errors.Keys.ToArray();
                __psg_errors.Clear();
                __psg_totalErrors = 0;

                foreach (string propName in propertyNames)
                {
                    __psg_OnErrorsChanged(propName);
                }
            }

            public void ClearErrors(string? propertyName = null)
            {
                if (propertyName is null)
                {
                    ClearAllErrors();
                    return;
                }

                if (__psg_errors.Remove(propertyName))
                {
                    __psg_totalErrors--;
                    __psg_OnErrorsChanged(propertyName);
                }
            }

            private void __psg_OnErrorsChanged(string propertyName)
            {
                ErrorsChanged?.Invoke(this, new global::System.ComponentModel.DataErrorsChangedEventArgs(propertyName));
                RaisePropertyChanged(nameof(HasErrors));
            }

            private string __psg_GetDisplayNameForProperty(string propertyName)
            {
                global::System.Type type = GetType();

                global::System.Reflection.PropertyInfo? propertyInfo = type.GetProperty(propertyName);
                if (propertyInfo is not null)
                {
                    global::System.ComponentModel.DataAnnotations.DisplayAttribute? displayAttribute =
                        (global::System.ComponentModel.DataAnnotations.DisplayAttribute?)global::System.Attribute.GetCustomAttribute(propertyInfo, typeof(global::System.ComponentModel.DataAnnotations.DisplayAttribute));
                    if (displayAttribute is not null)
                    {
                        return displayAttribute.GetName() ?? propertyName;
                    }
                }

                return propertyName;
            }

            private global::System.Collections.Generic.IEnumerable<global::System.ComponentModel.DataAnnotations.ValidationResult> __psg_GetAllErrors()
            {
                return __psg_errors.Values.SelectMany(static e => e);
            }
            """);

    private static ImmutableArray<MemberDeclarationSyntax> ParseMembersFromClassBody(string classBodyMembers)
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        string wrapped =
            """
            #nullable enable
            partial class __PsgBindableValidatorTemplate
            {
            """ + classBodyMembers + """
            }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(wrapped, options);
        ClassDeclarationSyntax cls = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        return ImmutableArray.CreateRange(cls.Members);
    }
}
