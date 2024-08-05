namespace Interstellar.Chromium.Wpf
open System
open System.Windows
open Interstellar
open System.Threading

module BrowserApp =
    /// <summary>Starts and runs a BrowserApp's lifecycle in a WPF + Chromium host, asychronously</summary>
    /// <param name="mainCtx">Indicates the thread that is to be used as the UI thread</param>
    /// <param name="app">Describes the application lifecycle</param>
    let runAsync mainCtx (app: BrowserApp<Window>) = async {
        let windowCreator : BrowserWindowCreator<Window> = fun config ->
            let w = new BrowserWindow(config)
            BrowserWindowConfig.applyWindowTitle mainCtx w w.Unloaded config.title
            upcast w
        do! Async.SwitchToContext mainCtx
        do! app.onStart mainCtx windowCreator
        do! Async.SwitchToContext mainCtx
        Application.Current.Shutdown ()
    }
    
    /// <summary>Starts and runs a BrowserApp's lifecycle in a WPF + Chromium host, using the current thread as the UI thread</summary>
    /// <param name="app">Describes the application lifecycle</param>
    let run app = Async.Start <| runAsync SynchronizationContext.Current app
