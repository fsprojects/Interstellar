#I "..\\.paket\\load\\netstandard2.0"
#load "BlackFox.MasterOfFoo.fsx"
#load "System.Text.Encodings.Web.fsx"
#load "main.group.fsx"
#load "Api.fs"

open System.Text.Encodings.Web
open FSharp.Core.Printf
open BlackFox.MasterOfFoo
open Interstellar


Interstellar.Printf.javascriptf "console.log(\"%s\")" """'hello \"world" """ |> printfn "%s"
Interstellar.Printf.javascriptf "console.log('%s')" """'hello "world""" |> printfn "%s"
Interstellar.Printf.javascriptf "console.log(\"%s\")" """ \"hello world" """ |> printfn "%s"
let browser = Unchecked.defaultof<IBrowser>
let foo = executeJavascriptf browser "%d%b" 42 true

let writeLine (browser: IBrowser) format =
    //kprintf (fun x -> browser.ExecuteJavascript x) format
    let result = sprintf format
    result
let result = writeLine browser "%d%b" 42 true