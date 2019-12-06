namespace Examples.SharedCode
open System
open System.Diagnostics
open Interstellar
open Interstellar.Core

module SimpleBrowserApp =
    let app = BrowserApp.create (fun createWindow -> async {
        let mainWindow = createWindow { defaultBrowserWindowConfig with initialAddress = Some "https://google.com/" }
        while true do
            let! pageTitle = Async.AwaitEvent mainWindow.PageTitleChanged
            Trace.WriteLine (sprintf "Page title changed to %s" pageTitle)
            mainWindow.Title <- pageTitle
        ()
    })