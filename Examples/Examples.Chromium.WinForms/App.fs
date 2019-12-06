namespace Example.Windows.Chromium.Wpf
open System
open System.Diagnostics
open System.Reflection
open System.Windows
open System.Windows.Forms
open Interstellar
open Examples.SharedCode
open Interstellar.Chromium.WinForms
open System.Runtime.Versioning

module Main =
    let runtimeFramework = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName

    let runApp () =
        Application.EnableVisualStyles ()
        Application.SetCompatibleTextRenderingDefault true
        BrowserApp.run (SimpleBrowserApp.app "WinForms" runtimeFramework)
        Application.Run ()
    
    [<EntryPoint; STAThread>]
    let main argv =
        Trace.WriteLine (sprintf "Runtime framework: %s" runtimeFramework)
        Interstellar.Chromium.Platform.Initialize ()
        runApp ()
        0