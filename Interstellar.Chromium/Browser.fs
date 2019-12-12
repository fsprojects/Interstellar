namespace Interstellar.Chromium
open Interstellar
open CefSharp
open System.Threading
open System.Diagnostics

/// CefSharp has an IBrowser interface which doesn't express all the functionality we need, but the several implementations of it do.
/// They actually each share a lot of duplicated methods and code which aren't in the interface for some reason. This record extracts
/// those mostly identical methods into a single shared point so we can still avoid violating DRY all the time
type SharedChromiumBrowserInternals = {
    getPageTitle: unit -> string
    titleChanged: IEvent<string>
    isBrowserInitializedChanged: IEvent<unit>
}

type Browser(browser: IWebBrowser, browserInternals: SharedChromiumBrowserInternals, config: BrowserWindowConfig) =
    // (primary) constructor
    do
        browserInternals.isBrowserInitializedChanged.Add (fun x ->
            if browser.IsBrowserInitialized then
                match config.address, config.html with
                | address, Some html -> browser.LoadHtml (html, Option.toObj address) |> ignore
                | Some address, None -> browser.Load address
                | None, None -> ()
                if config.showDevTools then browser.ShowDevTools()
        )

    member this.ChromiumBrowser = browser

    interface Interstellar.IBrowser with
        member this.AreDevToolsShowing = browser.GetBrowserHost().HasDevTools
        member this.Address = browser.Address
        member this.CloseDevTools () = browser.CloseDevTools ()
        member this.Engine = BrowserEngineType.Chromium
        member this.ExecuteJavascript code = browser.ExecuteScriptAsync code
        member this.Load address = browser.Load address
        member this.LoadString (html, ?uri) =
            match uri with
            | Some uri -> browser.LoadHtml (html, uri) |> ignore
            | None -> browser.LoadHtml html
        member this.Platform = BrowserPlatformType.WindowsWpf
        member this.PageTitle = browserInternals.getPageTitle ()
        [<CLIEvent>]
        member this.PageTitleChanged : IEvent<string> = browserInternals.titleChanged
        member this.Reload () = browser.Reload ()
        member this.ShowDevTools () = browser.ShowDevTools ()