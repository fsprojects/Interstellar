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