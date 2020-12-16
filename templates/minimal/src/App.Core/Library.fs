namespace App
open System
open Interstellar
open Interstellar.Core

module MyBrowserApp =
    let app onMainWindowCreated : BrowserApp<'TWindow> = BrowserApp.create (fun mainCtx createWindow -> async {
        let mainWindow = createWindow { defaultBrowserWindowConfig with address = Some (Uri "https://jwosty.github.io/Interstellar/") }
        do! mainWindow.Show ()
        do! Async.AwaitEvent mainWindow.Closed
    })