namespace InterstellarApp
open System
open Interstellar
open Interstellar.Core

module BrowserApp =
    let app onMainWindowCreated : BrowserApp<'TWindow> = BrowserApp.create (fun mainCtx createWindow -> async {
        let mainWindow = createWindow { defaultBrowserWindowConfig with address = Some (Uri "https://jwosty.github.io/Interstellar/") }
        do! mainWindow.Show ()
        do! Async.AwaitEvent mainWindow.Closed
    })