namespace InterstellarApp.macOS

open AppKit
open System

module main =
    [<EntryPoint>]
    let main args =
        NSApplication.Init()
        NSApplication.SharedApplication.Delegate <- new AppDelegate()
        NSApplication.Main(args)
        0
