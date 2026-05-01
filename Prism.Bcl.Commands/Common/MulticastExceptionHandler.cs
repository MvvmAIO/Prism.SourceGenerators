using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Prism.Common;

/// <summary>
/// Multicast exception routing aligned with Prism 9 async command infrastructure (MIT reference implementation).
/// </summary>
public readonly struct MulticastExceptionHandler
{
    private readonly Dictionary<Type, MulticastDelegate> _handlers;

    /// <summary>
    /// Initializes a new MulticastExceptionHandler.
    /// </summary>
    public MulticastExceptionHandler()
    {
        _handlers = new Dictionary<Type, MulticastDelegate>();
    }

    /// <summary>
    /// Registers a callback to handle the specified exception.
    /// </summary>
    public void Register<TException>(MulticastDelegate callback)
        where TException : Exception
    {
        _handlers.Add(typeof(TException), callback);
    }

    /// <summary>
    /// Determines if there is a callback registered to handle the specified exception.
    /// </summary>
    public bool CanHandle(Exception exception) =>
        GetDelegate(exception.GetType()) is not null;

    /// <summary>
    /// Handles a specified exception.
    /// </summary>
    public async void Handle(Exception exception, object? parameter = null) =>
        await HandleAsync(exception, parameter).ConfigureAwait(false);

    /// <summary>
    /// Handles a specified exception asynchronously with a given optional parameter.
    /// </summary>
    public async Task HandleAsync(Exception exception, object? parameter = null)
    {
        MulticastDelegate? multicastDelegate = GetDelegate(exception.GetType());
        if (multicastDelegate is null)
            return;

        MethodInfo? invokeMethod = multicastDelegate.GetType().GetMethod("Invoke")
            ?? throw new InvalidOperationException($"Could not find Invoke() method for delegate of type {multicastDelegate.GetType().Name}", exception);

        ParameterInfo[] parameters = invokeMethod.GetParameters();
        object?[] arguments = parameters.Length switch
        {
            0 => Array.Empty<object?>(),
            1 => typeof(Exception).IsAssignableFrom(parameters[0].ParameterType) ? new object?[] { exception } : new object?[] { parameter },
            2 => typeof(Exception).IsAssignableFrom(parameters[0].ParameterType)
                ? new object?[] { exception, parameter }
                : new object?[] { parameter, exception },
            _ => throw new InvalidOperationException($"Handler of type {multicastDelegate.GetType().Name} is not supported", exception)
        };

        object? result = invokeMethod.Invoke(multicastDelegate, arguments);

        if (result is Task task)
            await task.ConfigureAwait(false);
#if NET6_0_OR_GREATER
        else if (result is ValueTask valueTask)
            await valueTask.ConfigureAwait(false);
#endif
    }

    private MulticastDelegate? GetDelegate(Type? type)
    {
        if (type is null)
            return null;
        if (_handlers.ContainsKey(type))
            return _handlers[type];
        return GetDelegate(type.BaseType);
    }
}
