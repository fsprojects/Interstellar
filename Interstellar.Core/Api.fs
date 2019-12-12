namespace Interstellar
open System
open System.Threading

type BrowserEngineType = | Chromium = 0 | WebKit = 0b1
type BrowserPlatformType = | WindowsWpf = 0 | MacOS = 0b1

type IBrowser =
    abstract Address : string
    abstract AreDevToolsShowing : bool
    abstract CloseDevTools : unit -> unit
    abstract Engine : BrowserEngineType
    /// <summary>Executes some Javascript in the browser, returning immediately.</summary>
    abstract ExecuteJavascript : string -> unit
    abstract Load : uri:string -> unit
    abstract LoadString : html: string * ?uri: string -> unit
    abstract PageTitle : string
    abstract Platform : BrowserPlatformType
    abstract Reload : unit -> unit
    [<CLIEvent>] abstract PageTitleChanged: IEvent<string>
    abstract ShowDevTools : unit -> unit

type IBrowserWindow =
    inherit IDisposable
    abstract Browser : IBrowser
    abstract Close : unit -> unit
    [<CLIEvent>] abstract Closed : IEvent<unit>
    abstract Show : unit -> Async<unit>
    [<CLIEvent>] abstract Shown : IEvent<unit>
    abstract Title : string with get, set

type BrowserWindowConfig =
    { address: string option; html: string option; showDevTools: bool }
    static member DefaultValue = { address = None; html = None; showDevTools = false }

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