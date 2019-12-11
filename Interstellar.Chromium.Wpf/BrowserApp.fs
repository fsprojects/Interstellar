namespace Interstellar.Chromium.Wpf
open System
open System.Windows
open Interstellar
open System.Threading

module BrowserApp =
    let runAsync mainCtx (app: BrowserApp) = async {
        let windowCreator : BrowserWindowCreator = fun config ->
            let w = new BrowserWindow(?initialAddress = config.initialAddress)
            //w.Show ()
            upcast w
        do! app.onStart mainCtx windowCreator
    }

    let run app = Async.Start <| runAsync SynchronizationContext.Current app