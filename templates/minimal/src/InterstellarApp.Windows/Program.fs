namespace InterstellarApp.Windows
open System
open System.Diagnostics
open System.Threading
open System.Windows
open Interstellar
open Interstellar.Core
open Interstellar.Chromium.Wpf

type App() =
    inherit Application(ShutdownMode = ShutdownMode.OnExplicitShutdown)

    override this.OnStartup (e: StartupEventArgs) =
        base.OnStartup e
        let onMainWindowCreated (w: IBrowserWindow<Window>) =
            let nativeWindow = w.NativeWindow
            // This is where you could call some WPF-specific APIs on this window
            ()
        BrowserApp.run (InterstellarApp.BrowserApp.app onMainWindowCreated)

module Main =
    [<EntryPoint; STAThread>]
    let main argv =
        Thread.CurrentThread.Name <- "Main"
        Interstellar.Chromium.Platform.Initialize ()
        let app = App()
        let result = app.Run ()
        Debug.WriteLine "main() exiting"
        result