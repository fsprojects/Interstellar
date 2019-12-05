namespace Interstellar.Core
open System

type BrowserEngineType = | Chromium = 0 | WebKit = 0b1
type BrowserPlatformType = | WindowsWpf = 0 | MacOS = 0b1

type IBrowserWindow =
    abstract Engine : BrowserEngineType
    abstract Platform : BrowserPlatformType
    abstract Address : string with get
    abstract Load : string -> unit
    abstract Reload : unit -> unit
    abstract Title : string
    [<CLIEvent>] abstract TitleChanged: IEvent<string>