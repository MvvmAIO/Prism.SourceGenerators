using System;

namespace Prism.SourceGenerators;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterAttribute : Attribute
{
    public string? Name { get; set; } = null;

    public Type? ServiceType { get; set; } = null;

    /// <summary>
    /// 默认是 Transient。
    /// </summary>
    public PrismRegistrationLifetime ServiceLifetime { get; set; } = PrismRegistrationLifetime.Transient;

    public bool IfNotRegistered { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterAttribute<T> : Attribute
{
    public string? Name { get; set; } = null;

    /// <summary>
    /// 默认是 Transient。
    /// </summary>
    public PrismRegistrationLifetime ServiceLifetime { get; set; } = PrismRegistrationLifetime.Transient;

    public bool IfNotRegistered { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterSingletonAttribute : Attribute
{
    public string? Name { get; set; } = null;

    public Type? ServiceType { get; set; } = null;

    public bool IfNotRegistered { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterSingletonAttribute<T> : Attribute
{
    public string? Name { get; set; } = null;

    public bool IfNotRegistered { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterScopedAttribute : Attribute
{
    public string? Name { get; set; } = null;

    public Type? ServiceType { get; set; } = null;

    public bool IfNotRegistered { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterScopedAttribute<T> : Attribute
{
    public string? Name { get; set; } = null;

    public bool IfNotRegistered { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterTransientAttribute : Attribute
{
    public string? Name { get; set; } = null;

    public Type? ServiceType { get; set; } = null;

    public bool IfNotRegistered { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterTransientAttribute<T> : Attribute
{
    public string? Name { get; set; } = null;

    public bool IfNotRegistered { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterForNavigationAttribute : Attribute
{
    public required Type? ViewModelType { get; init; }

    public string? Name { get; set; } = null;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterForNavigationAttribute<TViewModel> : Attribute
{
    public string? Name { get; set; } = null;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterDialogAttribute : Attribute
{
    public required Type? ViewModelType { get; init; }

    public string? Name { get; set; } = null;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterDialogAttribute<TViewModel> : Attribute
{
    public string? Name { get; set; } = null;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class RegisterDialogWindowAttribute : Attribute
{
    public string? Name { get; set; } = null;
}

/// <summary>
/// Registration lifetime for <see cref="RegisterAttribute"/> (distinct from Microsoft.Extensions.DependencyInjection.ServiceLifetime).
/// </summary>
public enum PrismRegistrationLifetime
{
    Transient,
    Scoped,
    Singleton,
}
