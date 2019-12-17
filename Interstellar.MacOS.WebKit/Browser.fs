namespace Interstellar.MacOS.WebKit
open System
open AppKit
open Foundation
open Interstellar
open WebKit

type Browser(config: BrowserWindowConfig, wkBrowser: WKWebView) =
    do
        match config.address, config.html with
        | Some address, Some html -> wkBrowser.LoadHtmlString (html, new NSUrl(address)) |> ignore
        | None, Some html -> wkBrowser.LoadHtmlString (html, null) |> ignore
        | Some address, None -> wkBrowser.LoadRequest (new NSUrlRequest(new NSUrl(address))) |> ignore
        | None, None -> ()

    member this.WebKitBrowser = wkBrowser

    interface IBrowser with
        member this.Address = raise (new NotImplementedException())
        member this.AreDevToolsShowing = false
        member this.CloseDevTools () = raise (new NotImplementedException())
        member this.CanGoBack = raise (new NotImplementedException())
        member this.CanGoForward = raise (new NotImplementedException())
        member this.Engine = BrowserEngine.AppleWebKit
        member this.ExecuteJavascript code = wkBrowser.EvaluateJavaScript (code, null)
        member this.Load address = wkBrowser.LoadRequest (new NSUrlRequest(new NSUrl(address))) |> ignore
        member this.LoadString (html, ?uri) =
            let nsUrl = match uri with | Some uri -> new NSUrl(uri) | None -> null
            wkBrowser.LoadHtmlString (html, nsUrl) |> ignore
        member this.GoBack () = raise (new NotImplementedException())
        member this.GoForward () = raise (new NotImplementedException())
        member this.PageTitle = raise (new NotImplementedException())
        [<CLIEvent>]
        member this.PageLoaded : IEvent<_> = raise (new NotImplementedException())
        member this.Reload () = raise (new NotImplementedException())
        [<CLIEvent>]
        member this.PageTitleChanged : IEvent<_> = raise (new NotImplementedException())
        member this.ShowDevTools () = raise (new NotImplementedException())