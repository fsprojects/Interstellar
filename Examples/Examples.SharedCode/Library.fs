namespace Examples.SharedCode
open System
open System.Diagnostics
open Interstellar
open Interstellar.Core
open System.Threading

module SimpleBrowserApp =
    let updateTitle mapPageTitle (window: IBrowserWindow) =
        let work = async {
            use! holder = Async.OnCancel (fun () ->
                Trace.WriteLine "Window title updater cancelled")
            while true do
                let! pageTitle = Async.AwaitEvent window.PageTitleChanged
                Trace.WriteLine (sprintf "Browser page title is: %s" pageTitle)
                window.Title <- mapPageTitle pageTitle
        }
        let cancellation = new CancellationTokenSource()
        Async.StartImmediate (work, cancellation.Token)
        Async.StartImmediate <| async {
            do! Async.AwaitEvent window.Closed
            Trace.WriteLine "Window closed"
            cancellation.Cancel ()
        }

    let app host runtimeFramework = BrowserApp.create (fun createWindow -> async {
        Trace.WriteLine "Opening first window"
        do! async {
            use window1 = createWindow { defaultBrowserWindowConfig with initialAddress = Some "file:///C:/WINDOWS/" }
            updateTitle (fun pageTitle -> sprintf "%s - %s - %s" host runtimeFramework pageTitle) window1
            do! Async.AwaitEvent window1.Closed
        }
        Trace.WriteLine "First window closed. Opening next window in 3 seconds..."
        do! Async.Sleep 3_000
        Trace.WriteLine "Opening second window"
        do! async {
            use window2 = createWindow { defaultBrowserWindowConfig with initialAddress = Some "https://google.com/" }
            updateTitle (fun pageTitle -> sprintf "%s - %s - %s" host runtimeFramework pageTitle) window2
            do! Async.AwaitEvent window2.Closed
        }
        Trace.WriteLine "Second window closed -- exiting app"
    })