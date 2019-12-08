namespace Interstellar
open System

type BrowserEngineType = | Chromium = 0 | WebKit = 0b1
type BrowserPlatformType = | WindowsWpf = 0 | MacOS = 0b1

type IBrowserWindow =
    inherit IDisposable
    abstract Engine : BrowserEngineType
    abstract Platform : BrowserPlatformType
    abstract Address : string
    abstract Show : unit -> unit
    [<CLIEvent>] abstract Shown : IEvent<unit>
    abstract Close : unit -> unit
    [<CLIEvent>] abstract Closed : IEvent<unit>
    abstract Load : string -> unit
    abstract Reload : unit -> unit
    abstract PageTitle : string
    [<CLIEvent>] abstract PageTitleChanged: IEvent<string>
    abstract Title : string with get, set

type BrowserWindowConfig =
    { initialAddress: string option }
    static member DefaultValue = { initialAddress = None }

type BrowserWindowCreator = BrowserWindowConfig -> IBrowserWindow

type BrowserApp = {
    onStart : BrowserWindowCreator -> Async<unit>
}

module BrowserApp =
    /// A do-nothing application
    let zero = { onStart = fun _ -> async { () } }
    /// Creates an application with the given onStart function
    let create onStart = { onStart = onStart }
    let openAddress = { onStart = fun createWindow -> async {
            let window = createWindow { BrowserWindowConfig.DefaultValue with initialAddress = Some "" }
            window.Show ()
            ()
        }
    }

[<AutoOpen>]
module Core =
    let defaultBrowserWindowConfig = BrowserWindowConfig.DefaultValue