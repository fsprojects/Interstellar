namespace Interstellar.MacOS.Internal
open System
open AppKit
open CoreGraphics
open Foundation
open Interstellar
open WebKit
type WebKitViewDialogueHandler() =
    inherit WKUIDelegate()

    override this.RunJavaScriptAlertPanel (webView, message, frame, completionHandler) =
        let alert = new NSAlert(AlertStyle = NSAlertStyle.Informational, InformativeText = message, MessageText = "Alert")
        let result = alert.RunModal ()
        completionHandler.Invoke ()

namespace Interstellar.MacOS.WebKit
open System
open AppKit
open CoreGraphics
open Foundation
open Interstellar
open Interstellar.MacOS.Internal
open WebKit

type Browser(config: BrowserWindowConfig) =
    let wkBrowser = new WKWebView(CGRect.Empty, new WKWebViewConfiguration())
    let pageLoaded = new Event<EventArgs>()
    let pageTitleChanged = new Event<string>()
    let jsMsgRecieved = new Event<string>()
    let mutable pageTitleObserverHandle = null

    static let wkBridgeName = "interstellarWkBridge"

    do
        wkBrowser.NavigationDelegate <- {
            new WKNavigationDelegate() with
                member this.DidFinishNavigation (view, nav) =
                    pageLoaded.Trigger (new EventArgs())
        }
        wkBrowser.UIDelegate <- new WebKitViewDialogueHandler()

        wkBrowser.Configuration.UserContentController.AddScriptMessageHandler ({
            new WKScriptMessageHandler() with
                member this.DidReceiveScriptMessage (_, msg: WKScriptMessage) =
                    let msgAsString = (msg.Body :?> NSString).ToString()
                    jsMsgRecieved.Trigger msgAsString
        }, wkBridgeName)

        // TODO: dispose this
        pageTitleObserverHandle <-
            wkBrowser.AddObserver
                (new NSString("title"), NSKeyValueObservingOptions.New, fun x ->
                    pageTitleChanged.Trigger (wkBrowser.Title))

        match config.address, config.html with
        | Some address, Some html -> wkBrowser.LoadHtmlString (html, new NSUrl(address)) |> ignore
        | None, Some html -> wkBrowser.LoadHtmlString (html, null) |> ignore
        | Some address, None -> wkBrowser.LoadRequest (new NSUrlRequest(new NSUrl(address))) |> ignore
        | None, None -> ()

    member this.WebKitBrowser = wkBrowser

    interface IBrowser with
        member this.Address =
            match wkBrowser.Url with
            | null -> None
            | url -> Some (new Uri(url.AbsoluteString))
        member this.AreDevToolsShowing = false
        member this.CloseDevTools () = ()
        member this.CanGoBack = wkBrowser.CanGoBack
        member this.CanGoForward = wkBrowser.CanGoForward
        member this.Engine = BrowserEngine.AppleWebKit
        member this.ExecuteJavascript code = wkBrowser.EvaluateJavaScript (code, null)
        member this.Load address = wkBrowser.LoadRequest (new NSUrlRequest(new NSUrl(address))) |> ignore
        member this.LoadString (html, ?uri) =
            let nsUrl = match uri with | Some uri -> new NSUrl(uri) | None -> null
            wkBrowser.LoadHtmlString (html, nsUrl) |> ignore
        member this.GoBack () = wkBrowser.GoBack () |> ignore
        member this.GoForward () = wkBrowser.GoForward () |> ignore
        [<CLIEvent>]
        member this.JavascriptMessageRecieved = jsMsgRecieved.Publish
        member this.PageTitle = wkBrowser.Title
        [<CLIEvent>]
        member this.PageLoaded : IEvent<_> = pageLoaded.Publish
        member this.Reload () = wkBrowser.Reload () |> ignore
        [<CLIEvent>]
        member this.PageTitleChanged : IEvent<_> = pageTitleChanged.Publish
        member this.ShowDevTools () = () // there's no way that I know of to programmatically open the WKWebView inspector