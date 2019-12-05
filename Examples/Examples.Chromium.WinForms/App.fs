namespace Example.Windows.Chromium.Wpf
open System
open System.Diagnostics
open System.Reflection
open System.Windows
open System.Windows.Forms
open Interstellar.Core
open Interstellar.Chromium.WinForms
open System.Runtime.Versioning


module Main =
    let runApp () =
        Application.EnableVisualStyles ()
        Application.SetCompatibleTextRenderingDefault true
        Application.Run (new BrowserWindow(Address = "https://google.com/"))
    
    [<EntryPoint; STAThread>]
    let main argv =
        Trace.WriteLine (sprintf "Runtime framework: %s" (Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName))
        Interstellar.Chromium.Platform.Initialize ()
        runApp ()
        0