namespace Example.Windows.Chromium.Wpf
open System
open System.Diagnostics
open System.Reflection
open System.Runtime.Versioning
open System.Threading
open System.Windows
open System.Windows.Controls
open Examples.SharedCode
open Interstellar
open Interstellar.Wpf.WebView2

type App() =
    inherit Application(ShutdownMode = ShutdownMode.OnExplicitShutdown)

    override this.OnStartup (e: StartupEventArgs) =
        base.OnStartup e

        let onMainWindowCreated (w: IBrowserWindow<Window>) =
            let nativeWindow = w.NativeWindow
            // This is where you could call some WPF-specific APIs on this window
            ()
        BrowserApp.run (SimpleBrowserApp.app onMainWindowCreated)
        Trace.WriteLine "returning from OnStartup"

module Main =
    [<EntryPoint; STAThread>]
    let main argv =
        Thread.CurrentThread.Name <- "Main"
        let app = new App()
        let result = app.Run ()
        Debug.WriteLine "main() exiting"
        result