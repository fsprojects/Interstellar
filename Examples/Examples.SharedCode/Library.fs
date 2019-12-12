namespace Examples.SharedCode
open System
open System.Diagnostics
open Interstellar
open Interstellar.Core
open System.Threading

module SimpleBrowserApp =
    let startTitleUpdater mainCtx mapPageTitle (window: IBrowserWindow) =
        let work = async {
            use! holder = Async.OnCancel (fun () ->
                Trace.WriteLine "Window title updater cancelled")
            while true do
                let! pageTitle = Async.AwaitEvent window.Browser.PageTitleChanged
                Trace.WriteLine (sprintf "Browser page title is: %s" pageTitle)
                do! Async.SwitchToContext mainCtx
                window.Title <- mapPageTitle pageTitle
                do! Async.SwitchToThreadPool ()
        }
        let cancellation = new CancellationTokenSource()
        Async.Start (work, cancellation.Token)
        Async.Start <| async {
            do! Async.AwaitEvent window.Closed
            Trace.WriteLine "Window closed"
            cancellation.Cancel ()
        }

    let complexApp host runtimeFramework = BrowserApp.create (fun mainCtx createWindow -> async {
        Trace.WriteLine "Opening first window"
        do! async {
            do! Async.SwitchToContext mainCtx
            use window1 = createWindow { defaultBrowserWindowConfig with address = Some "https://rendering/"; html = Some "<html><body><p1>Hello world</p1></body></html>" }
            do! window1.Show ()
            do! Async.SwitchToThreadPool ()
            startTitleUpdater mainCtx (fun pageTitle -> sprintf "%s - %s - %s" host runtimeFramework pageTitle) window1
            do! Async.AwaitEvent window1.Closed
        }
        Trace.WriteLine "First window closed. Opening next window"
        do! async {
            do! Async.SwitchToContext mainCtx
            use window2 = createWindow { defaultBrowserWindowConfig with address = Some "https://google.com/" }
            do! window2.Show ()
            do! Async.SwitchToThreadPool ()
            startTitleUpdater mainCtx (fun pageTitle -> sprintf "%s - %s - %s" host runtimeFramework pageTitle) window2
            do! Async.AwaitEvent window2.Closed
        }
        Trace.WriteLine "Second window closed -- exiting app"
    })

    let app host runtimeFramework = BrowserApp.create (fun mainCtx createWindow -> async {
        do! Async.SwitchToContext mainCtx
        Trace.WriteLine "Opening window"
        //let window = createWindow { defaultBrowserWindowConfig with address = Some "data:text/html;charset=utf-8;base64,PGh0bWw+PGJvZHk+PHA+SGVsbG8gd29ybGQ8L3A+PC9ib2R5PjwvaHRtbD4=" }
        let window = createWindow { defaultBrowserWindowConfig with showDevTools = true; address = Some "https://rendering/"; html = Some "<html><body><p>Hello world</p><div id=\"myDiv\"></div></body></html>" }
        do! window.Show ()
        do! Async.SwitchToThreadPool ()
        do! Async.Sleep 1_000
        do! Async.SwitchToContext mainCtx
        window.Browser.ExecuteJavascript "alert('hello')"
        do! Async.SwitchToThreadPool ()
        Trace.WriteLine "Window shown"
        do! Async.AwaitEvent window.Closed
        Trace.WriteLine "Window closed. Exiting app function"
    })