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

    interface IDisposable with
        member this.Dispose () = this.Close ()

    interface IBrowserWindow with
        member this.Engine = BrowserEngineType.Chromium
        member this.Platform = BrowserPlatformType.WindowsWpf
        member this.Address = browser.Address
        member this.Show () = (this :> Window).Show ()
        [<CLIEvent>] member this.Shown = failwith "bang" :> IEvent<unit>
        member this.Close () = (this :> Window).Close ()
        [<CLIEvent>] member this.Closed = (this :> Window).Closed |> Event.map ignore
        member this.Load address = browser.Load address
        member this.Reload () = browser.Reload ()
        member this.PageTitle = browser.Title
        [<CLIEvent>]
        member this.PageTitleChanged : IEvent<string> =
            browser.TitleChanged |> Event.map (fun (e: DependencyPropertyChangedEventArgs) -> e.NewValue :?> string)
        member this.Title
            with get () = (this :> Window).Title
            and set title = (this :> Window).Title <- title