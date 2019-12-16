namespace Example.macOS.WebKit

open AppKit
open System

module main =
    [<EntryPoint>]
    let main args =
        printfn "entry point"
        NSApplication.Init()
        printfn "after init"
        NSApplication.SharedApplication.Delegate <- new AppDelegate()
        printfn "after shareappdelegate"
        NSApplication.Main(args)
        printfn "after main"
        0
