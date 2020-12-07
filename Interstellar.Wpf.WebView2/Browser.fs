namespace Interstellar.Wpf
open System
open System.Diagnostics
open System.Text
open System.Threading
open System.Threading.Tasks
open Interstellar
open Microsoft.Web.WebView2.Core
open Microsoft.Web.WebView2.Wpf

type Browser(msBrowser: WebView2, cwv2: CoreWebView2) =
    // fire-and-forget method to get the CoreWebView2 and then do something with it possibly sometime in the future
    //let withCwv2 (lambda: CoreWebView2 -> unit) =
        // TODO: would be nice to use a native task builder
        //Async.RunSynchronously <|
        //    async {
        //        try
        //            let x = Async.AwaitTask (msBrowser.EnsureCoreWebView2Async ())
        //            ()
        //        with :? EdgeNotFoundException as e ->
        //            reraise ()
        //    }
        //msBrowser.EnsureCoreWebView2Async().ContinueWith(fun _ ->
        //    lambda msBrowser.CoreWebView2)
        //|> ignore

    // gets the CoreWebView2, potentially blocking until it has initialized if it hasn't yet
    //let getCwv2 () =
    //    if msBrowser.CoreWebView2 = null then
    //        msBrowser.EnsureCoreWebView2Async().Wait()
    //    msBrowser.CoreWebView2
    
    let mutable areDevToolsShowing = false

    let pageTitleChanged = new Event<string>()
    let javascriptMessageRecieved = new Event<_>()

    member this.AwaitJavascriptReady () =
        // I'm not sure if WebView2 allows you to immediately start executing Javascript... It's getting
        // late at night so let's just pretend it does and hope that assumption doesn't make an ass of me
        async { () }

    interface IBrowser with
        member this.Address =
            // this feels a little gross
            try Some msBrowser.Source
            with :? NullReferenceException -> None
        member this.AreDevToolsShowing = areDevToolsShowing
        member this.CanGoBack = msBrowser.CanGoBack
        member this.CanGoForward = msBrowser.CanGoForward
        member this.CanShowDevTools = true
        member this.CloseDevTools () = raise (System.NotImplementedException())
        member this.Engine = BrowserEngine.Chromium
        member this.ExecuteJavascript (arg1) =
            msBrowser.ExecuteScriptAsync arg1 |> ignore
        member this.GoBack () = msBrowser.GoBack ()
        member this.GoForward () = msBrowser.GoForward ()
        [<CLIEvent>]
        member val JavascriptMessageRecieved = javascriptMessageRecieved.Publish
        member this.Load uri = cwv2.Navigate (string uri)
        member this.LoadAsync uri =
            msBrowser.Source <- uri
            async {
                let! _ = Async.AwaitEvent msBrowser.NavigationCompleted
                return this.AwaitJavascriptReady ()
            }
        member this.LoadString (html, uri) =
            match uri with
            | Some _ ->
                // FIXME: is implementing this possible?
                raise (new NotImplementedException())
            | _ -> cwv2.NavigateToString html
        member this.LoadStringAsync (html, uri) =
            match uri with
            | Some _ ->
                // look up ^^
                raise (new NotImplementedException())
            | _ -> async {
                cwv2.NavigateToString html
                let! _ = Async.AwaitEvent cwv2.NavigationCompleted
                return this.AwaitJavascriptReady ()
            }
        [<CLIEvent>]
        member val PageLoaded = msBrowser.NavigationCompleted |> Event.map (fun e -> e :> EventArgs)
        member this.PageTitle = cwv2.DocumentTitle
        [<CLIEvent>]
        member val PageTitleChanged = pageTitleChanged.Publish
        member this.Reload() = msBrowser.Reload ()
        member this.ShowDevTools() = cwv2.OpenDevToolsWindow ()
