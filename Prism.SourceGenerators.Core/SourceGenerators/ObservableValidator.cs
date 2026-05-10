using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Prism.SourceGenerators;

/// <summary>
/// A base class for objects implementing the <see cref="INotifyDataErrorInfo"/> interface.
/// This class also implements <see cref="INotifyPropertyChanged"/>, providing <c>SetProperty</c> and
/// <c>RaisePropertyChanged</c> helpers compatible with Prism's <c>BindableBase</c> pattern.
/// <para>
/// Properties annotated with <c>[ObservableProperty]</c> and <c>[NotifyDataErrorInfo]</c> will have their
/// generated setters automatically call <see cref="ValidateProperty(object?, string)"/> after setting the value.
/// </para>
/// </summary>
public abstract class ObservableValidator : INotifyPropertyChanged, INotifyDataErrorInfo
{
    /// <summary>
    /// The <see cref="ValidationContext"/> instance currently in use.
    /// </summary>
    private readonly ValidationContext validationContext;

    /// <summary>
    /// The <see cref="Dictionary{TKey,TValue}"/> instance used to store previous validation results.
    /// </summary>
    private readonly Dictionary<string, List<ValidationResult>> errors = new();

    /// <summary>
    /// Indicates the total number of properties with errors (not total errors).
    /// This allows <see cref="HasErrors"/> to operate in O(1).
    /// </summary>
    private int totalErrors;

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc cref="INotifyDataErrorInfo.ErrorsChanged"/>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableValidator"/> class.
    /// </summary>
    protected ObservableValidator()
    {
        validationContext = new ValidationContext(this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableValidator"/> class.
    /// </summary>
    /// <param name="items">A set of key/value pairs to make available to consumers.</param>
    protected ObservableValidator(IDictionary<object, object?>? items)
    {
        validationContext = new ValidationContext(this, items);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableValidator"/> class.
    /// </summary>
    /// <param name="serviceProvider">An <see cref="IServiceProvider"/> instance to make available during validation.</param>
    /// <param name="items">A set of key/value pairs to make available to consumers.</param>
    protected ObservableValidator(IServiceProvider? serviceProvider, IDictionary<object, object?>? items)
    {
        validationContext = new ValidationContext(this, serviceProvider, items);
    }

    /// <inheritdoc cref="INotifyDataErrorInfo.HasErrors"/>
    public bool HasErrors => totalErrors > 0;

    // --- INotifyPropertyChanged support (BindableBase-compatible API) ---

    /// <summary>
    /// Sets the property value and raises <see cref="PropertyChanged"/> if the value has changed.
    /// </summary>
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        RaisePropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets the property value, invokes <paramref name="onChanged"/> after assignment, then raises <see cref="PropertyChanged"/>.
    /// </summary>
    protected bool SetProperty<T>(ref T storage, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        onChanged?.Invoke();
        RaisePropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
    /// </summary>
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, args);
    }

    // --- INotifyDataErrorInfo support ---

    /// <inheritdoc cref="INotifyDataErrorInfo.GetErrors"/>
    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return GetAllErrors();
        }

        if (errors.TryGetValue(propertyName!, out List<ValidationResult>? propertyErrors))
        {
            return propertyErrors;
        }

        return Array.Empty<ValidationResult>();
    }

    /// <summary>
    /// Validates a property with a specified name and a given input value.
    /// If any changes are detected, the <see cref="ErrorsChanged"/> event will be raised.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="propertyName">The name of the property to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertyName"/> is <see langword="null"/>.</exception>
    public void ValidateProperty(object? value, [CallerMemberName] string? propertyName = null)
    {
        if (propertyName is null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        validationContext.MemberName = propertyName;
        validationContext.DisplayName = GetDisplayNameForProperty(propertyName);

        List<ValidationResult> propertyErrors = new();
        bool isValid = Validator.TryValidateProperty(value, validationContext, propertyErrors);

        bool hadErrors = errors.TryGetValue(propertyName, out List<ValidationResult>? previousErrors)
                         && previousErrors!.Count > 0;

        if (isValid)
        {
            if (hadErrors)
            {
                errors.Remove(propertyName);
                totalErrors--;
                OnErrorsChanged(propertyName);
            }
        }
        else
        {
            if (!hadErrors)
            {
                totalErrors++;
            }

            errors[propertyName] = propertyErrors;
            OnErrorsChanged(propertyName);
        }
    }

    /// <summary>
    /// Validates all properties on the current instance, updating the errors collection
    /// and raising <see cref="ErrorsChanged"/> as needed.
    /// </summary>
    public void ValidateAllProperties()
    {
        List<ValidationResult> validationResults = new();
        bool isValid = Validator.TryValidateObject(this, validationContext, validationResults, true);

        Dictionary<string, List<ValidationResult>> newErrors = new();
        foreach (ValidationResult result in validationResults)
        {
            foreach (string memberName in result.MemberNames)
            {
                if (!newErrors.TryGetValue(memberName, out List<ValidationResult>? list))
                {
                    list = new List<ValidationResult>();
                    newErrors[memberName] = list;
                }

                list.Add(result);
            }
        }

        HashSet<string> allPropertyNames = new(errors.Keys);
        foreach (string key in newErrors.Keys)
        {
            allPropertyNames.Add(key);
        }

        foreach (string propName in allPropertyNames)
        {
            bool hadPropErrors = errors.TryGetValue(propName, out List<ValidationResult>? oldList) && oldList!.Count > 0;
            bool hasPropErrors = newErrors.TryGetValue(propName, out List<ValidationResult>? newList) && newList!.Count > 0;

            if (hasPropErrors && !hadPropErrors)
            {
                totalErrors++;
                OnErrorsChanged(propName);
            }
            else if (!hasPropErrors && hadPropErrors)
            {
                totalErrors--;
                OnErrorsChanged(propName);
            }
            else if (hadPropErrors && hasPropErrors)
            {
                OnErrorsChanged(propName);
            }
        }

        errors.Clear();
        foreach (KeyValuePair<string, List<ValidationResult>> kvp in newErrors)
        {
            errors[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Clears all validation errors for all properties and raises the necessary events.
    /// </summary>
    public void ClearAllErrors()
    {
        if (totalErrors == 0)
        {
            return;
        }

        string[] propertyNames = errors.Keys.ToArray();
        errors.Clear();
        totalErrors = 0;

        foreach (string propName in propertyNames)
        {
            OnErrorsChanged(propName);
        }
    }

    /// <summary>
    /// Clears the validation errors for a given property and raises the necessary events.
    /// </summary>
    /// <param name="propertyName">The name of the property to clear errors for.</param>
    public void ClearErrors(string? propertyName = null)
    {
        if (propertyName is null)
        {
            ClearAllErrors();
            return;
        }

        if (errors.Remove(propertyName))
        {
            totalErrors--;
            OnErrorsChanged(propertyName);
        }
    }

    /// <summary>
    /// Raises the <see cref="ErrorsChanged"/> event for the specified property name and
    /// also raises <c>PropertyChanged</c> for <see cref="HasErrors"/>.
    /// </summary>
    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        RaisePropertyChanged(nameof(HasErrors));
    }

    /// <summary>
    /// Gets the display name for a property to use in the <see cref="ValidationContext"/>.
    /// </summary>
    private string GetDisplayNameForProperty(string propertyName)
    {
        Type type = GetType();

        System.Reflection.PropertyInfo? propertyInfo = type.GetProperty(propertyName);
        if (propertyInfo is not null)
        {
            DisplayAttribute? displayAttribute = (DisplayAttribute?)Attribute.GetCustomAttribute(propertyInfo, typeof(DisplayAttribute));
            if (displayAttribute is not null)
            {
                return displayAttribute.GetName() ?? propertyName;
            }
        }

        return propertyName;
    }

    /// <summary>
    /// Aggregates all errors across all properties.
    /// </summary>
    private IEnumerable<ValidationResult> GetAllErrors()
    {
        return errors.Values.SelectMany(static e => e);
    }
}
