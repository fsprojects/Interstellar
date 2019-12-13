namespace Interstellar.Chromium.WinForms
open System
open System.Windows
open System.Windows.Forms
open CefSharp
open CefSharp.WinForms
open Interstellar
open System.Threading
open System.Diagnostics

type BrowserWindow(config: BrowserWindowConfig) as this =
    inherit Form(Visible = false)

    let mutable disposed = false
    let mutable lastKnownPageTitle = ""
    let owningThreadId = Thread.CurrentThread.ManagedThreadId
    let cefBrowser = new ChromiumWebBrowser(null: string)
    let browser =
        new Interstellar.Chromium.Browser(
            cefBrowser,
            { getPageTitle = fun () -> lastKnownPageTitle
              titleChanged = cefBrowser.TitleChanged |> Event.map (fun x -> x.Title)
              isBrowserInitializedChanged = cefBrowser.IsBrowserInitializedChanged |> Event.map ignore},
            config)

    // (primary) constructor
    do
        this.Controls.Add cefBrowser
        // TODO: dispose event handlers if necessary
        cefBrowser.TitleChanged.Add (fun e ->
            lastKnownPageTitle <- e.Title
        )

    member this.ChromiumBrowser = browser

    interface IBrowserWindow with
        member this.Browser = upcast browser
        member this.Close () = (this :> Form).Close ()
        [<CLIEvent>]
        member this.Closed : IEvent<unit> = (this :> Form).FormClosed |> Event.map ignore
        member this.Platform = BrowserWindowPlatform.WinForms
        member this.Show () =
            if (Thread.CurrentThread.ManagedThreadId <> owningThreadId) then
                raise (new InvalidOperationException("Show() called from a thread other than the thread on which the BrowserWindow was constructed."))
            (this :> Form).Show ()
            async {
                if cefBrowser.IsBrowserInitialized then ()
                else
                    let! _ = Async.AwaitEvent cefBrowser.IsBrowserInitializedChanged
                    ()
            }
        [<CLIEvent>]
        member this.Shown = (this :> Form).Shown |> Event.map ignore
        member this.Title
            with get () = this.Text
            and set title = this.Text <- title

    member private this._Dispose disposing =
        if not disposed then
            if disposing then
                cefBrowser.Dispose ()
                base.Dispose ()
            disposed <- true

    interface IDisposable with
        override this.Dispose () =
            let token = this.BeginInvoke (Action(fun () ->
                this._Dispose true
            ))
            ()