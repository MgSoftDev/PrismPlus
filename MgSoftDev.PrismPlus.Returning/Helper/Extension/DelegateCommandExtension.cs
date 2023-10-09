using MgSoftDev.PrismPlus.Returning.Commands;

namespace MgSoftDev.PrismPlus.Returning.Helper.Extension;

public static class DelegateCommandExtension
{
    
    public static bool CanAndExecute(this AsyncReturningCommand command)
    {
        var res = command.CanExecute();
        if (res)command.Execute();
        return res;
    }
    public static bool CanAndExecute<T>(this AsyncReturningCommand<T> command, T parameter)
    {
        var res = command.CanExecute(parameter);
        if (res)command.Execute(parameter);
        return res;
    }
}
