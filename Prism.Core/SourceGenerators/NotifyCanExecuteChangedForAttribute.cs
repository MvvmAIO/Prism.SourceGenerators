namespace Prism.SourceGenerators;

/// <summary>
/// An attribute that can be applied to a field or partial property with <c>[ObservableProperty]</c>
/// to indicate that the generated property setter should also raise <c>RaiseCanExecuteChanged</c>
/// on the specified <see cref="System.Windows.Input.ICommand"/>-typed members after the value changes.
/// <para>
/// <code>
/// [ObservableProperty]
/// [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
/// private string _firstName;
///
/// [DelegateCommand(CanExecute = nameof(CanSave))]
/// private void Save() { /* ... */ }
/// </code>
/// The generated setter for <c>FirstName</c> will call
/// <c>SaveCommand?.RaiseCanExecuteChanged()</c> after raising <c>PropertyChanged</c>.
/// </para>
/// <para>
/// The referenced name should match the generated command property name produced by
/// <c>[DelegateCommand]</c>/<c>[AsyncDelegateCommand]</c> (e.g. <c>SaveCommand</c> for a method
/// <c>Save</c>), or any existing field/property exposing a <c>RaiseCanExecuteChanged()</c> method.
/// </para>
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Field | global::System.AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class NotifyCanExecuteChangedForAttribute : global::System.Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyCanExecuteChangedForAttribute"/> class.
    /// </summary>
    /// <param name="commandName">The name of the command property to call <c>RaiseCanExecuteChanged()</c> on.</param>
    public NotifyCanExecuteChangedForAttribute(string commandName)
    {
        CommandNames = new[] { commandName };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyCanExecuteChangedForAttribute"/> class.
    /// </summary>
    /// <param name="commandName">The name of the first command property.</param>
    /// <param name="otherCommandNames">The names of additional command properties.</param>
    public NotifyCanExecuteChangedForAttribute(string commandName, params string[] otherCommandNames)
    {
        var names = new string[otherCommandNames.Length + 1];
        names[0] = commandName;
        otherCommandNames.CopyTo(names, 1);
        CommandNames = names;
    }

    /// <summary>
    /// Gets the command names whose <c>RaiseCanExecuteChanged()</c> should be invoked.
    /// </summary>
    public string[] CommandNames { get; }
}
