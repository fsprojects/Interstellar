namespace Interstellar.Chromium.WinForms
open Interstellar

module BrowserApp =
    let runAsync (app: BrowserApp<BrowserWindow>) = async {
        let windowCreator : BrowserWindowCreator<BrowserWindow> = fun () -> new BrowserWindow()
        do! app.onStart windowCreator
    }
    let run app = Async.StartImmediate <| runAsync app