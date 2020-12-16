// F# 4.7 due to: https://github.com/fsharp/FAKE/issues/2001
#r "paket:
nuget FSharp.Core 4.7
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Paket
nuget Fake.Tools.Git //"
#load "./.fake/build.fsx/intellisense.fsx"
#load "nupkg-hack.fsx"
// include Fake modules, see Fake modules section

#if !FAKE
    #r "netstandard"
    #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System
open System.Text.RegularExpressions
open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.Tools

module Projects =
    let coreLib = Path.Combine ("src", "Interstellar.Core", "Interstellar.Core.fsproj")
    let chromiumLib = Path.Combine ("src", "Interstellar.Chromium", "Interstellar.Chromium.fsproj")
    let winFormsLib = Path.Combine ("src", "Interstellar.WinForms.Chromium", "Interstellar.WinForms.Chromium.fsproj")
    let wpfLib = Path.Combine ("src", "Interstellar.Wpf.Chromium", "Interstellar.Wpf.Chromium.fsproj")
    let macosWkLib = Path.Combine ("src", "Interstellar.macOS.WebKit", "Interstellar.macOS.WebKit.fsproj")

module Solutions =
    let windows = "Interstellar.Windows.sln"
    let macos = "Interstellar.MacOS.sln"

let templatesNuspec = "templates/Interstellar.Templates.nuspec"

let projectRepo = "https://github.com/jwosty/Interstellar"

let projAsTarget (projFileName: string) = projFileName.Split('/').[0].Replace(".", "_")

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let addTargets targets (defaults: MSBuildParams) = { defaults with Targets = targets @ defaults.Targets }
let addTarget target (defaults: MSBuildParams) = { defaults with Targets = target :: defaults.Targets }

let quiet (defaults: MSBuildParams) = { defaults with Verbosity = Some MSBuildVerbosity.Quiet }

type PackageVersionInfo = { versionName: string; versionChanges: string }

let scrapeChangelog () =
    let changelog = System.IO.File.ReadAllText "CHANGELOG.md"
    let regex = Regex("""## (?<Version>.*)\n+(?<Changes>(.|\n)*?)##""")
    let result = seq {
        for m in regex.Matches changelog ->
            {   versionName = m.Groups.["Version"].Value.Trim()
                versionChanges =
                    m.Groups.["Changes"].Value.Trim()
                        .Replace("    *", "    ◦")
                        .Replace("*", "•")
                        .Replace("    ", "\u00A0\u00A0\u00A0\u00A0") }
    }
    result

let changelog = scrapeChangelog () |> Seq.toList
let currentVersionInfo = changelog.[0]

let addProperties props defaults = { defaults with Properties = [yield! defaults.Properties; yield! props]}

let addVersionInfo (versionInfo: PackageVersionInfo) =
    let versionPrefix, versionSuffix =
        match String.splitStr "-" versionInfo.versionName with
        | [hd] -> hd, None
        | hd::tl -> hd, Some (String.Join ("-", tl))
        | [] -> failwith "Version name is missing"
    addProperties [
        "VersionPrefix", versionPrefix
        match versionSuffix with Some versionSuffix -> "VersionSuffix", versionSuffix | _ -> ()
        "PackageReleaseNotes", versionInfo.versionChanges
    ]    

let projects = [
    yield Projects.coreLib
    if Environment.isWindows then yield! [Projects.chromiumLib; Projects.winFormsLib; Projects.wpfLib]
    if Environment.isMacOS then yield! [Projects.macosWkLib ]
]

let msbuild setParams project =
    let buildMode = Environment.environVarOrDefault "buildMode" "Release"
    project |> MSBuild.build (
        quiet <<
        setParams <<
        addProperties ["Configuration", buildMode] <<
        addVersionInfo currentVersionInfo << setParams
    )

// *** Define Targets ***
Target.create "PackageDescription" (fun _ ->
    let changelog = scrapeChangelog ()
    let currentVersion = Seq.head changelog
    let str = sprintf "Changes in package version %s\n%s" currentVersion.versionName currentVersion.versionChanges
    Trace.log str
)

let doRestore msbParams = { msbParams with DoRestore = true }

let getNupkgPath version projPath =
    let vstr = match version with Some v -> sprintf ".%s" v | None -> ""
    let projDir = Path.GetDirectoryName projPath
    Path.Combine ([|projDir; "bin"; "Release";
                    sprintf "%s%s.nupkg" (Path.GetFileNameWithoutExtension projPath) vstr|])

let getNupkgArtifactPath proj = Path.Combine ("artifacts", sprintf "%s.nupkg" (Path.GetFileNameWithoutExtension proj))

Target.create "Clean" (fun _ ->
    Trace.log " --- Cleaning --- "
    for proj in projects do
        try File.Delete (getNupkgPath (Some currentVersionInfo.versionName) proj) with _ -> ()
        try File.Delete (getNupkgArtifactPath proj) with _ -> ()
    if Environment.isWindows then
        msbuild (addTarget "Clean") Projects.winFormsLib
        msbuild (addTarget "Clean") Projects.wpfLib
    else
        msbuild (addTarget "Clean") Solutions.macos
    Shell.deleteDir ".fsdocs"
    Shell.deleteDir "output"
    Shell.deleteDir "temp"
)

Target.create "Restore" (fun _ ->
    DotNet.exec id "tool" "restore" |> ignore
    DotNet.restore id |> ignore
)

Target.create "Build" (fun _ ->
    Trace.log " --- Building --- "
    if Environment.isWindows then
        msbuild (addTarget "Restore") Solutions.windows
    else
        msbuild (addTarget "Restore") Solutions.macos
    if Environment.isWindows then
        msbuild (doRestore << addTarget "Build") Projects.winFormsLib
        msbuild (doRestore << addTarget "Build") Projects.wpfLib
    else if Environment.isMacOS then
        msbuild (doRestore << addTarget "Build") Projects.macosWkLib
)

Target.create "Test" (fun _ ->
    Trace.log " --- Running tests --- "
    // TODO: add some tests!
)

Target.create "BuildDocs" (fun _ ->
    Trace.log " --- Building documentation --- "
    let result = DotNet.exec id "fsdocs" ("build --clean --projects=" + Projects.coreLib + " --property Configuration=Release")
    Trace.logfn "%s" (result.ToString())
)

Target.create "ReleaseDocs" (fun _ ->
    Trace.log "--- Releasing documentation --- "
    Git.CommandHelper.runSimpleGitCommand "." (sprintf "clone %s temp/gh-pages --depth 1 -b gh-pages" projectRepo) |> ignore
    Shell.copyRecursive "output" "temp/gh-pages" true |> printfn "%A"
    Git.CommandHelper.runSimpleGitCommand "temp/gh-pages" "add ." |> printfn "%s"
    let commit = Git.Information.getCurrentHash ()
    Git.CommandHelper.runSimpleGitCommand "temp/gh-pages"
        (sprintf """commit -a -m "Update generated docs for version %s from %s" """ currentVersionInfo.versionName commit)
    |> printfn "%s"
    Git.Branches.pushBranch "temp/gh-pages" "origin" "gh-pages"
)

Target.create "Pack" (fun _ ->
    Trace.log " --- Packing NuGet packages --- "
    let props = ["SolutionDir", __SOURCE_DIRECTORY__; "RepositoryCommit", Git.Information.getCurrentSHA1 __SOURCE_DIRECTORY__]
    let msbuild f = msbuild (doRestore << addTargets ["Pack"] << addProperties props << f)
    Trace.log (sprintf "PROJECT LIST: %A" projects)
    for proj in projects do
        msbuild id proj
        // Strip version stuff from the file name, and collect all generated package archives into a common folder
        let oldNupkgPath = getNupkgPath (Some currentVersionInfo.versionName) proj
        Directory.CreateDirectory "artifacts" |> ignore
        let nupkgArtifact = getNupkgArtifactPath proj
        Trace.log (sprintf "Moving %s -> %s" oldNupkgPath nupkgArtifact)
        try File.Delete nupkgArtifact with _ -> ()
        File.Copy (oldNupkgPath, nupkgArtifact)
        ``Nupkg-hack``.hackNupkgAtPath nupkgArtifact // see #3
)

Target.create "PackTemplates" (fun _ ->
    Trace.log " --- Packing template packages --- "
    NuGet.NuGetPack
        (fun opt -> {
            opt with
                WorkingDir = Path.GetDirectoryName templatesNuspec
                Version = currentVersionInfo.versionName
        })
        templatesNuspec
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
    ==> "Restore"
    ==> "Build"
    ==> "Pack"

"Clean"
    ==> "PackTemplates"

"Build"
    ==> "BuildDocs"
    ==> "ReleaseDocs"

// *** Start Build ***
Target.runOrDefault "Build"