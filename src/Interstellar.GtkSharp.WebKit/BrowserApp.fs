namespace Interstellar.GtkSharp.Webkit
open System
open System.Threading
open Gtk
open Interstellar
open Interstellar.Core
open WebKit

module BrowserApp =
    /// <summary>Starts and runs a BrowserApp's lifecycle in a Cocoa + WebKit host, asychronously</summary>
    /// <param name="mainCtx">indicates the thread that is to be used as the UI thread</param>
    /// <param name="app">Describes the application lifecycle</param>
    let runAsync mainCtx (app: BrowserApp<Window>) = async {
        do! Async.SwitchToContext mainCtx
        let windowCreator : BrowserWindowCreator<Window> = fun config ->
            let w = new BrowserWindow(config)
            BrowserWindowConfig.applyWindowTitle mainCtx w (w :> IBrowserWindow<_>).Closed config.title
            upcast w
        do! app.onStart mainCtx windowCreator
        do! Async.SwitchToContext mainCtx
    }

    let run app = Async.Start <| runAsync SynchronizationContext.Current app

