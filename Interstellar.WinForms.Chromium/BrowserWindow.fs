namespace Interstellar.Chromium.WinForms
open System
open System.Drawing
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

    let titleChangedHandle = cefBrowser.TitleChanged.Subscribe (fun e -> lastKnownPageTitle <- e.Title)

    // (primary) constructor
    do
        this.Controls.Add cefBrowser

    member this.ChromiumBrowser = browser

    interface IBrowserWindow with
        member this.Browser = upcast browser
        member this.Close () = (this :> Form).Close ()
        [<CLIEvent>]
        member val Closed : IEvent<unit> = (this :> Form).FormClosed |> Event.map ignore
        member this.Platform = BrowserWindowPlatform.WinForms
        member this.IsShowing =
            seq { for frm in Application.OpenForms -> frm }
            |> Seq.contains (this :> Form)
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
        member val Shown = (this :> Form).Shown |> Event.map ignore
        member this.Size
            with get () =
                let size = (this :> Form).Size
                float size.Width, float size.Height
            and set (width, height) =
                (this :> Form).Size <- new Size(int width, int height)
        member this.Title
            with get () = this.Text
            and set title = this.Text <- title

    override this.Dispose disposing =
        if disposing then
            titleChangedHandle.Dispose ()
        base.Dispose disposing