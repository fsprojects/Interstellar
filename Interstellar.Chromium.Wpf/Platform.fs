namespace Interstellar.Windows.Chromium.Wpf
open System
open System.IO
open System.Reflection
open CefSharp
open CefSharp.Wpf

type Platform private() =
    static let mutable isInitialized = false
    static let initLock = new Object()

    static member Initialize () =
        lock initLock (fun () ->
            if not isInitialized then
                AppDomain.CurrentDomain.add_AssemblyResolve (ResolveEventHandler(Platform.ResolveCefSharpAssembly))
                Platform.InitAnyCpuCefSharp () |> ignore
        )

    static member private GetPlatformAssemblyPath assemblyName =
        Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, (if Environment.Is64BitProcess then "x64" else "x86"), assemblyName)

    static member private InitAnyCpuCefSharp () =
        let settings = new CefSettings(BrowserSubprocessPath = Platform.GetPlatformAssemblyPath("CefSharp.BrowserSubprocess.exe"))
        Cef.Initialize (settings, false, (null : IBrowserProcessHandler))

    static member private ResolveCefSharpAssembly sender (args: ResolveEventArgs) =
        if (args.Name.StartsWith("CefSharp")) then
            let assemblyName = args.Name.Split([|','|], 2).[0] + ".dll"
            let archSpecificPath = Platform.GetPlatformAssemblyPath assemblyName
            if File.Exists archSpecificPath then (Assembly.LoadFile archSpecificPath) else null
        else null