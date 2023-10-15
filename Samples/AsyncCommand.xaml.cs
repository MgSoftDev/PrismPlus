using System;
using System.Threading.Tasks;
using System.Windows;
using MgSoftDev.PrismPlus.Commands;
using MgSoftDev.PrismPlus.Returning.Commands;
using MgSoftDev.ReturningCore;
using Prism.Mvvm;

namespace Samples;

public partial class AsyncCommand : Window
{
    public AsyncCommand() { InitializeComponent(); }
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

    ReturningCommand _SimpleCommand;

    public ReturningCommand SimpleCommand=>
        _SimpleCommand ??= new ReturningCommand(()=>
            {
                Wait = true;
                Msg  = "Start" + DateTime.Now.Ticks;
                Task.Delay(5000).Wait();
                Msg = DateTime.Now.ToString();

                throw new Exception("MY CUSTOM ERROR");


                Wait = false;

                return Returning.Success();
            }).SaveLog("Error en la funcion XXXXX")
              .StartAction(()=>Msg+="Start Execute")
              .EndAction(r=>Msg += r.ErrorInfo?.ErrorMessage);


    AsyncDelegateCommand<string> _SimpleParameterCommand;

    public AsyncDelegateCommand<string> SimpleParameterCommand=>
        _SimpleParameterCommand ??= new AsyncDelegateCommand<string>(async (p)=>
        {
            Msg = p + "Start" + DateTime.Now.Ticks;
            await Task.Delay(5000);
            Msg = DateTime.Now.ToString();
        });

    private AsyncDelegateCommand _commandPropertyCommand;

    public AsyncDelegateCommand commandPropertyCommand=>
        _commandPropertyCommand ??= new AsyncDelegateCommand(async ()=>
            {
            }, ()=>!Wait).ObservesProperty(()=>Wait)
                         .StartAction(()=>Wait = true)
                         .EndAction((ex)=>Wait = false);
}
