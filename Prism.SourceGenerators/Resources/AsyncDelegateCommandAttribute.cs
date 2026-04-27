using System;
using System.Collections.Generic;
using System.Text;

namespace Prism.SourceGenerators.Resources;

/// <summary>
/// 如果在同一个方法上多次使用此特性，需要保证CommandName不同
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class AsyncDelegateCommandAttribute : Attribute
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

    /// <summary>
    /// <see cref="AsyncDelegateCommand.CancelAfter(TimeSpan)"/>
    /// </summary>
    public double? CancelAfterMicroseconds { get; set; }


    /// <summary>
    /// <see cref="AsyncDelegateCommand.Catch(Action{Exception})"/>
    /// <see cref="AsyncDelegateCommand.Catch{TException}(Action{TException})"/>
    /// 允许是方法、字段、属性
    /// 如果是方法,应该包含一个参数 ,类型为Exception或其子类,用于接收命令执行过程中抛出的异常
    /// 如果是字段或属性,应该是一个Action<Exception>类型的委托,用于处理命令执行过程中抛出的异常
    /// </summary>
    public string? Catch { get; set; }

    /// <summary>
    /// <see cref="AsyncDelegateCommand.CancellationTokenSourceFactory(Func{CancellationToken})"/>
    /// </summary>
    public string? CancellationTokenSourceFactory { get; set; }

    /// <summary>
    /// <see cref="AsyncDelegateCommand.EnableParallelExecution"/>
    /// </summary>
    public bool EnableParallelExecution { get; set; }

}