namespace Prism.SourceGenerators;

/// <summary>
/// An attribute that can be applied to a method to generate a <c>DelegateCommand</c>
/// or <c>AsyncDelegateCommand</c> property.
/// <para>
/// For synchronous methods (<c>void</c>), generates <c>DelegateCommand</c> or <c>DelegateCommand&lt;T&gt;</c>.
/// For asynchronous methods (<c>Task</c>), generates <c>AsyncDelegateCommand</c> or <c>AsyncDelegateCommand&lt;T&gt;</c>.
/// </para>
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class DelegateCommandAttribute : global::System.Attribute
{
    /// <summary>
    /// Gets or sets the name of the generated command property.
    /// If not specified, the name is derived from the method name
    /// (e.g., <c>Submit</c> becomes <c>SubmitCommand</c>).
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// Gets or sets the name of the <c>CanExecute</c> method or property.
    /// </summary>
    public string? CanExecute { get; set; }
}