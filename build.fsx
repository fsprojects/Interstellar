#r "paket:
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Paket //"
#load "./.fake/build.fsx/intellisense.fsx"
// include Fake modules, see Fake modules section

#if !FAKE
    #r "netstandard"
    #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System.Text.RegularExpressions
open Fake.Core
open Fake.DotNet

module Projects =
    let coreLib = "Interstellar.Core"
    let chromiumLib = "Interstellar.Chromium"
    let winFormsLib = "Interstellar.WinForms.Chromium/Interstellar.WinForms.Chromium.fsproj"
    let wpfLib = "Interstellar.Wpf.Chromium/Interstellar.Wpf.Chromium.fsproj"
    let macosWkLib = "Interstellar.macOS.WebKit/Interstellar.macOS.WebKit.fsproj"

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
    let changelog = Fake.IO.File.readAsString "CHANGELOG.md"
    let regex = Regex("""## (?<Version>.*)\n+(?<Changes>(.|\n)*?)##""")
    let result = seq {
        for m in regex.Matches changelog ->
            {   versionName = m.Groups.["Version"].Value
                versionChanges =
                    m.Groups.["Changes"].Value.Trim()
                        .Replace("    *", "    ◦")
                        .Replace("*", "•")
                        .Replace("    ", "\u00A0\u00A0\u00A0\u00A0") }
    }
    result

let changelog = scrapeChangelog () |> Seq.toList

let addVersionInfo (versionInfo: PackageVersionInfo) (defaults: MSBuildParams) =
    { defaults with
        Properties = defaults.Properties @ ["Version", versionInfo.versionName
                                            "PackageReleaseNotes", versionInfo.versionChanges] }

let addProperties props defaults = { defaults with Properties = [yield! defaults.Properties; yield! props]}

let msbuild setParams project =
    let versionInfo = changelog |> List.head
    let buildMode = Environment.environVarOrDefault "buildMode" "Release"
    project |> MSBuild.build (
        quiet <<
        setParams <<
        addProperties ["Configuration", buildMode] <<
        addVersionInfo versionInfo << setParams
    )
    

// *** Define Targets ***
Target.create "PackageDescription" (fun _ ->
    let changelog = scrapeChangelog ()
    let currentVersion = Seq.head changelog
    let str = sprintf "Changes in package version %s\n%s" currentVersion.versionName currentVersion.versionChanges
    Trace.log str
)

Target.create "Clean" (fun _ ->
    Trace.log " --- Cleaning --- "
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
        msbuild (addTarget "Build") Projects.winFormsLib
        msbuild (addTarget "Build") Projects.wpfLib
    else if Environment.isMacOS then
        msbuild (addTarget "Restore;Build") Projects.macosWkLib    
)

Target.create "Pack" (fun _ ->
    Trace.log " --- Packing NuGet packages --- "
    let msbuild f = msbuild (addTargets ["Restore"; "Pack"] << addProperties ["SolutionDir", __SOURCE_DIRECTORY__] << f)
    msbuild id Projects.coreLib
    msbuild id Projects.chromiumLib
    if Environment.isWindows then
        msbuild id Projects.winFormsLib
        msbuild id Projects.wpfLib
    else if Environment.isMacOS then
        msbuild id Projects.macosWkLib    
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
    ==> "Build"
    ==> "Pack"

// *** Start Build ***
Target.runOrDefault "Build"