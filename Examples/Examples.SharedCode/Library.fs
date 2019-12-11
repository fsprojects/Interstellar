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
                let! pageTitle = Async.AwaitEvent window.PageTitleChanged
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

    let app host runtimeFramework = BrowserApp.create (fun mainCtx createWindow -> async {
        Trace.WriteLine "Opening first window"
        do! async {
            do! Async.SwitchToContext mainCtx
            use window1 = createWindow { defaultBrowserWindowConfig with initialAddress = Some "file:///C:/WINDOWS/" }
            window1.Show ()
            do! Async.SwitchToThreadPool ()
            startTitleUpdater mainCtx (fun pageTitle -> sprintf "%s - %s - %s" host runtimeFramework pageTitle) window1
            do! Async.AwaitEvent window1.Closed
        }
        Trace.WriteLine "First window closed. Opening next window"
        do! async {
            do! Async.SwitchToContext mainCtx
            use window2 = createWindow { defaultBrowserWindowConfig with initialAddress = Some "https://google.com/" }
            window2.Show ()
            do! Async.SwitchToThreadPool ()
            startTitleUpdater mainCtx (fun pageTitle -> sprintf "%s - %s - %s" host runtimeFramework pageTitle) window2
            do! Async.AwaitEvent window2.Closed
        }
        Trace.WriteLine "Second window closed -- exiting app"
    })

    let simpleApp host runtimeFramework = BrowserApp.create (fun mainCtx createWindow -> async {
        Trace.WriteLine (sprintf "Opening window. mainCtx: %A, thread id: %A" mainCtx Thread.CurrentThread.ManagedThreadId)
        do! Async.SwitchToContext mainCtx
        Trace.WriteLine (sprintf "ctx switch successful. mainCtx: %A, thread id: %A" mainCtx Thread.CurrentThread.ManagedThreadId)
        let window = createWindow { defaultBrowserWindowConfig with initialAddress = Some "file:///C:/WINDOWS/" }
        window.Show ()
        do! Async.SwitchToNewThread ()
        Trace.WriteLine "Window shown"
        window.Closed.Add (fun e -> Trace.WriteLine "Closed event fired")
        do! Async.AwaitEvent window.Closed
        //do! Async.Sleep 10_000
        Trace.WriteLine "Window closed. Exiting app function"
    })