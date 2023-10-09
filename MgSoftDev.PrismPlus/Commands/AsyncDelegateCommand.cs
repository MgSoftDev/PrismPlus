using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;
using Prism.Commands;

namespace MgSoftDev.PrismPlus.Commands;

public class AsyncDelegateCommand : DelegateCommandBase
{
    Func<Task> _executeMethod;
    Func<bool> _canExecuteMethod;
    Action     _StartAction;
    Action<Exception>     _EndAction;
    bool       _ConfigureAwait = false;

    /// <summary>
    /// Creates a new instance of <see cref="AsyncDelegateCommand"/> with the <see cref="Action"/> to invoke on execution.
    /// </summary>
    /// <param name="executeMethod">The <see cref="Action"/> to invoke when <see cref="ICommand.Execute(object)"/> is called.</param>
    public AsyncDelegateCommand(Func<Task> executeMethod) : this(executeMethod, ()=>true) { }

    /// <summary>
    /// Creates a new instance of <see cref="AsyncDelegateCommand"/> with the <see cref="Action"/> to invoke on execution
    /// and a <see langword="Func" /> to query for determining if the command can execute.
    /// </summary>
    /// <param name="executeMethod">The <see cref="Action"/> to invoke when <see cref="ICommand.Execute"/> is called.</param>
    /// <param name="canExecuteMethod">The <see cref="Func{TResult}"/> to invoke when <see cref="ICommand.CanExecute"/> is called</param>
    public AsyncDelegateCommand(Func<Task> executeMethod, Func<bool> canExecuteMethod) : base()
    {
        if (executeMethod == null || canExecuteMethod == null) throw new ArgumentNullException(nameof(executeMethod), "AsyncDelegateCommand Delegates Cannot Be Null");

        _executeMethod    = executeMethod;
        _canExecuteMethod = canExecuteMethod;
    }


    /// <summary>
    /// Create action to be executed before the command is executed
    /// </summary>
    /// <param name="startAction"></param>
    /// <returns></returns>
    public AsyncDelegateCommand StartAction(Action startAction)
    {
        _StartAction = startAction;

        return this;
    }

    /// <summary>
    /// Create action to be executed after the command has completed
    /// </summary>
    /// <param name="endAction"></param>
    /// <returns></returns>
    public AsyncDelegateCommand EndAction(Action<Exception> endAction)
    {
        _EndAction = endAction;

        return this;
    }

    /// <summary>
    /// Create action to be executed before the command is executed
    /// </summary>
    /// <param name="startActionAsync"></param>
    /// <returns></returns>
    public AsyncDelegateCommand StartAction(Func<bool> canExecuteAction)
    {
        _canExecuteMethod = canExecuteAction ?? throw new ArgumentNullException(nameof(canExecuteAction), "AsyncDelegateCommand Delegates Cannot Be Null");

        return this;
    }

    /// <summary>
    /// Configure await for the command execution 
    /// </summary>
    /// <param name="configureAwait"></param>
    /// <returns></returns>
    public AsyncDelegateCommand ConfigureAwait(bool configureAwait = true)
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
        Exception exception = null;

        try
        {
            await _executeMethod().ConfigureAwait(_ConfigureAwait);
        }
        catch ( Exception ex )
        {
            exception = ex;
        }

        IsActive = false;
        RaiseCanExecuteChanged();

        try
        {
            _EndAction?.Invoke(exception);
        }
        catch ( Exception )
        {
        }
    }

    /// <summary>
    /// Determines if the command can be executed.
    /// </summary>
    /// <returns>Returns <see langword="true"/> if the command can execute,otherwise returns <see langword="false"/>.</returns>
    public bool CanExecute() { return !IsActive && _canExecuteMethod(); }

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
    public AsyncDelegateCommand ObservesProperty< T >(Expression<Func<T>> propertyExpression)
    {
        ObservesPropertyInternal(propertyExpression);

        return this;
    }

    /// <summary>
    /// Observes a property that is used to determine if this command can execute, and if it implements INotifyPropertyChanged it will automatically call AsyncDelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
    /// </summary>
    /// <param name="canExecuteExpression">The property expression. Example: ObservesCanExecute(() => PropertyName).</param>
    /// <returns>The current instance of AsyncDelegateCommand</returns>
    public AsyncDelegateCommand ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        _canExecuteMethod = canExecuteExpression.Compile();
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
public class AsyncDelegateCommand< T > : DelegateCommandBase
{
    readonly Func<T, Task> _executeMethod;
    Func<T, bool>          _canExecuteMethod;
    Action<T>              _StartAction;
    Action<T,Exception>              _EndAction;

    bool _ConfigureAwait = false;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncDelegateCommand{T}"/>.
    /// </summary>
    /// <param name="executeMethod">Delegate to execute when Execute is called on the command. This can be null to just hook up a CanExecute delegate.</param>
    /// <remarks><see cref="CanExecute(T)"/> will always return true.</remarks>
    public AsyncDelegateCommand(Func<T, Task> executeMethod) : this(executeMethod, (o)=>true) { }


    /// <summary>
    /// Initializes a new instance of <see cref="AsyncDelegateCommand{T}"/>.
    /// </summary>
    /// <param name="executeMethod">Delegate to execute when Execute is called on the command. This can be null to just hook up a CanExecute delegate.</param>
    /// <param name="canExecuteMethod">Delegate to execute when CanExecute is called on the command. This can be null.</param>
    /// <exception cref="ArgumentNullException">When both <paramref name="executeMethod"/> and <paramref name="canExecuteMethod"/> are <see langword="null" />.</exception>
    public AsyncDelegateCommand(Func<T, Task> executeMethod, Func<T, bool> canExecuteMethod) : base()
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

        _executeMethod    = executeMethod;
        _canExecuteMethod = canExecuteMethod;
    }


    /// <summary>
    /// Create action to be executed before the command is executed
    /// </summary>
    /// <param name="startAction"></param>
    /// <returns></returns>
    public AsyncDelegateCommand<T> StartAction(Action<T> startAction)
    {
        _StartAction = startAction;

        return this;
    }

    /// <summary>
    /// Create action to be executed after the command has completed
    /// </summary>
    /// <param name="endAction"></param>
    /// <returns></returns>
    public AsyncDelegateCommand<T> EndAction(Action<T,Exception> endAction)
    {
        _EndAction = endAction;

        return this;
    }


    /// <summary>
    /// Create action to be executed before the command is executed
    /// </summary>
    /// <param name="startActionAsync"></param>
    /// <returns></returns>
    public AsyncDelegateCommand<T> StartAction(Func<T, bool> canExecuteAction)
    {
        _canExecuteMethod = canExecuteAction ?? throw new ArgumentNullException(nameof(canExecuteAction), "AsyncDelegateCommand Delegates Cannot Be Null");

        return this;
    }

    /// <summary>
    /// Configure await for the command execution 
    /// </summary>
    /// <param name="configureAwait"></param>
    /// <returns></returns>
    public AsyncDelegateCommand<T> ConfigureAwait(bool configureAwait = true)
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
        Exception exception = null;

        try
        {
            await _executeMethod(parameter).ConfigureAwait(_ConfigureAwait);
        }
        catch ( Exception ex )
        {
            exception = ex;
        }

        IsActive = false;
        RaiseCanExecuteChanged();

        try
        {
            _EndAction?.Invoke(parameter, exception);
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
    public bool CanExecute(T parameter) { return !IsActive && _canExecuteMethod(parameter); }

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
    public AsyncDelegateCommand<T> ObservesProperty< TType >(Expression<Func<TType>> propertyExpression)
    {
        ObservesPropertyInternal(propertyExpression);

        return this;
    }

    /// <summary>
    /// Observes a property that is used to determine if this command can execute, and if it implements INotifyPropertyChanged it will automatically call DelegateCommandBase.RaiseCanExecuteChanged on property changed notifications.
    /// </summary>
    /// <param name="canExecuteExpression">The property expression. Example: ObservesCanExecute(() => PropertyName).</param>
    /// <returns>The current instance of AsyncDelegateCommand</returns>
    public AsyncDelegateCommand<T> ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        Expression<Func<T, bool>> expression = Expression.Lambda<Func<T, bool>>(canExecuteExpression.Body, Expression.Parameter(typeof( T ), "o"));
        _canExecuteMethod = expression.Compile();
        ObservesPropertyInternal(canExecuteExpression);

        return this;
    }
}
