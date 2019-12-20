namespace Interstellar.Chromium
open System.Threading
open System.Diagnostics
open System
open CefSharp
open Interstellar
open CefSharp.Web

/// CefSharp has an IBrowser interface which doesn't express all the functionality we need, but the several implementations of it do.
/// They actually each share a lot of duplicated methods and code which aren't in the interface for some reason. This record extracts
/// those mostly identical methods into a single shared point so we can still avoid violating DRY all the time
type SharedChromiumBrowserInternals = {
    getPageTitle: unit -> string
    titleChanged: IEvent<string>
    isBrowserInitializedChanged: IEvent<unit>
}

type Browser(browser: IWebBrowser, browserInternals: SharedChromiumBrowserInternals, config: BrowserWindowConfig) as this =
    // (primary) constructor
    do
        browser.RequestHandler <- new JSInjectionRequestHandler("window.interstellarBridge={'postMessage':function(message){CefSharp.PostMessage(message)}}")
        browserInternals.isBrowserInitializedChanged.Add (fun () ->
            if browser.IsBrowserInitialized then
                match config.address, config.html with
                | address, Some html -> (this :> IBrowser).LoadString (html, ?uri = address) |> ignore
                | Some address, None -> (this :> IBrowser).Load address
                | None, None -> ()
                if config.showDevTools then (this :> IBrowser).ShowDevTools()
        )

    member this.ChromiumBrowser = browser

    interface Interstellar.IBrowser with
        member this.AreDevToolsShowing = browser.GetBrowserHost().HasDevTools
        member this.Address =
            match browser.Address with
            | null -> None
            | value -> Some (new Uri(value))
        member this.CloseDevTools () = browser.CloseDevTools ()
        member this.CanGoBack = browser.GetBrowser().CanGoBack
        member this.CanGoForward = browser.GetBrowser().CanGoForward
        member this.Engine = BrowserEngine.Chromium
        member this.ExecuteJavascript code = browser.ExecuteScriptAsync code
        member this.GoBack () = browser.GetBrowser().GoForward ()
        member this.GoForward () = browser.GetBrowser().GoBack ()
        member this.Load address = browser.Load address.OriginalString
        member this.LoadString (html, ?uri) =
            match uri with
            | Some uri ->
                // FIXME: at the moment, we can't use IWebBrowser.LoadHtml(string,string) because it installs a request handler to resolve the given URI,
                // which conflicts with the request handler _we_ have to install in order to inject Javascript (see JavascriptInjectionFilter and friends).
                // Not sure how to fix this yet, but I suspect it would involve writing our own handler that basically combines that two.
                raise (new PlatformNotSupportedException("Providing data along with a given origin URI is not yet supported in Chromium"))
                //browser.LoadHtml (html, uri) |> ignore
            | None ->
                // this one works fine because it still uses real URI behavior, thus not requiring any kind of custom URI handlers
                let data = new HtmlString(html, true)
                (this :> IBrowser).Load (new Uri(data.ToDataUriString()))
        [<CLIEvent>]
        member this.JavascriptMessageRecieved : IEvent<string> = browser.JavascriptMessageReceived |> Event.map (fun x -> x.ConvertMessageTo<string>())
        member this.PageTitle = browserInternals.getPageTitle ()
        [<CLIEvent>]
        member this.PageTitleChanged : IEvent<string> = browserInternals.titleChanged
        [<CLIEvent>]
        member this.PageLoaded : IEvent<EventArgs> = browser.FrameLoadEnd |> Event.map (fun x -> upcast x)
        member this.Reload () = browser.Reload ()
        member this.ShowDevTools () = browser.ShowDevTools ()