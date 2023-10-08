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
    
    do
        wkBrowser.Settings.EnableDeveloperExtras <- true
        wkBrowser.Settings.EnableWriteConsoleMessagesToStdout <- true
        wkBrowser.UserContentManager.ScriptMessageReceived.Add (fun e ->
            printfn "%A" e
        )
        let s = UserScript("", UserContentInjectedFrames.AllFrames, UserScriptInjectionTime.Start, null, null)
        wkBrowser.UserContentManager.AddScript(s)
        if not (wkBrowser.UserContentManager.RegisterScriptMessageHandler("interstellarBridge")) then
            eprintfn "Failed to register script message handler; JS bridge will not work"
        
        match config.address, config.html with
        | Some address, Some html -> wkBrowser.LoadHtml (html, string address)
        | None, Some html -> wkBrowser.LoadHtml html
        | Some address, None -> wkBrowser.LoadUri (string address)
        | None, None -> ()
    
    member this.WebKitBrowser = wkBrowser
    
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
        member this.LoadAsync uri = async { return async { wkBrowser.LoadUri (string uri) } }
        member this.LoadString (html, uri) = wkBrowser.LoadHtml (html, string uri)
        member this.LoadStringAsync (html, uri) = async { return async { wkBrowser.LoadHtml (html, string uri) } }
        [<CLIEvent>]
        member this.PageLoaded = pageLoaded.Publish
        member this.PageTitle = wkBrowser.Title
        [<CLIEvent>]
        member this.PageTitleChanged = pageTitleChanged.Publish
        member this.Reload() = wkBrowser.Reload ()
        member this.ShowDevTools() =
            wkBrowser.Inspector.Show ()
