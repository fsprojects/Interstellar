namespace Interstellar.MacOS.Internal
open System
open System.Threading
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
open System.Threading
open AppKit
open CoreGraphics
open Foundation
open Interstellar
open Interstellar.MacOS.Internal
open WebKit

module internal BrowserHelpers =
    let loadString (wkBrowser: WKWebView, html: string, (uri: Uri option)) =
        let nsUrl = match uri with | Some uri -> new NSUrl(uri.OriginalString) | None -> null
        wkBrowser.LoadHtmlString (html, nsUrl) |> ignore

    let load (wkBrowser: WKWebView, uri: Uri) =
        printfn "loading uri: %A" uri
        let nsUrl = new NSUrl(uri.OriginalString)
        if uri.Scheme = "file" then
            wkBrowser.LoadFileUrl (nsUrl, nsUrl) |> ignore
        else
            wkBrowser.LoadRequest (new NSUrlRequest(nsUrl)) |> ignore

type Browser(config: BrowserWindowConfig<NSWindow>) =
    let wkBrowser = new WKWebView(CGRect.Empty, new WKWebViewConfiguration())
    let pageLoaded = new Event<EventArgs>()
    let pageTitleChanged = new Event<string>()
    let jsMsgRecieved = new Event<string>()
    let mutable pageTitleObserverHandle = null

    let owningThreadId = Thread.CurrentThread.ManagedThreadId

    static let wkBridgeName = "interstellarWkBridge"

    do
        wkBrowser.NavigationDelegate <- {
            new WKNavigationDelegate() with
                member this.DidFinishNavigation (view, nav) =
                    pageLoaded.Trigger (new EventArgs())
                    pageTitleChanged.Trigger view.Title
        }
        wkBrowser.UIDelegate <- new WebKitViewDialogueHandler()

        // install JS->.Net bridge
        wkBrowser.Configuration.UserContentController.AddScriptMessageHandler ({
            new WKScriptMessageHandler() with
                member this.DidReceiveScriptMessage (_, msg: WKScriptMessage) =
                    let msgAsString = (msg.Body :?> NSString).ToString()
                    jsMsgRecieved.Trigger msgAsString
        }, wkBridgeName)
        // make it accessible in JS from window.interstellarBridge
        wkBrowser.Configuration.UserContentController.AddUserScript
            (new WKUserScript(new NSString (sprintf "window.interstellarBridge={'postMessage':function(message){window.webkit.messageHandlers.%s.postMessage(message)}}" wkBridgeName),
                              WKUserScriptInjectionTime.AtDocumentStart, false))

        // TODO: dispose this
        pageTitleObserverHandle <-
            wkBrowser.AddObserver
                (new NSString("title"), NSKeyValueObservingOptions.New, fun x ->
                    pageTitleChanged.Trigger (wkBrowser.Title))

        match config.address, config.html with
        | address, Some html -> BrowserHelpers.loadString (wkBrowser, html, address)
        | Some address, None -> BrowserHelpers.load (wkBrowser, address)
        | None, None -> ()

    member private this.CheckThread() =
        // check manually because some doing this for some operations will otherwise give nasty undebuggable crashes with no information
        // (for example ExecuteJavascript)
        if Thread.CurrentThread.ManagedThreadId <> owningThreadId then
            invalidOp "Browser methods may only be invoked from the thread it was created on" // TODO: nameof

    member this.WebKitBrowser = wkBrowser

    member this.AwaitLoadedThenJSReady () = async {
        if wkBrowser.IsLoading then
            let! _ = Async.AwaitEvent (this :> IBrowser).PageLoaded
            ()
        // it appears that in WebKit it's safe to execute JS right away, so I guess we're good
        return async { () }
    }

    interface IBrowser with
        member this.Address =
            match wkBrowser.Url with
            | null -> None
            | url -> Some (new Uri(url.AbsoluteString))
        member this.AreDevToolsShowing = false
        member this.CloseDevTools () = ()
        member this.CanGoBack =
            this.CheckThread ()
            wkBrowser.CanGoBack
        member this.CanGoForward =
            this.CheckThread ()
            wkBrowser.CanGoForward
        member this.CanShowDevTools = false
        member this.Engine =
            this.CheckThread ()
            BrowserEngine.AppleWebKit
        member this.ExecuteJavascript code =
            this.CheckThread ()
            wkBrowser.EvaluateJavaScript (code, null)
        member this.Load address =
            this.CheckThread ()
            BrowserHelpers.load (wkBrowser, address)
        member this.LoadAsync address = async {
            this.CheckThread ()
            (this :> IBrowser).Load address
            return! this.AwaitLoadedThenJSReady ()
        }
        member this.LoadString (html, ?uri) =
            this.CheckThread ()
            BrowserHelpers.loadString (wkBrowser, html, uri)
        member this.LoadStringAsync (html, ?uri) = async {
            this.CheckThread ()
            (this :> IBrowser).LoadString (html, ?uri = uri)
            return! this.AwaitLoadedThenJSReady ()
        }
        member this.GoBack () =
            this.CheckThread ()
            wkBrowser.GoBack () |> ignore
        member this.GoForward () =
            this.CheckThread ()
            wkBrowser.GoForward () |> ignore
        [<CLIEvent>]
        member val JavascriptMessageRecieved = jsMsgRecieved.Publish
        member this.PageTitle =
            this.CheckThread ()
            wkBrowser.Title
        [<CLIEvent>]
        member val PageLoaded : IEvent<_> = pageLoaded.Publish
        member this.Reload () =
            this.CheckThread ()
            wkBrowser.Reload () |> ignore
        [<CLIEvent>]
        member val PageTitleChanged : IEvent<_> = pageTitleChanged.Publish
        // there's no way that I know of to programmatically open the WKWebView inspector: https://stackoverflow.com/questions/25200116/how-to-show-the-inspector-within-your-wkwebview-based-desktop-app
        member this.ShowDevTools () = ()