namespace Interstellar
open System
open System.Threading

type BrowserEngineType = | Chromium = 0 | WebKit = 0b1
type BrowserPlatformType = | WindowsWpf = 0 | MacOS = 0b1

type IBrowserWindow =
    inherit IDisposable
    abstract Engine : BrowserEngineType
    abstract Platform : BrowserPlatformType
    abstract Address : string
    abstract Show : unit -> Async<unit>
    [<CLIEvent>] abstract Shown : IEvent<unit>
    abstract Close : unit -> unit
    [<CLIEvent>] abstract Closed : IEvent<unit>
    abstract Load : uri:string -> unit
    abstract LoadString : html: string * ?uri: string -> unit
    abstract Reload : unit -> unit
    abstract PageTitle : string
    [<CLIEvent>] abstract PageTitleChanged: IEvent<string>
    abstract Title : string with get, set

type BrowserWindowConfig =
    { address: string option; html: string option }
    static member DefaultValue = { address = None; html = None }

type BrowserWindowCreator = BrowserWindowConfig -> IBrowserWindow

type BrowserApp = {
    onStart : SynchronizationContext -> BrowserWindowCreator -> Async<unit>
}

module BrowserApp =
    /// A do-nothing application
    let zero = { onStart = fun _ _ -> async { () } }
    /// Creates an application with the given onStart function
    let create onStart = { onStart = onStart }
    let openAddress address = { onStart = fun mainCtx createWindow -> async {
            do! Async.SwitchToContext mainCtx
            let window = createWindow { BrowserWindowConfig.DefaultValue with address = Some address }
            do! window.Show ()
        }
    }

[<AutoOpen>]
module Core =
    let defaultBrowserWindowConfig = BrowserWindowConfig.DefaultValue