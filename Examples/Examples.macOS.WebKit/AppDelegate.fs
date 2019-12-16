namespace Example.macOS.WebKit
open System
open System.Threading
open AppKit
open Foundation
open Interstellar
open Interstellar.MacOS.WebKit

[<Register("AppDelegate")>]
type AppDelegate() =
    inherit NSApplicationDelegate()

    override this.ApplicationShouldTerminateAfterLastWindowClosed sender = false

    override this.DidFinishLaunching notification =
        printfn "didFinishLaunching"
        Thread.CurrentThread.Name <- "Main"

        let w = new Internal.BrowserWindow()
        //let w = Internal.BrowserWindow.CreateNew ()
        (w :> IBrowserWindow).Show () |> Async.StartImmediate

        ()