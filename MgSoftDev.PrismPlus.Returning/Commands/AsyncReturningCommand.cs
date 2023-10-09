using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;
using MgSoftDev.ReturningCore.Helper;
using Prism.Commands;

namespace MgSoftDev.PrismPlus.Returning.Commands;

public class AsyncReturningCommand : DelegateCommandBase
{
    Func<Task<ReturningCore.Returning>> _ExecuteMethod;
    Func<bool>                          _CanExecuteMethod;
    Action                              _StartAction;
    Action<ReturningCore.Returning>     _EndAction;
    string                              _ErrorName;
    string                              _ErrorCode;
    bool                                _SaveLog;
    ReturningEnums.LogLevel             _LogLevel;
    
    bool _ConfigureAwait = false;

    /// <summary>
    /// Creates a new instance of <see cref="AsyncReturningCommand"/> with the <see cref="Action"/> to invoke on execution.
    /// </summary>
    /// <param name="executeMethod">The <see cref="Action"/> to invoke when <see cref="ICommand.Execute(object)"/> is called.</param>
    public AsyncReturningCommand(Func<Task<ReturningCore.Returning>> executeMethod) : this(executeMethod, ()=>true) { }

    /// <summary>
    /// Creates a new instance of <see cref="AsyncReturningCommand"/> with the <see cref="Action"/> to invoke on execution
    /// and a <see langword="Func" /> to query for determining if the command can execute.
    /// </summary>
    /// <param name="executeMethod">The <see cref="Action"/> to invoke when <see cref="ICommand.Execute"/> is called.</param>
    /// <param name="canExecuteMethod">The <see cref="Func{TResult}"/> to invoke when <see cref="ICommand.CanExecute"/> is called</param>
    public AsyncReturningCommand(Func<Task<ReturningCore.Returning>> executeMethod, Func<bool> canExecuteMethod) : base()
    {
        if (executeMethod == null || canExecuteMethod == null) throw new ArgumentNullException(nameof(executeMethod), "AsyncDelegateCommand Delegates Cannot Be Null");

        _ExecuteMethod    = executeMethod;
        _CanExecuteMethod = canExecuteMethod;
    }


    /// <summary>
    /// Create action to be executed before the command is executed
    /// </summary>
    /// <param name="startAction"></param>
    /// <returns></returns>
    public AsyncReturningCommand StartAction(Action startAction)
    {
        _StartAction = startAction;

        return this;
    }

    /// <summary>
    /// Create action to be executed after the command has completed
    /// </summary>
    /// <param name="endAction"></param>
    /// <returns></returns>
    public AsyncReturningCommand EndAction(Action<ReturningCore.Returning> endAction)
    {
        _EndAction = endAction;

        return this;
    }

   

    /// <summary>
    /// Create action to be executed before the command is executed
    /// </summary>
    /// <param name="startActionAsync"></param>
    /// <returns></returns>
    public AsyncReturningCommand StartAction(Func<bool> canExecuteAction)
    {
        _CanExecuteMethod = canExecuteAction ?? throw new ArgumentNullException(nameof(canExecuteAction), "AsyncDelegateCommand Delegates Cannot Be Null");

        return this;
    }
    /// <summary>
    /// Add a log entry when the command is executed 
    /// </summary>
    /// <param name="errorName"></param>
    /// <param name="errorCode"></param>
    /// <returns></returns>
    public AsyncReturningCommand SaveLog(string errorName = "Unhandled error", ReturningEnums.LogLevel logLevel = ReturningEnums.LogLevel.Error, string errorCode = "ErrorInfo.UnhandledError")
    {
        _SaveLog   = true;
        _ErrorName = errorName;
        _ErrorCode = errorCode;
        _LogLevel  = logLevel;

        return this;
    }
    /// <summary>
    /// Configure await for the command execution 
    /// </summary>
    /// <param name="configureAwait"></param>
    /// <returns></returns>
    public AsyncReturningCommand ConfigureAwait(bool configureAwait =true)
    {
        _ConfigureAwait = configureAwait;

        return this;
    }
    
    ///<summary>
    /// Executes the command.
    ///</summary>
    public async void Execute()
    {
        try
        {
            _StartAction?.Invoke();
        }
        catch ( Exception )
        {
        }

        IsActive = true;
        RaiseCanExecuteChanged();

        var res = await ReturningCore.Returning.TryTask(async ()=>await _ExecuteMethod(),_SaveLog,_ErrorName,_ErrorCode).ConfigureAwait(_ConfigureAwait);

        IsActive = false;
        RaiseCanExecuteChanged();

        try
        {
            _EndAction?.Invoke(res);
        }
        catch ( Exception )
        {
        }
    }

    /// <summary>
    /// Determines if the command can be executed.
    /// </summary>
    /// <returns>Returns <see langword="true"/> if the command can execute,otherwise returns <see langword="false"/>.</returns>
    public bool CanExecute() { return !IsActive && _CanExecuteMethod(); }

    /// <summary>
    /// Handle the internal invocation of <see cref="ICommand.Execute(object)"/>
    /// </summary>
    /// <param name="parameter">Command Parameter</param>
    protected override void Execute(object parameter) { Execute(); }

    /// <summary>
    /// Handle the internal invocation of <see cref="ICommand.CanExecute(object)"/>
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns><see langword="true"/> if the Command Can Execute, otherwise <see langword="false" /></returns>
    protected override bool CanExecute(object parameter) { return CanExecute(); }

    /// <summary>
    /// Observes a property that implements INotifyPropertyChanged, and automatically calls AsyncDelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
    /// </summary>
    /// <typeparam name="T">The object type containing the property specified in the expression.</typeparam>
    /// <param name="propertyExpression">The property expression. Example: ObservesProperty(() => PropertyName).</param>
    /// <returns>The current instance of AsyncDelegateCommand</returns>
    public AsyncReturningCommand ObservesProperty< T >(Expression<Func<T>> propertyExpression)
    {
        ObservesPropertyInternal(propertyExpression);

        return this;
    }

    /// <summary>
    /// Observes a property that is used to determine if this command can execute, and if it implements INotifyPropertyChanged it will automatically call AsyncDelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
    /// </summary>
    /// <param name="canExecuteExpression">The property expression. Example: ObservesCanExecute(() => PropertyName).</param>
    /// <returns>The current instance of AsyncDelegateCommand</returns>
    public AsyncReturningCommand ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        _CanExecuteMethod = canExecuteExpression.Compile();
        ObservesPropertyInternal(canExecuteExpression);

        return this;
    }
}

/// <summary>
/// An <see cref="ICommand"/> whose delegates can be attached for <see cref="Execute(T)"/> and <see cref="CanExecute(T)"/>.
/// </summary>
/// <typeparam name="T">Parameter type.</typeparam>
/// <remarks>
/// The constructor deliberately prevents the use of value types.
/// Because ICommand takes an object, having a value type for T would cause unexpected behavior when CanExecute(null) is called during XAML initialization for command bindings.
/// Using default(T) was considered and rejected as a solution because the implementor would not be able to distinguish between a valid and defaulted values.
/// <para/>
/// Instead, callers should support a value type by using a nullable value type and checking the HasValue property before using the Value property.
/// <example>
///     <code>
/// public MyClass()
/// {
///     this.submitCommand = new DelegateCommand&lt;int?&gt;(this.Submit, this.CanSubmit);
/// }
/// 
/// private bool CanSubmit(int? customerId)
/// {
///     return (customerId.HasValue &amp;&amp; customers.Contains(customerId.Value));
/// }
///     </code>
/// </example>
/// </remarks>
public class AsyncReturningCommand< T > : DelegateCommandBase
{
    readonly Func<T, Task<ReturningCore.Returning>> _ExecuteMethod;
    Func<T, bool>                                   _CanExecuteMethod;
    Action<T>                                       _StartAction;
    Action<T, ReturningCore.Returning>              _EndAction;
   
    string                                          _ErrorName;
    string                                          _ErrorCode;
    bool                                            _SaveLog;
    ReturningEnums.LogLevel                         _LogLevel;
    bool _ConfigureAwait = false;
    /// <summary>
    /// Initializes a new instance of <see cref="AsyncReturningCommand"/>.
    /// </summary>
    /// <param name="executeMethod">Delegate to execute when Execute is called on the command. This can be null to just hook up a CanExecute delegate.</param>
    /// <remarks><see cref="CanExecute(T)"/> will always return true.</remarks>
    public AsyncReturningCommand(Func<T, Task<ReturningCore.Returning>> executeMethod) : this(executeMethod, (o)=>true) { }


    /// <summary>
    /// Initializes a new instance of <see cref="AsyncReturningCommand"/>.
    /// </summary>
    /// <param name="executeMethod">Delegate to execute when Execute is called on the command. This can be null to just hook up a CanExecute delegate.</param>
    /// <param name="canExecuteMethod">Delegate to execute when CanExecute is called on the command. This can be null.</param>
    /// <exception cref="ArgumentNullException">When both <paramref name="executeMethod"/> and <paramref name="canExecuteMethod"/> are <see langword="null" />.</exception>
    public AsyncReturningCommand(Func<T, Task<ReturningCore.Returning>> executeMethod, Func<T, bool> canExecuteMethod) : base()
    {
        if (executeMethod == null || canExecuteMethod == null) throw new ArgumentNullException(nameof(executeMethod), "AsyncDelegateCommand Delegates Cannot Be Null");

        TypeInfo genericTypeInfo = typeof( T ).GetTypeInfo();

        // AsyncDelegateCommand allows object or Nullable<>.  
        // note: Nullable<> is a struct so we cannot use a class constraint.
        if (genericTypeInfo.IsValueType)
        {
            if (( !genericTypeInfo.IsGenericType ) || ( !typeof( Nullable<> ).GetTypeInfo().IsAssignableFrom(genericTypeInfo.GetGenericTypeDefinition().GetTypeInfo()) ))
            {
                throw new InvalidCastException("DelegateCommand Invalid Generic Payload Type");
            }
        }

        _ExecuteMethod    = executeMethod;
        _CanExecuteMethod = canExecuteMethod;
    }


    /// <summary>
    /// Create action to be executed before the command is executed
    /// </summary>
    /// <param name="startAction"></param>
    /// <returns></returns>
    public AsyncReturningCommand<T> StartAction(Action<T> startAction)
    {
        _StartAction = startAction;

        return this;
    }

    /// <summary>
    /// Create action to be executed after the command has completed
    /// </summary>
    /// <param name="endAction"></param>
    /// <returns></returns>
    public AsyncReturningCommand<T> EndAction(Action<T, ReturningCore.Returning> endAction)
    {
        _EndAction = endAction;

        return this;
    }

    


    /// <summary>
    /// Create action to be executed before the command is executed
    /// </summary>
    /// <param name="startActionAsync"></param>
    /// <returns></returns>
    public AsyncReturningCommand<T> StartAction(Func<T, bool> canExecuteAction)
    {
        _CanExecuteMethod = canExecuteAction ?? throw new ArgumentNullException(nameof(canExecuteAction), "AsyncDelegateCommand Delegates Cannot Be Null");

        return this;
    }
    /// <summary>
    /// Add a log entry when the command is executed 
    /// </summary>
    /// <param name="errorName"></param>
    /// <param name="errorCode"></param>
    /// <returns></returns>
    public AsyncReturningCommand<T> SaveLog(string errorName = "Unhandled error", ReturningEnums.LogLevel logLevel = ReturningEnums.LogLevel.Error, string errorCode = "ErrorInfo.UnhandledError")
    {
        _SaveLog   = true;
        _ErrorName = errorName;
        _ErrorCode = errorCode;
        _LogLevel  = logLevel;

        return this;
    }
/// <summary>
/// Configure await for the command execution 
/// </summary>
/// <param name="configureAwait"></param>
/// <returns></returns>
    public AsyncReturningCommand<T> ConfigureAwait(bool configureAwait=true)
    {
        _ConfigureAwait = configureAwait;

        return this;
    }
    
    
    ///<summary>
    ///Executes the command and invokes the <see cref="Action{T}"/> provided during construction.
    ///</summary>
    ///<param name="parameter">Data used by the command.</param>
    public async void Execute(T parameter)
    {
        try
        {
            _StartAction?.Invoke(parameter);
        }
        catch ( Exception )
        {
        }

        IsActive = true;
        RaiseCanExecuteChanged();
        var res = await ReturningCore.Returning.TryTask(async ()=>await _ExecuteMethod(parameter),_SaveLog,_ErrorName,_ErrorCode).ConfigureAwait(_ConfigureAwait);

        IsActive = false;
        RaiseCanExecuteChanged();

        try
        {
            _EndAction?.Invoke(parameter, res);
        }
        catch ( Exception )
        {
        }
    }

    ///<summary>
    ///Determines if the command can execute by invoked the <see cref="Func{T,Bool}"/> provided during construction.
    ///</summary>
    ///<param name="parameter">Data used by the command to determine if it can execute.</param>
    ///<returns>
    ///<see langword="true" /> if this command can be executed; otherwise, <see langword="false" />.
    ///</returns>
    public bool CanExecute(T parameter) { return !IsActive && _CanExecuteMethod(parameter); }

    /// <summary>
    /// Handle the internal invocation of <see cref="ICommand.Execute(object)"/>
    /// </summary>
    /// <param name="parameter">Command Parameter</param>
    protected override void Execute(object parameter) { Execute(( T ) parameter); }

    /// <summary>
    /// Handle the internal invocation of <see cref="ICommand.CanExecute(object)"/>
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns><see langword="true"/> if the Command Can Execute, otherwise <see langword="false" /></returns>
    protected override bool CanExecute(object parameter) { return CanExecute(( T ) parameter); }

    /// <summary>
    /// Observes a property that implements INotifyPropertyChanged, and automatically calls DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
    /// </summary>
    /// <typeparam name="TType">The type of the return value of the method that this delegate encapsulates</typeparam>
    /// <param name="propertyExpression">The property expression. Example: ObservesProperty(() => PropertyName).</param>
    /// <returns>The current instance of AsyncDelegateCommand</returns>
    public AsyncReturningCommand<T> ObservesProperty< TType >(Expression<Func<TType>> propertyExpression)
    {
        ObservesPropertyInternal(propertyExpression);

        return this;
    }

    /// <summary>
    /// Observes a property that is used to determine if this command can execute, and if it implements INotifyPropertyChanged it will automatically call DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
    /// </summary>
    /// <param name="canExecuteExpression">The property expression. Example: ObservesCanExecute(() => PropertyName).</param>
    /// <returns>The current instance of AsyncDelegateCommand</returns>
    public AsyncReturningCommand<T> ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        Expression<Func<T, bool>> expression = Expression.Lambda<Func<T, bool>>(canExecuteExpression.Body, Expression.Parameter(typeof( T ), "o"));
        _CanExecuteMethod = expression.Compile();
        ObservesPropertyInternal(canExecuteExpression);

        return this;
    }
}
