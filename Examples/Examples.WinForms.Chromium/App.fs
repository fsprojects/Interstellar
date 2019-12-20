namespace Example.Windows.Chromium.Wpf
open System
open System.Diagnostics
open System.Reflection
open System.Threading
open System.Windows
open System.Windows.Forms
open Interstellar
open Examples.SharedCode
open Interstellar.Chromium.WinForms
open System.Runtime.Versioning

module Main =
    let runApp () =
        Application.EnableVisualStyles ()
        Application.SetCompatibleTextRenderingDefault true
        BrowserApp.run SimpleBrowserApp.app
    
    [<EntryPoint; STAThread>]
    let main argv =
        Thread.CurrentThread.Name <- "Main"
        Trace.WriteLine (sprintf "Starting app. Main thread id: %A" Thread.CurrentThread.ManagedThreadId)
        Interstellar.Chromium.Platform.Initialize ()
        runApp ()
        Interstellar.Chromium.Platform.Shutdown ()
        0