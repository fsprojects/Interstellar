namespace Interstellar.Chromium.WinForms
open System
open System.Windows
open System.Windows.Forms
open CefSharp
open CefSharp.WinForms
open Interstellar

type BrowserWindow(?initialAddress: string) as this =
    inherit Form()

    let browser = new ChromiumWebBrowser(null: string)
    let mutable lastKnownPageTitle = ""

    // (primary) constructor
    do
        this.Controls.Add browser
        initialAddress |> Option.iter ((this :> IBrowserWindow).Load)
        // TODO: dispose the event handler
        browser.TitleChanged.Add (fun e ->
            lastKnownPageTitle <- e.Title
        )

    interface IBrowserWindow with
        member this.Engine = BrowserEngineType.Chromium
        member this.Platform = BrowserPlatformType.WindowsWpf
        member this.Address = browser.Address
        member this.Show () = (this :> Form).Show ()
        [<CLIEvent>]
        member this.Shown = (this :> Form).Shown |> Event.map ignore
        member this.Load address = browser.Load address
        member this.Reload () = browser.Reload ()
        member this.PageTitle = lastKnownPageTitle
        [<CLIEvent>]
        member this.PageTitleChanged : IEvent<string> =
            browser.TitleChanged |> Event.map (fun (e: TitleChangedEventArgs) -> e.Title)
        member this.Title
            with get () = this.Text
            and set title = this.Text <- title
        member this.Close () = (this :> Form).Close ()
        [<CLIEvent>]
        member this.Closed : IEvent<unit> = (this :> Form).FormClosed |> Event.map ignore