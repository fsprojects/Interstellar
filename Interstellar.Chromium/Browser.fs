namespace Interstellar.Chromium
open System.Threading
open System.Diagnostics
open System
open CefSharp
open CefSharp.JSInjectorResponseFilter
open Interstellar

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
        browser.RequestHandler <- new JSInjectionRequestHandler("window.interstellarBridge={'postMessage':function(message){CefSharp.PostMessage(message)}};console.log('Injected script executed')")
        browserInternals.isBrowserInitializedChanged.Add (fun x ->
            if browser.IsBrowserInitialized then
                match config.address, config.html with
                | address, Some html -> browser.LoadHtml (html, Option.toObj address) |> ignore
                | Some address, None -> browser.Load address
                | None, None -> ()
                if config.showDevTools then browser.ShowDevTools()
        )
        browser.ExecuteScriptAsyncWhenPageLoaded "console.log('ExecuteScriptAsyncWhenPageLoaded')"
        browser.FrameLoadStart.Add (fun x ->
            browser.GetMainFrame().ExecuteJavaScriptAsync "console.log('FrameLoadStart')"
            ()
        )
        browser.RenderProcessMessageHandler <- {
            new IRenderProcessMessageHandler with
                member this.OnContextCreated (wb,_,_) =
                    wb.ExecuteScriptAsync "console.log('OnContextCreated')"
                    ()
                member this.OnContextReleased (_,_,_) = ()
                member this.OnFocusedNodeChanged (_,_,_,_) = ()
                member this.OnUncaughtException (_,_,_,_) = ()
        }

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
        member this.Load address = browser.Load address
        member this.LoadString (html, ?uri) =
            match uri with
            | Some uri -> browser.LoadHtml (html, uri) |> ignore
            | None -> browser.LoadHtml html
        [<CLIEvent>]
        member this.JavascriptMessageRecieved : IEvent<string> = browser.JavascriptMessageReceived |> Event.map (fun x -> x.ConvertMessageTo<string>())
        member this.PageTitle = browserInternals.getPageTitle ()
        [<CLIEvent>]
        member this.PageTitleChanged : IEvent<string> = browserInternals.titleChanged
        [<CLIEvent>]
        member this.PageLoaded : IEvent<EventArgs> = browser.FrameLoadEnd |> Event.map (fun x -> upcast x)
        member this.Reload () = browser.Reload ()
        member this.ShowDevTools () = browser.ShowDevTools ()