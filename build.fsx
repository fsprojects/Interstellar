#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Paket //"
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

module Projects =
    let coreLib = Path.Combine ("Interstellar.Core", "Interstellar.Core.fsproj")
    let chromiumLib = Path.Combine ("Interstellar.Chromium", "Interstellar.Chromium.fsproj")
    let winFormsLib = Path.Combine ("Interstellar.WinForms.Chromium", "Interstellar.WinForms.Chromium.fsproj")
    let wpfLib = Path.Combine ("Interstellar.Wpf.Chromium", "Interstellar.Wpf.Chromium.fsproj")
    let macosWkLib = Path.Combine ("Interstellar.macOS.WebKit", "Interstellar.macOS.WebKit.fsproj")

module Solutions =
    let windows = "Interstellar.Windows.sln"
    let macos = "Interstellar.MacOS.sln"

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

let addVersionInfo (versionInfo: PackageVersionInfo) (defaults: MSBuildParams) =
    let versionPrefix, versionSuffix =
        match String.splitStr "-" versionInfo.versionName with
        | [hd] -> hd, None
        | hd::tl -> hd, Some (String.Join ("-", tl))
        | [] -> failwith "Version name is missing"
    let props =
        ["VersionPrefix", versionPrefix
         match versionSuffix with Some versionSuffix -> "VersionSuffix", versionSuffix | _ -> ()
         "PackageReleaseNotes", versionInfo.versionChanges]    
    { defaults with Properties = defaults.Properties @ props }

let addProperties props defaults = { defaults with Properties = [yield! defaults.Properties; yield! props]}

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

Target.create "Pack" (fun _ ->
    Trace.log " --- Packing NuGet packages --- "
    let msbuild f = msbuild (doRestore << addTargets ["Pack"] << addProperties ["SolutionDir", __SOURCE_DIRECTORY__] << f)
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
        ``Nupkg-hack``.hackNupkgAtPath nupkgArtifact

)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
    ==> "Build"
    ==> "Pack"

// *** Start Build ***
Target.runOrDefault "Build"