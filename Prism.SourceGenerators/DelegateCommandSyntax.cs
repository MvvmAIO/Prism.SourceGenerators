using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

/// <summary>
/// Roslyn syntax for <c>[DelegateCommand]</c> / <c>[AsyncDelegateCommand]</c> generated members.
/// </summary>
internal static class DelegateCommandSyntax
{
    public static CompilationUnitSyntax CreateCompilationUnit(CommandGenerationInfo info)
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        (string commandType, string initialization) = GetCommandTypeAndInitialization(info);

        ImmutableArray<MemberDeclarationSyntax>.Builder members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        if (info.UseFieldKeyword)
        {
            string propertyText =
                $"public {commandType} {info.CommandName} => field ??= {initialization};";
            members.Add(
                ParseMemberDeclaration(propertyText, options: options)
                ?? throw new System.InvalidOperationException("Failed to parse command property."));
        }
        else
        {
            string fieldName = GetBackingFieldName(info.CommandName);
            string fieldDecl = $"private {commandType}? {fieldName};";
            members.Add(
                ParseMemberDeclaration(fieldDecl, options: options)
                ?? throw new System.InvalidOperationException("Failed to parse command backing field."));

            string propertyText =
                $"public {commandType} {info.CommandName} => {fieldName} ??= {initialization};";
            members.Add(
                ParseMemberDeclaration(propertyText, options: options)
                ?? throw new System.InvalidOperationException("Failed to parse command property."));
        }

        return info.Hierarchy.GetCompilationUnit(members.ToImmutable());
    }

    private static string GetBackingFieldName(string commandName) =>
        $"_{char.ToLowerInvariant(commandName[0])}{commandName.Substring(1)}";

    private static (string CommandType, string Initialization) GetCommandTypeAndInitialization(CommandGenerationInfo info)
    {
        string initialization;
        string commandType;

        if (info.IsAsync)
        {
            string executeArg = GetAsyncCommandExecuteArgument(info);
            if (info.ParameterType is not null)
            {
                commandType = $"global::Prism.Commands.AsyncDelegateCommand<{info.ParameterType}>";
                initialization = info.CanExecute is not null
                    ? $"new {commandType}({executeArg}, {info.CanExecute})"
                    : $"new {commandType}({executeArg})";
            }
            else
            {
                commandType = "global::Prism.Commands.AsyncDelegateCommand";
                initialization = info.CanExecute is not null
                    ? $"new {commandType}({executeArg}, {info.CanExecute})"
                    : $"new {commandType}({executeArg})";
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

        bool hasFluentCalls = info.IsAsync && (
            info.EnableParallelExecution ||
            info.CancelAfterMicroseconds is not null ||
            info.CancellationTokenSourceFactory is not null ||
            info.Catch is not null);

        bool hasObservesProperty = !info.ObservesProperties.AsImmutableArray().IsDefaultOrEmpty;

        if (hasFluentCalls || hasObservesProperty)
        {
            StringBuilder fluentSb = new();
            fluentSb.Append(initialization);

            if (info.EnableParallelExecution)
            {
                fluentSb.Append("\n                .EnableParallelExecution()");
            }

            if (info.CancelAfterMicroseconds is { } microseconds)
            {
                string timespan = $"global::System.TimeSpan.FromMicroseconds({microseconds})";
                fluentSb.Append($"\n                .CancelAfter({timespan})");
            }

            if (info.CancellationTokenSourceFactory is { } ctsFactory)
            {
                fluentSb.Append($"\n                .CancellationTokenSourceFactory({ctsFactory})");
            }

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

        return (commandType, initialization);
    }

    private static string GetAsyncCommandExecuteArgument(CommandGenerationInfo info)
    {
        if (!info.WrapAsyncExecuteWithAsTask)
        {
            return info.MethodName;
        }

        return info.ParameterType is null
            ? $"() => {info.MethodName}().AsTask()"
            : $"(__p) => {info.MethodName}(__p).AsTask()";
    }
}
