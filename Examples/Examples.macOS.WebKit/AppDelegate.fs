namespace Example.macOS.WebKit

open AppKit
open Foundation
open System

[<Register("AppDelegate")>]
type AppDelegate() =
    inherit NSApplicationDelegate()
