#r "System.IO.Compression"
open System
open System.IO
open System.IO.Compression
open System.Text
open System.Text.RegularExpressions

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let changeVersionConstraints text =
    Regex("(?<=id=\"Interstellar.+?\"\\s+version=\")[^[\\]]*?(?=\")")
        .Replace (text, MatchEvaluator(fun m -> sprintf "[%s]" m.Value))

// """<group targetFramework=".NETFramework4.7.2">
//     <dependency id="Interstellar.Chromium" version="0.2.0-rc" exclude="Build,Analyzers" />
//     <dependency id="Interstellar.Core" version="0.2.0-rc" exclude="Build,Analyzers" />
//     <dependency id="CefSharp.Wpf" version="75.1.143" exclude="Build,Analyzers" />
//     <dependency id="FSharp.Core" version="4.2.3" exclude="Build,Analyzers" />
// </group>
// <group targetFramework=".NETCoreApp3.0">
//     <dependency id="Interstellar.Chromium" version="0.2.0-rc" exclude="Build,Analyzers" />
//     <dependency id="Interstellar.Core" version="0.2.0-rc" exclude="Build,Analyzers" />
//     <dependency id="CefSharp.Wpf" version="75.1.143" exclude="Build,Analyzers" />
//     <dependency id="FSharp.Core" version="4.2.3" exclude="Build,Analyzers" />
// </group>"""
// |> changeVersionConstraints |> printfn "%s"

let hackNupkgFromStream path (stream: Stream) =
    use archive = new ZipArchive(stream, ZipArchiveMode.Update)
    let nuspecName = sprintf "%s.nuspec" (Path.GetFileNameWithoutExtension path)
    let oldEntry = archive.GetEntry nuspecName
    let input =
        (use nuspecReader = new StreamReader(oldEntry.Open(), Encoding.UTF8) in nuspecReader.ReadToEnd())
    let output = changeVersionConstraints input
    use nuspecWriter = new StreamWriter(oldEntry.Open(), Encoding.UTF8)
    nuspecWriter.Write output

/// Cracks open a nupkg and changes all Interstellar package reference constraints from >= to =
let hackNupkgAtPath path =
    use file = File.Open (path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
    hackNupkgFromStream path file

//hackNupkgAtPath (Path.Combine ("artifacts", "Interstellar.Wpf.Chromium.nupkg"))