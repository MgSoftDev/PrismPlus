using MgSoftDev.PrismPlus.Commands;
using Prism.Commands;

namespace MgSoftDev.PrismPlus.Helper.Extension;

public static class DelegateCommandExtension
{
    public static bool CanAndExecute(this DelegateCommand command)
    {
        var res = command.CanExecute();
        if (res)command.Execute();
        return res;
    }
    public static bool CanAndExecute<T>(this DelegateCommand<T> command, T parameter)
    {
        var res = command.CanExecute(parameter);
        if (res)command.Execute(parameter);
        return res;
    }
    public static bool CanAndExecute(this AsyncDelegateCommand command)
    {
        var res = command.CanExecute();
        if (res)command.Execute();
        return res;
    }
    public static bool CanAndExecute<T>(this AsyncDelegateCommand<T> command, T parameter)
    {
        var res = command.CanExecute(parameter);
        if (res)command.Execute(parameter);
        return res;
    }
}
