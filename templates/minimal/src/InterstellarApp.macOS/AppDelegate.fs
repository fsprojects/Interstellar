namespace InterstellarApp.macOS
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

        let mainCtx = SynchronizationContext.Current
        Async.Start <| async {
            let onMainWindowCreated (w: IBrowserWindow<NSWindow>) =
                let nsWindow = w.NativeWindow
                // This is where you could call some Cocoa-specific APIs on this window
                ()
            do! BrowserApp.runAsync mainCtx (SimpleBrowserApp.app ignore)
            NSApplication.SharedApplication.Terminate null
        }

        ()