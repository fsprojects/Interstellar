namespace Examples.SharedCode
open System
open System.Diagnostics
open Interstellar
open Interstellar.Core

module SimpleBrowserApp =
    let app host runtimeFramework = BrowserApp.create (fun createWindow -> async {
        let mainWindow = createWindow { defaultBrowserWindowConfig with initialAddress = Some "https://google.com/" }
        while true do
            let! pageTitle = Async.AwaitEvent mainWindow.PageTitleChanged
            Trace.WriteLine (sprintf "Browser page title is: %s" pageTitle)
            mainWindow.Title <- sprintf "%s - %s - %s" host runtimeFramework pageTitle
        ()
    })