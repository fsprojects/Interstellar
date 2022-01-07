namespace Example.Windows.Chromium.Wpf
open System
open System.Diagnostics
open System.Reflection
open System.Runtime.Versioning
open System.Threading
open Examples.SharedCode
open Interstellar
open Webview
//open Interstellar.Webview

//type App() =
//    inherit Application(ShutdownMode = ShutdownMode.OnExplicitShutdown)

//    override this.OnStartup (e: StartupEventArgs) =
//        base.OnStartup e
//        let onMainWindowCreated (w: IBrowserWindow<Window>) =
//            let nativeWindow = w.NativeWindow
//            // This is where you could call some WPF-specific APIs on this window
//            ()
//        BrowserApp.run (SimpleBrowserApp.app onMainWindowCreated)
//        Trace.WriteLine "returning from OnStartup"

module Main =
    [<EntryPoint; STAThread>]
    let main argv =
        Thread.CurrentThread.Name <- "Main"

        //let app = new App()
        //let result = app.Run ()
        
        let wvb = WebviewBuilder(Uri "https://github.com/fsprojects/Interstellar")
        let wv = wvb.Build()
        wv.Run ()

        Debug.WriteLine "main() exiting"
        0