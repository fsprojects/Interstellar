namespace Example.Windows.Chromium.Wpf
open System
open System.Diagnostics
open System.Reflection
open System.Runtime.Versioning
open System.Windows
open System.Windows.Controls
open Examples.SharedCode
open Interstellar
open Interstellar.Chromium.Wpf

type App() =
    inherit Application()
    override this.OnStartup(e: StartupEventArgs) =
        base.OnStartup e
        BrowserApp.run (SimpleBrowserApp.app)

module Main =
    [<EntryPoint; STAThread>]
    let main argv =
        Trace.WriteLine (sprintf "Runtime framework: %s" (Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName))
        Interstellar.Chromium.Platform.Initialize ()
        let app = new App()
        app.Run ()