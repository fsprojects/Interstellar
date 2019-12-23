namespace Interstellar.Chromium.Wpf
open System
open System.Windows
open Interstellar
open System.Threading

module BrowserApp =
    let runAsync mainCtx (app: BrowserApp) = async {
        let windowCreator : BrowserWindowCreator = fun config ->
            let w = new BrowserWindow(config)
            BrowserWindowConfig.applyWindowTitle mainCtx w w.Unloaded config.title
            upcast w
        do! Async.SwitchToContext mainCtx
        do! app.onStart mainCtx windowCreator
        do! Async.SwitchToContext mainCtx
        Application.Current.Shutdown ()
    }

    let run app = Async.Start <| runAsync SynchronizationContext.Current app