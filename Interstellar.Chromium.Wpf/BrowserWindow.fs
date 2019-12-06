namespace Interstellar.Chromium.Wpf
open System
open System.Windows
open System.Windows.Controls
open CefSharp
open CefSharp.Wpf
open Interstellar

type BrowserWindow(?initialAddress: string) as this =
    inherit Window()

    let browser = new CefSharp.Wpf.ChromiumWebBrowser()

    // (primary) constructor
    do
        this.Content <- browser
        initialAddress |> Option.iter (fun x ->
            browser.Address <- x
        )

    interface IBrowserWindow with
        member this.Engine = BrowserEngineType.Chromium
        member this.Platform = BrowserPlatformType.WindowsWpf
        member this.Address = browser.Address
        member this.Load address = browser.Load address
        member this.Reload () = browser.Reload ()
        member this.PageTitle = browser.Title
        [<CLIEvent>]
        member this.PageTitleChanged : IEvent<string> =
            browser.TitleChanged |> Event.map (fun (e: DependencyPropertyChangedEventArgs) -> e.NewValue :?> string)
        member this.Title
            with get () = (this :> Window).Title
            and set title = (this :> Window).Title <- title

    member inline private this.I = this :> IBrowserWindow

    member this.Engine = this.I.Engine
    member this.Platform = this.I.Platform
    member this.Address with get () = this.I.Address
    member this.Load address = this.I.Load address
    [<CLIEvent>] member this.PageTitleChanged = this.I.PageTitleChanged