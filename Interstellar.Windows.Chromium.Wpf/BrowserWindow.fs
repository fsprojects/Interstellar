namespace Interstellar.Windows.Chromium.Wpf
open System
open System.Windows
open System.Windows.Controls
open CefSharp
open CefSharp.Wpf
open Interstellar.Core

type BrowserWindow() as this =
    inherit Window()

    let browser = new CefSharp.Wpf.ChromiumWebBrowser()

    // (primary) constructor
    do
        this.Content <- browser

    interface IBrowserWindow with
        member this.Engine = BrowserEngineType.Chromium
        member this.Platform = BrowserPlatformType.WindowsWpf
        member this.Address
            with get () = browser.Address
            and set address = browser.Address <- address
        member this.Title = browser.Title
        [<CLIEvent>]
        member this.TitleChanged : IEvent<BrowserWindowTitleChangedEventArgs> =
            browser.TitleChanged |> Event.map (fun (e: DependencyPropertyChangedEventArgs) ->
                new BrowserWindowTitleChangedEventArgs(e.OldValue :?> string, e.NewValue :?> string))

    member inline private this.I = this :> IBrowserWindow

    member this.Engine = this.I.Engine
    member this.Platform = this.I.Platform
    member this.Address with get () = this.I.Address and set x = (this.I.Address <- x)
    member this.Title = this.I
    [<CLIEvent>] member this.TitleChanged = this.I.TitleChanged