namespace Interstellar.GtkSharp.Webkit
open System
open Gtk
open Interstellar
open Interstellar.Core
open WebKit

type BrowserWindow(config: BrowserWindowConfig<Window>) as this =
    inherit Window("")
    
    let browser = new Browser(config)

    let closedEvt = new Event<_>()
    let shownEvt = new Event<_>()
    let disposedEvt = new Event<_>()
    
    do
        this.DeleteEvent.Add (fun _ -> closedEvt.Trigger ())
        this.Add browser.WebKitBrowser
    
    interface IBrowserWindow<Window> with
        member this.Browser = upcast browser
        member this.Close () = this.Close ()
        [<CLIEvent>]
        member val Closed = closedEvt.Publish
        member this.IsShowing = this.IsVisible
        member this.Show () = async {
            this.ShowAll ()
        }
        member this.NativeWindow = this
        member this.Platform = BrowserWindowPlatform.Gtk
        [<CLIEvent>]
        member val Shown = shownEvt.Publish
        member this.Size
            with get () =
                let mutable w = 0
                let mutable h = 0
                this.GetSize (&w, &h)
                float w, float h
            and set (width, height) =
                this.SetSizeRequest (int width, int height)
        member this.Title
            with get () = (this :> Window).Title
            and set x = (this :> Window).Title <- x
        