namespace Interstellar.Chromium.WinForms
open System
open System.Windows.Forms
open Interstellar
open System.Threading
open System.Diagnostics

module BrowserApp =
    /// <summary>Starts and runs a BrowserApp's lifecycle in a Windows Forms + Chromium host, asychronously</summary>
    /// <param name="mainCtx">Indicates the thread that is to be used as the UI thread</param>
    /// <param name="app">Describes the application lifecycle</param>
    let runAsync mainCtx (app: BrowserApp<Form>) = async {
        let windowCreator : BrowserWindowCreator<Form> = fun config ->
            let w = new BrowserWindow(config)
            BrowserWindowConfig.applyWindowTitle mainCtx w w.Disposed config.title
            upcast w
        do! Async.SwitchToContext mainCtx
        do! app.onStart mainCtx windowCreator
        do! Async.SwitchToContext mainCtx
        Interstellar.Chromium.Platform.Shutdown ()
        Application.Exit ()
    }

    /// <summary>Starts and runs a BrowserApp's lifecycle in a Windows Forms + Chromium host, using the current thread as the UI thread</summary>
    /// <param name="app">Describes the application lifecycle</param>
    let run app =
        // call a Control ctor in order to initialize SynchronizationContext.Current. Constructing a Form, or calling Application.Run also does this.
        use dummyControl = new Control() in ()
        Debug.WriteLine (sprintf "DummyControl thread: %A" Thread.CurrentThread.ManagedThreadId)
        Async.Start <| runAsync SynchronizationContext.Current app
        Application.Run ()
