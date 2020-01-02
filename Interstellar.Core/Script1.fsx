#I "..\\.paket\\load\\netstandard2.0"
#load "BlackFox.MasterOfFoo.fsx"
#load "System.Text.Encodings.Web.fsx"
#load "main.group.fsx"
#load "Api.fs"

open System.Text.Encodings.Web
open FSharp.Core.Printf
open BlackFox.MasterOfFoo
open Interstellar

type MockBrowser() =
    interface IBrowser with
        member this.Address = raise (System.NotImplementedException())
        member this.AreDevToolsShowing = raise (System.NotImplementedException())
        member this.CanGoBack = raise (System.NotImplementedException())
        member this.CanGoForward = raise (System.NotImplementedException())
        member this.CloseDevTools() = raise (System.NotImplementedException())
        member this.Engine = raise (System.NotImplementedException())
        member this.GoBack() = raise (System.NotImplementedException())
        member this.GoForward() = raise (System.NotImplementedException())
        [<CLIEvent>]
        member this.JavascriptMessageRecieved = Unchecked.defaultof<IEvent<_>>
        member this.Load(arg1) = raise (System.NotImplementedException())
        member this.LoadAsync(arg1) = raise (System.NotImplementedException())
        member this.LoadString(html, uri) = raise (System.NotImplementedException())
        member this.LoadStringAsync(html, uri) = raise (System.NotImplementedException())
        [<CLIEvent>]
        member this.PageLoaded = Unchecked.defaultof<IEvent<_>>
        member this.PageTitle = raise (System.NotImplementedException())
        [<CLIEvent>]
        member this.PageTitleChanged = Unchecked.defaultof<IEvent<_>>
        member this.Reload() = raise (System.NotImplementedException())
        member this.ShowDevTools() = raise (System.NotImplementedException())
        member this.ExecuteJavascript script = printfn "%s" script

let mockBrowser = new MockBrowser() :> IBrowser

Interstellar.Printf.javascriptf "console.log(\"%s\")" """'hello \"world" """ |> printfn "%s"
Interstellar.Printf.javascriptf "console.log('%s')" """'hello "world""" |> printfn "%s"
Interstellar.Printf.javascriptf "console.log(\"%s\")" """ \"hello world" """ |> printfn "%s"
do executeJavascriptf mockBrowser "%d%b" 42 true

let executeJavascriptf' (browser: IBrowser) format = kjavascriptf (fun result -> browser.ExecuteJavascript result) format
do executeJavascriptf' mockBrowser "console.log(\"%s\")" """ \"hello world"<%*>; """