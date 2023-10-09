namespace Interstellar.GtkSharp.Webkit
open System
open Gtk
open Interstellar
open Interstellar.Core
open WebKit

type Browser(config: BrowserWindowConfig<Window>) =
    let wc = new WebContext()
    let wkBrowser = new WebView(wc)
    
    let pageLoaded = new Event<EventArgs>()
    let pageTitleChanged = new Event<string>()
    let jsMsgReceived = new Event<string>()
    
    let wkBridgeName = "interstellarWkBridge"
    
    do
        wkBrowser.Settings.EnableDeveloperExtras <- true
        wkBrowser.Settings.EnableWriteConsoleMessagesToStdout <- true
        wkBrowser.UserContentManager.ScriptMessageReceived.Add (fun e ->
            let msgAsStr = e.JsResult.JsValue |> string
            jsMsgReceived.Trigger msgAsStr
        )
        let scriptSrc =
            sprintf
                "window.interstellarBridge={'postMessage':function(message){window.webkit.messageHandlers.%s.postMessage(message)}}"
                wkBridgeName
        wkBrowser.UserContentManager.AddScript
            (new UserScript(scriptSrc, UserContentInjectedFrames.AllFrames, UserScriptInjectionTime.Start, null, null))
        if not (wkBrowser.UserContentManager.RegisterScriptMessageHandler(wkBridgeName)) then
            eprintfn "Failed to register script message handler; JS bridge will not work"
        
        wkBrowser.LoadChanged.Add (fun e ->
             // printfn $"Load changed: %A{e.LoadEvent}"
             // e.Args |> Array.map string |> String.concat "," |> printfn "Args: (%d) %s" e.Args.Length
             match e.LoadEvent with
             | LoadEvent.Finished -> pageLoaded.Trigger (EventArgs())
             | _ -> ()
        )
        
        match config.address, config.html with
        | Some address, Some html -> wkBrowser.LoadHtml (html, string address)
        | None, Some html -> wkBrowser.LoadHtml html
        | Some address, None -> wkBrowser.LoadUri (string address)
        | None, None -> ()
    
    member this.WebKitBrowser = wkBrowser
    
    member this.AwaitLoadedThenJSReady () = async {
        if wkBrowser.IsLoading then
            let! _ = Async.AwaitEvent (this :> IBrowser).PageLoaded
            ()
        return async { () }
    }
    
    interface IBrowser with
        member this.CloseDevTools () = ()
        member this.Address = wkBrowser.Uri |> Option.ofObj |> Option.map Uri
        member this.AreDevToolsShowing = false // TODO: implement me
        member this.CanGoBack = wkBrowser.CanGoBack ()
        member this.CanGoForward = wkBrowser.CanGoForward ()
        member this.CanShowDevTools = true
        member this.Engine = BrowserEngine.GtkWebKit
        member this.ExecuteJavascript code = wkBrowser.RunJavascript code
        member this.GoBack () = wkBrowser.GoBack ()
        member this.GoForward () = wkBrowser.GoForward ()
        [<CLIEvent>]
        member this.JavascriptMessageRecieved = jsMsgReceived.Publish
        member this.Load uri = wkBrowser.LoadUri (string uri)
        member this.LoadAsync uri = async {
            wkBrowser.LoadUri (string uri)
            return! this.AwaitLoadedThenJSReady ()
        }
        member this.LoadString (html, uri) = wkBrowser.LoadHtml (html, string uri)
        member this.LoadStringAsync (html, uri) = async {
            wkBrowser.LoadHtml (html, string uri)
            return! this.AwaitLoadedThenJSReady ()
        }
        [<CLIEvent>]
        member this.PageLoaded = pageLoaded.Publish
        member this.PageTitle = wkBrowser.Title
        [<CLIEvent>]
        member this.PageTitleChanged = pageTitleChanged.Publish
        member this.Reload() = wkBrowser.Reload ()
        member this.ShowDevTools() =
            wkBrowser.Inspector.Show ()
