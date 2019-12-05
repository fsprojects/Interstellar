namespace Interstellar.Chromium.Wpf
open System
open System.Windows
open System.Windows.Controls
open CefSharp
open CefSharp.Wpf
open Interstellar.Core

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
        member this.Title = browser.Title
        [<CLIEvent>]
        member this.TitleChanged : IEvent<string> =
            browser.TitleChanged |> Event.map (fun (e: DependencyPropertyChangedEventArgs) -> e.NewValue :?> string)

    member inline private this.I = this :> IBrowserWindow

    member this.Engine = this.I.Engine
    member this.Platform = this.I.Platform
    member this.Address with get () = this.I.Address
    member this.Load address = this.I.Load address
    member this.Title = this.I
    [<CLIEvent>] member this.TitleChanged = this.I.TitleChanged