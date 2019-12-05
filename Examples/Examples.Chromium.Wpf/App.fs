namespace Example.Windows.Chromium.Wpf
open System
open System.Diagnostics
open System.Reflection
open System.Windows
open System.Windows.Controls
open Interstellar.Core
open Interstellar.Chromium.Wpf
open System.Runtime.Versioning

type App() =
    inherit Application()
    override this.OnStartup(e: StartupEventArgs) =
        base.OnStartup e
        let window = new BrowserWindow("https://google.com/")
        window.Show ()

module Main =
    [<EntryPoint; STAThread>]
    let main argv =
        Trace.WriteLine (sprintf "Runtime framework: %s" (Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName))
        Interstellar.Chromium.Platform.Initialize ()
        let app = new App()
        app.Run ()