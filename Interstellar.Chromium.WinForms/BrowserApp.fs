namespace Interstellar.Chromium.WinForms
open System
open System.Windows.Forms
open Interstellar
open System.Threading
open System.Diagnostics

module BrowserApp =    
    let runAsync mainCtx (app: BrowserApp) = async {
        let windowCreator : BrowserWindowCreator = fun config ->
            upcast new BrowserWindow(?initialAddress = config.initialAddress)
        do! app.onStart mainCtx windowCreator
        Debug.WriteLine "after app function"
        do! Async.SwitchToContext mainCtx
        Debug.WriteLine (sprintf "Calling Application.Exit (). thread id: %A" Thread.CurrentThread.ManagedThreadId)
        Interstellar.Chromium.Platform.Shutdown ()
        Application.Exit ()
    }

    let run app =
        // call a Control ctor in order to initialize SynchronizationContext.Current. Constructing a Form, or calling Application.Run also does this.
        let dummyControl = new Control()
        Debug.WriteLine (sprintf "DummyControl thread: %A" Thread.CurrentThread.ManagedThreadId)
        Async.Start <| runAsync SynchronizationContext.Current app
        Application.Run ()