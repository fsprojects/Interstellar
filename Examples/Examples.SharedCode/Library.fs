namespace Examples.SharedCode
open System
open System.Diagnostics
open Interstellar
open Interstellar.Core
open System.Threading
open System.Reflection
open System.Runtime.Versioning

module SimpleBrowserApp =
    let runtimeFramework = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName
    
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

    let complexApp = BrowserApp.create (fun mainCtx createWindow -> async {
        Trace.WriteLine (sprintf "Runtime framework: %s" runtimeFramework)
        Trace.WriteLine "Opening first window"
        do! async {
            do! Async.SwitchToContext mainCtx
            use window1 = createWindow { defaultBrowserWindowConfig with address = Some "https://rendering/"; html = Some "<html><body><p1>Hello world</p1></body></html>" }
            do! window1.Show ()
            do! Async.SwitchToThreadPool ()
            startTitleUpdater mainCtx (fun pageTitle -> sprintf "%A - %s - %s" window1.Platform runtimeFramework pageTitle) window1
            do! Async.AwaitEvent window1.Closed
        }
        Trace.WriteLine "First window closed. Opening next window"
        do! async {
            do! Async.SwitchToContext mainCtx
            use window2 = createWindow { defaultBrowserWindowConfig with address = Some "https://google.com/" }
            do! window2.Show ()
            do! Async.SwitchToThreadPool ()
            startTitleUpdater mainCtx (fun pageTitle -> sprintf "%A - %s - %s" window2.Platform runtimeFramework pageTitle) window2
            do! Async.AwaitEvent window2.Closed
        }
        Trace.WriteLine "Second window closed -- exiting app"
    })

    let app = BrowserApp.create (fun mainCtx createWindow -> async {
        do! Async.SwitchToContext mainCtx
        Trace.WriteLine "Opening window"
        //let window = createWindow { defaultBrowserWindowConfig with address = Some "data:text/html;charset=utf-8;base64,PGh0bWw+PGJvZHk+PHA+SGVsbG8gd29ybGQ8L3A+PC9ib2R5PjwvaHRtbD4=" }
        //let window = createWindow { defaultBrowserWindowConfig with address = Some "https://google.com/" }
        let page = sprintf "
            <html>
                <head>
                    <title>Static HTML Example</title>
                </head>
                <body>
                    <p>Here is some static HTML.</p>
                    <p id=\"dynamicContent\" />
                    <p id=\"host\" />
                    <p id=\"runtimeFramework\" />
                    <p id=\"browserWindowPlatform\"/>
                    <p id=\"browserEngine\" />
                </body>
            </html>"
        let window = createWindow { defaultBrowserWindowConfig with showDevTools = true; address = Some "https://rendering/"; html = Some page }
        startTitleUpdater mainCtx (sprintf "BrowserApp - %s") window
        do! window.Show ()
        do! Async.SwitchToThreadPool ()
        do! Async.Sleep 1_000 // FIXME: introduce some mechanism to let us wait until it is valid to start executing Javascript
        do! Async.SwitchToContext mainCtx
        let lines = [
            "document.getElementById(\"dynamicContent\")               .innerHTML = \"Hello from browser-injected Javascript!\""
            sprintf "document.getElementById(\"runtimeFramework\")     .innerHTML = \"Runtime framework: %s\"" runtimeFramework
            sprintf "document.getElementById(\"browserEngine\")        .innerHTML = \"Browser engine: %A\"" window.Browser.Engine
            sprintf "document.getElementById(\"browserWindowPlatform\").innerHTML = \"BrowserWindow platform: %A\"" window.Platform
            sprintf "setTimeout(function () { document.title = \"PSYCH, It's actually a Dynamic Javascript Example!\" }, 5000)"
        ]
        let script = String.Join (";", lines)
        Debug.WriteLine (sprintf "Executing script:\n%s" script)
        window.Browser.ExecuteJavascript script
        do! Async.SwitchToThreadPool ()
        Trace.WriteLine "Window shown"
        do! Async.AwaitEvent window.Closed
        Trace.WriteLine "Window closed. Exiting app function"
    })