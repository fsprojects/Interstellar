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
open Interstellar.Chromium.Wpf

type App() =
    inherit Application(ShutdownMode = ShutdownMode.OnExplicitShutdown)

    static member RuntimeFramework = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName

    override this.OnStartup (e: StartupEventArgs) =
        base.OnStartup e
        BrowserApp.run (SimpleBrowserApp.app "WPF" App.RuntimeFramework)
        Trace.WriteLine "returning from OnStartup"

module Main =
    [<EntryPoint; STAThread>]
    let main argv =
        Thread.CurrentThread.Name <- "Main"
        Trace.WriteLine (sprintf "Runtime framework: %s" App.RuntimeFramework)
        Interstellar.Chromium.Platform.Initialize ()
        let app = new App()
        let result = app.Run ()
        Debug.WriteLine "main() exiting"
        result