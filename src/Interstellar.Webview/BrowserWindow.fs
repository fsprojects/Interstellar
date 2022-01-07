namespace Interstellar.Chromium.Wpf
open System
open System.Threading
open System.Diagnostics
open System.Windows
open System.Windows.Controls
open Webview
open Interstellar
open Interstellar.Chromium

type BrowserWindow(config: BrowserWindowConfig<Window>) as this =
    inherit Window()
    
    let mainCtx = SynchronizationContext.Current
    let cefBrowser = new CefSharp.Wpf.ChromiumWebBrowser()
    let browser =
        new Interstellar.Chromium.Browser<_>(
            cefBrowser,
            { getPageTitle = fun () -> cefBrowser.Title
              titleChanged = cefBrowser.TitleChanged |> Event.map (fun x -> x.NewValue :?> string)
              isBrowserInitializedChanged = cefBrowser.IsBrowserInitializedChanged |> Event.map ignore},
            config)
    let owningThreadId = Thread.CurrentThread.ManagedThreadId

    let mutable alreadyShown = false
    let shown = new Event<unit>()

    // (primary) constructor
    do
        this.Content <- cefBrowser

    interface IDisposable with
        member this.Dispose () =
            Async.StartImmediate <| async {
                do! Async.SwitchToContext mainCtx
                this.Close ()
            }

    interface IBrowserWindow<Window> with
        member this.Browser = upcast browser
        member this.Close () = (this :> Window).Close ()
        [<CLIEvent>] member val Closed = (this :> Window).Closed |> Event.map ignore
        member this.IsShowing =
            seq { for w in Application.Current.Windows -> w }
            |> Seq.contains (this :> Window)
        member this.NativeWindow = this :> Window
        member this.Platform = BrowserWindowPlatform.Wpf
        member this.Show () =
            if owningThreadId <> Thread.CurrentThread.ManagedThreadId then
                raise (new InvalidOperationException("Show() called from a thread other than the thread on which the BrowserWindow was constructed."))
            (this :> Window).Show ()
            async {
                if not cefBrowser.IsBrowserInitialized then
                    let! _ = Async.AwaitEvent cefBrowser.IsBrowserInitializedChanged
                    ()
            }
        member this.Size
            with get () = base.Width, base.Height
            and set (width, height) =
                base.Width <- width
                base.Height <- height
        [<CLIEvent>] member val Shown = shown.Publish
        member this.Title
            with get () = (this :> Window).Title
            and set title = (this :> Window).Title <- title

    override this.OnContentRendered e =
        base.OnContentRendered e
        if not alreadyShown then
            alreadyShown <- true
            shown.Trigger ()