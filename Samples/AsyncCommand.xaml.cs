using System;
using System.Threading.Tasks;
using System.Windows;
using MgSoftDev.FuncResult;
using MgSoftDev.PrismPlus.Commands;
using Prism.Mvvm;

namespace Samples;

public partial class AsyncCommand : Window
{
    public AsyncCommand()
    {
        InitializeComponent();
        
    }
}

public class AsyncCommandViewModel : BindableBase
{

    private string _Msg;

    public string Msg
    {
        get=>_Msg;
        set=>SetProperty(ref _Msg, value);
    }

    private bool _Wait;

    public bool Wait
    {
        get=>_Wait;
        set=>SetProperty(ref _Wait, value);
    }
    AsyncDelegateCommand _SimpleCommand;
    public AsyncDelegateCommand SimpleCommand => _SimpleCommand ??= new AsyncDelegateCommand( async ()=>
    {
        Wait = true;
       await  Returning.TryTask(async ()=>
        {
            Msg = "Start" + DateTime.Now.Ticks;
            await Task.Delay(5000);
            Msg = DateTime.Now.ToString();
        },true);

       Wait = false;
    });
    
    
    AsyncDelegateCommand<string> _SimpleParameterCommand;
    public AsyncDelegateCommand<string> SimpleParameterCommand=> _SimpleParameterCommand ??= new AsyncDelegateCommand<string>(async (p)=>
    {
        Msg =p+ "Start" + DateTime.Now.Ticks;
        await Task.Delay(5000);
        Msg = DateTime.Now.ToString();
    });
}

