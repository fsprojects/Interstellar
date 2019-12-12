namespace Interstellar.Chromium.Wpf
open System
open System.Windows
open System.Windows.Controls
open CefSharp
open CefSharp.Wpf
open Interstellar
open System.Threading
open System.Diagnostics

type BrowserWindow(config: BrowserWindowConfig) as this =
    inherit Window()

    let mainCtx = SynchronizationContext.Current
    let browser = new CefSharp.Wpf.ChromiumWebBrowser()
    let owningThreadId = Thread.CurrentThread.ManagedThreadId

    let mutable alreadyShown = false
    let shown = new Event<unit>()

    // (primary) constructor
    do
        this.Content <- browser
        browser.IsBrowserInitializedChanged.Add (fun x ->
            if browser.IsBrowserInitialized then
                match config.address, config.html with
                | address, Some html -> browser.LoadHtml (html, Option.toObj address) |> ignore
                | Some address, None -> browser.Load address
                | None, None -> ()
        )

    member this.ChromiumBrowser = browser

    interface IDisposable with
        member this.Dispose () =
            Async.Start <| async {
                do! Async.SwitchToContext mainCtx
                this.Close ()
            }

    interface IBrowserWindow with
        member this.Engine = BrowserEngineType.Chromium
        member this.Platform = BrowserPlatformType.WindowsWpf
        member this.Address = browser.Address
        member this.Show () =
            if owningThreadId <> Thread.CurrentThread.ManagedThreadId then
                raise (new InvalidOperationException("Show() called from a thread other than the thread on which the BrowserWindow was constructed."))
            (this :> Window).Show ()
            async {
                if browser.IsBrowserInitialized then ()
                else
                    let! _ = Async.AwaitEvent browser.IsBrowserInitializedChanged
                    ()
            }
        [<CLIEvent>] member this.Shown = shown.Publish
        member this.Close () = (this :> Window).Close ()
        [<CLIEvent>] member this.Closed = (this :> Window).Closed |> Event.map ignore
        member this.Load address = browser.Load address
        member this.LoadString (html, ?uri) =
            match uri with
            | Some uri -> browser.LoadHtml (html, uri) |> ignore
            | None -> browser.LoadHtml html
        member this.Reload () = browser.Reload ()
        member this.PageTitle = browser.Title
        [<CLIEvent>]
        member this.PageTitleChanged : IEvent<string> =
            browser.TitleChanged |> Event.map (fun (e: DependencyPropertyChangedEventArgs) -> e.NewValue :?> string)
        member this.Title
            with get () = (this :> Window).Title
            and set title = (this :> Window).Title <- title
        member this.ShowDevTools () = browser.ShowDevTools ()
        member this.CloseDevTools () = browser.CloseDevTools ()
        member this.AreDevToolsShowing = browser.GetBrowserHost().HasDevTools

    override this.OnContentRendered e =
        base.OnContentRendered e
        if not alreadyShown then
            alreadyShown <- true
            shown.Trigger ()