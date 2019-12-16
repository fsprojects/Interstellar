namespace Interstellar.MacOS.WebKit.Internal
open System
open AppKit
open CoreGraphics
open Foundation
open Interstellar
open Interstellar.MacOS.WebKit
open WebKit

//type BrowserWindow() =
    //inherit NSViewController()

    //let wkBrowser = new WKWebView(CGRect.Empty, new WKWebViewConfiguration())
    //let browser = new Browser(wkBrowser)

    //do
    //    browser.WebKitBrowser
    //    ()

    //member this.WebKitBrowser = wkBrowser
    //member this.Browser = browser

    //override this.LoadView () =
    //    base.View <- new NSView()

    ////member this.View = view

//type BrowserWindowView() = inherit NSWindow()

type NiblessViewController(view: NSView) =
    inherit NSViewController()

    override this.LoadView () =
        base.View <- view

type BrowserWindow(config: BrowserWindowConfig) as this =
    inherit NSWindowController("BrowserWindow")

    let wkBrowser = new WKWebView(CGRect.Empty, new WKWebViewConfiguration())
    //let wkBrowserController = new NSViewController()
    let browser = new Browser(wkBrowser)

    let shown = new Event<_>()

    do
        let wkBrowserController = {
            new NiblessViewController(wkBrowser) with
                override this.ViewDidAppear () =
                    base.ViewDidAppear ()
                    shown.Trigger ()
        }
        this.Window <-
            new NSWindow(new CGRect (0., 0., 1000., 500.),
                         NSWindowStyle.Titled ||| NSWindowStyle.Closable ||| NSWindowStyle.Miniaturizable ||| NSWindowStyle.Resizable,
                         NSBackingStore.Buffered, false, Title = "My Window")
        this.Window.ContentView <- wkBrowser
        this.Window.ContentViewController <- wkBrowserController
        //wkBrowser.LoadRequest (new NSUrlRequest(new NSUrl("https://google.com/"))) |> ignore
        this.Window.Center ()
        this.Window.AwakeFromNib ()


    member this.WKBrowserView = wkBrowser
    member this.WKBrowser = browser

    override this.LoadWindow () =
        base.LoadWindow ()

    //override this.LoadWindow () =
        ////let window = new NSWindow()
        ////window.ContentView <- wkBrowser
        ////window.IsVisible <- true

        ////wkBrowser.AddConstraints [|
        ////    NSLayoutConstraint.Create(this, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, wkBrowser, NSLayoutAttribute.Leading, nfloat 1., nfloat 0.)
        ////    NSLayoutConstraint.Create(this, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, wkBrowser, NSLayoutAttribute.Trailing, nfloat 1., nfloat 0.)
        ////    NSLayoutConstraint.Create(this, NSLayoutAttribute.Top, NSLayoutRelation.Equal, wkBrowser, NSLayoutAttribute.Top, nfloat 1., nfloat 0.)
        ////    NSLayoutConstraint.Create(this, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, wkBrowser, NSLayoutAttribute.Bottom, nfloat 1., nfloat 0.)
        ////|]
        //base.LoadWindow ()
        //()

    interface IBrowserWindow with
        member this.Browser = upcast browser
        member this.Close () = (this :> NSWindowController).Close ()
        member this.Platform = BrowserWindowPlatform.MacOS
        [<CLIEvent>]
        member this.Closed = (this :> NSWindowController).Window.WillClose |> Event.map (fun x -> ())
        member this.Show () = async {
            (this :> NSWindowController).ShowWindow this
        }
        [<CLIEvent>]
        member this.Shown = shown.Publish
        member this.Title
            with get () = base.Window.Title
            and set x = base.Window.Title <- x