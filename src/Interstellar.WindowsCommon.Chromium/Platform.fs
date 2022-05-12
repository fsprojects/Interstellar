namespace Interstellar.Chromium
open System
open System.IO
open System.Reflection
open CefSharp
#if WPF
open CefSharp.Wpf
#endif
#if WINFORMS
open CefSharp.WinForms
#endif

type Platform private() =
    static let mutable isInitialized = false
    static let initLock = new Object()
    
    static member Initialize () =
        lock initLock (fun () ->
            if not isInitialized then
                AppDomain.CurrentDomain.add_AssemblyResolve (ResolveEventHandler(Platform.ResolveCefSharpAssembly))
                Platform.InitAnyCpuCefSharp () |> ignore
        )

    // Without this inlining, WinForms builds crash in Release mode: https://github.com/fsprojects/Interstellar/issues/10
    [<System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)>]
    static member Shutdown () =
        Cef.Shutdown ()

    static member private GetPlatformAssemblyPath assemblyName =
        Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, (if Environment.Is64BitProcess then "x64" else "x86"), assemblyName)

    static member private InitAnyCpuCefSharp () =
        // Required to fix high DPI issue: https://github.com/fsprojects/Interstellar/issues/25
        Cef.EnableHighDPISupport()
        
        let browserSubpath = Platform.GetPlatformAssemblyPath("CefSharp.BrowserSubprocess.exe")
        let settings = new CefSettings(BrowserSubprocessPath = browserSubpath)
        //settings.RegisterExtension Browser.bridgeExtension
        Cef.Initialize (settings, false, (null : IBrowserProcessHandler))

    static member private ResolveCefSharpAssembly sender (args: ResolveEventArgs) =
        if (args.Name.StartsWith("CefSharp")) then
            let assemblyName = args.Name.Split([|','|], 2).[0] + ".dll"
            let archSpecificPath = Platform.GetPlatformAssemblyPath assemblyName
            if File.Exists archSpecificPath then (Assembly.LoadFile archSpecificPath) else null
        else null