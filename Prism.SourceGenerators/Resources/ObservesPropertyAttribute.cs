using System;
using System.Collections.Generic;
using System.Text;

namespace Prism.SourceGenerators.Resources;

/// <summary>
/// <see cref="DelegateCommand.ObservesProperty{T}(System.Linq.Expressions.Expression{Func{T}})"/>
/// <see cref="AsyncDelegateCommand.ObservesProperty{T}(System.Linq.Expressions.Expression{Func{T}})"/>
/// </summary>
/// <param name="propertyName"></param>
/// <param name="otherPropertyNames"></param>

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public partial class ObservesPropertyAttribute(string propertyName, params string[] otherPropertyNames) : Attribute
{
    private string PropertyName { get; } = propertyName;
    private string[] OtherPropertyNames { get; } = otherPropertyNames;
}

