﻿open Examples.SharedCode
open Gtk
open Interstellar
open Interstellar.GtkSharp.Webkit
open WebKit

module Main =
    let runApp () =
        let onMainWindowCreated (w: IBrowserWindow<Window>) =
            let nativeWindow = w.NativeWindow
            // This is where you could call some GTK-specific APIs on this window
            ()
        BrowserApp.run (SimpleBrowserApp.app onMainWindowCreated)
    
    [<EntryPoint>]
    let main argv =
        Application.Init ()
        
        // let wv = new WebView(WidthRequest = 400, HeightRequest = 100, Hexpand = true)
        // let window = new Window("WebView Sample")
        // window.Add wv
        // wv.LoadUri "https://en.wikipedia.org/"
        // window.DeleteEvent.Add (fun _ -> Application.Quit ())
        
        // window.ShowAll ()
        
        runApp ()
        
        Application.Run ()
        0
