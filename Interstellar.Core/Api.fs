namespace Interstellar
open System

type BrowserEngineType = | Chromium = 0 | WebKit = 0b1
type BrowserPlatformType = | WindowsWpf = 0 | MacOS = 0b1

type IBrowserWindow =
    abstract Engine : BrowserEngineType
    abstract Platform : BrowserPlatformType
    abstract Address : string
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
    let create<'a> onStart = { onStart = onStart }

[<AutoOpen>]
module Core =
    let defaultBrowserWindowConfig = BrowserWindowConfig.DefaultValue