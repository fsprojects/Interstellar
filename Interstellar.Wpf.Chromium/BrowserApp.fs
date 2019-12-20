namespace Interstellar.Chromium.Wpf
open System
open System.Windows
open Interstellar
open System.Threading

module BrowserApp =
    let runAsync mainCtx (app: BrowserApp) = async {
        let windowCreator : BrowserWindowCreator = fun config -> upcast new BrowserWindow(config)
        do! app.onStart mainCtx windowCreator
        do! Async.SwitchToContext mainCtx
        Application.Current.Shutdown ()
    }

    let run app = Async.Start <| runAsync SynchronizationContext.Current app