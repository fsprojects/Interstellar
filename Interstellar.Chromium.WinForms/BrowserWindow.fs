namespace Interstellar.Chromium.WinForms
open System
open System.Windows
open System.Windows.Forms
open CefSharp
open CefSharp.WinForms
open Interstellar.Core

type BrowserWindow() as this =
    inherit Form()

    let browser = new ChromiumWebBrowser(null: string)
    let mutable title = ""

    // (primary) constructor
    do
        this.Controls.Add browser
        // TODO: dispose the event handler
        browser.TitleChanged.Add (fun e ->
            title <- e.Title
        )

    interface IBrowserWindow with
        member this.Engine = BrowserEngineType.Chromium
        member this.Platform = BrowserPlatformType.WindowsWpf
        member this.Address
            with get () = browser.Address
            and set address = browser.Load address
        member this.Title = title
        [<CLIEvent>]
        member this.TitleChanged : IEvent<string> =
            browser.TitleChanged |> Event.map (fun (e: TitleChangedEventArgs) -> e.Title)

    member private this.I = this :> IBrowserWindow

    member this.Engine = this.I.Engine
    member this.Platform = this.I.Platform
    member this.Address with get () = this.I.Address and set x = (this.I.Address <- x)
    member this.Title = this.I
    [<CLIEvent>] member this.TitleChanged = this.I.TitleChanged