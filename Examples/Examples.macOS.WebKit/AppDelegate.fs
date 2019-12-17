namespace Example.macOS.WebKit
open System
open System.Threading
open AppKit
open Foundation
open Interstellar
open Interstellar.MacOS.WebKit
open Examples.SharedCode

[<Register("AppDelegate")>]
type AppDelegate() =
    inherit NSApplicationDelegate()

    override this.ApplicationShouldTerminateAfterLastWindowClosed sender = false

    override this.DidFinishLaunching notification =
        printfn "DidFinishLaunching"
        Thread.CurrentThread.Name <- "Main"

        let w = new Internal.BrowserWindow(BrowserWindowConfig.DefaultValue)
        (w :> IBrowserWindow).Show () |> Async.StartImmediate

        let mainCtx = SynchronizationContext.Current
        Async.Start <| async {
            do! BrowserApp.runAsync mainCtx SimpleBrowserApp.app
            NSApplication.SharedApplication.Terminate null
        }

        ()