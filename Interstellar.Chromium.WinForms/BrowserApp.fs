namespace Interstellar.Chromium.WinForms
open System
open System.Windows.Forms
open Interstellar

module BrowserApp =
    let runAsync (app: BrowserApp) = async {
        let windowCreator : BrowserWindowCreator = fun config ->
            let w = new BrowserWindow(?initialAddress = config.initialAddress)
            w.Show ()
            upcast w
        do! app.onStart windowCreator
        Application.Exit ()
    }

    let run app = Async.StartImmediate <| runAsync app