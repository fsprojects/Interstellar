namespace Interstellar
open System
open System.Runtime.CompilerServices

[<Extension>]
type IBrowserExtensions =
    [<Extension>]
    static member Load (this: IBrowser, uri: string) = this.Load (new Uri(uri))
    [<Extension>]
    static member LoadString (this: IBrowser, html: string, ?uri: string) =
        this.LoadString (html, ?uri = Option.map Uri uri)

[<AutoOpen>]
module FSharpIBrowserExtensions =
    open FSharp.Core.Printf

    type IBrowser with
        /// <summary>printf-style method that executes some Javascript code on a browser instance, sanitizing format parameters using <see cref="javascriptf"/>. It is safe to pass in untrusted format parameters from the outside world. Think SQL prepared statements.</summary>
        member this.ExecuteJavascriptf (format: StringFormat<'a, unit>) =
            executeJavascriptf this format