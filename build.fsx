#if FAKE
#load ".fake/build.fsx/intellisense.fsx"
#endif

// F# 4.7 due to https://github.com/fsharp/FAKE/issues/2001
#r "paket:
nuget FSharp.Core 4.7.0
nuget FSharp.Data
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Paket
nuget Fake.Tools.Git //"

//#r "nuget: FSharp.Data"

#if !FAKE
#r "netstandard"
// #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

#load "nupkg-hack.fsx"

open System
open System.Text.RegularExpressions
open System.IO
open FSharp.Data
open Fake.Core
open Fake.DotNet
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools

let srcDir = Path.Combine (__SOURCE_DIRECTORY__, "src")
let examplesDir = Path.Combine (__SOURCE_DIRECTORY__, "Examples")

module Projects =
    let coreLib = Path.Combine (srcDir, "Interstellar.Core", "Interstellar.Core.fsproj")
    let chromiumLib = Path.Combine (srcDir, "Interstellar.Chromium", "Interstellar.Chromium.fsproj")
    let winFormsLib = Path.Combine (srcDir, "Interstellar.WinForms.Chromium", "Interstellar.WinForms.Chromium.fsproj")
    let wpfLib = Path.Combine (srcDir, "Interstellar.Wpf.Chromium", "Interstellar.Wpf.Chromium.fsproj")
    let macosWkLib = Path.Combine (srcDir, "Interstellar.macOS.WebKit", "Interstellar.macOS.WebKit.fsproj")
    let wpfExampleApp = Path.Combine (examplesDir, "Examples.wpf.Chromium", "Examples.wpf.Chromium.fsproj")
    let macosExampleApp = Path.Combine (examplesDir, "Examples.macOS.WebKit", "Examples.macOS.WebKit.fsproj")

module Solutions =
    let windows = "Interstellar.Windows.sln"
    let macos = "Interstellar.MacOS.sln"

let artifactsPath = Path.Combine (__SOURCE_DIRECTORY__, "artifacts")

module Templates =
    let path = Path.Combine (__SOURCE_DIRECTORY__, "templates")

    let nuspecPaths = !! (Path.Combine (path, "*.nuspec"))
    let allProjects =
        !! (Path.Combine (path, "**/*.fsproj"))
    let winProjects =
        !! (Path.Combine (path, "**/*windows*.fsproj"))
    let macosProjects =
        !! (Path.Combine (path, "**/*macos*.fsproj"))

let [<Literal>] _srcDir =  __SOURCE_DIRECTORY__
type PackageInfo = XmlProvider<"AssemblyAndPackageInfo.props", ResolutionFolder=_srcDir>
let packageInfo = PackageInfo.Load(Path.Combine(__SOURCE_DIRECTORY__, "AssemblyAndPackageInfo.props"))
let packageProps = packageInfo.PropertyGroup

let projAsTarget (projFileName: string) = projFileName.Split('/').[0].Replace(".", "_")

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let addTargets targets (defaults: DotNet.BuildOptions) = { defaults with MSBuildParams = { defaults.MSBuildParams with Targets = targets @ defaults.MSBuildParams.Targets } }
let addTarget target (defaults: DotNet.BuildOptions) = { defaults with MSBuildParams = { defaults.MSBuildParams with Targets = target :: defaults.MSBuildParams.Targets } }

let quiet (defaults: DotNet.BuildOptions) = { defaults with MSBuildParams = { defaults.MSBuildParams with Verbosity = Some MSBuildVerbosity.Quiet } }

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
/// Indicates the extra version number that's added to the template package. When releasing a new version of Interstellar, reset this to 0. Whenever making a
/// change to just the template, increment this.
let currentTemplateMinorVersion = 1

let addProperties props (defaults: DotNet.BuildOptions) = { defaults with MSBuildParams = { defaults.MSBuildParams with Properties = [yield! defaults.MSBuildParams.Properties; yield! props]} }

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

let buildOptions setParams =
    let buildMode = Environment.environVarOrDefault "buildMode" "Release"
    let commit = Git.Information.getCurrentSHA1 __SOURCE_DIRECTORY__

    quiet <<
    setParams <<
    addProperties ["Configuration", buildMode; "RepositoryCommit", commit] <<
    addVersionInfo currentVersionInfo << setParams

let dotnetBuild (setParams: DotNet.BuildOptions -> DotNet.BuildOptions) project = project |> DotNet.build (buildOptions setParams)

// *** Define Targets ***
Target.create "PackageDescription" (fun _ ->
    let changelog = scrapeChangelog ()
    let currentVersion = Seq.head changelog
    let str = sprintf "Changes in package version %s\n%s" currentVersion.versionName currentVersion.versionChanges
    Trace.log str
)

let doRestore (dotnetBuildOptions: DotNet.BuildOptions) = { dotnetBuildOptions with MSBuildParams = { dotnetBuildOptions.MSBuildParams with DoRestore = true } }

let getNupkgPath version projPath =
    let vstr = match version with Some v -> sprintf ".%s" v | None -> ""
    let projDir = Path.GetDirectoryName projPath
    Path.Combine ([|projDir; "bin"; "Release";
                    sprintf "%s%s.nupkg" (Path.GetFileNameWithoutExtension projPath) vstr|])

Target.create "Clean" (fun _ ->
    Trace.log " --- Cleaning --- "
    for proj in projects do
        let vstr = currentVersionInfo.versionName
        File.delete (getNupkgPath (Some vstr) proj)
    !! (Path.Combine (artifactsPath, "**/*.nupkg")) |> File.deleteAll
    let projects =
        if Environment.isWindows then [ Projects.winFormsLib; Projects.wpfLib; yield! Templates.winProjects ]
        else if Environment.isMacOS then [ Solutions.macos; yield! Templates.macosProjects ]
        else []
    for proj in projects do
        dotnetBuild (addTarget "Clean") proj
    Shell.deleteDir ".fsdocs"
    Shell.deleteDir "output"
    Shell.deleteDir "temp"
)

Target.create "Restore" (fun _ ->
    DotNet.exec id "tool" "restore" |> ignore
    let proj = if Environment.isWindows then Solutions.windows else if Environment.isMacOS then Solutions.macos else failwithf "Platform not supported"
    DotNet.restore id proj |> ignore
)

Target.create "Build" (fun _ ->
    Trace.log " --- Building --- "
    if Environment.isWindows then
        dotnetBuild (addTarget "Restore") Solutions.windows
    else
        dotnetBuild (addTarget "Restore") Solutions.macos
    if Environment.isWindows then
        dotnetBuild (doRestore << addTarget "Build") Projects.winFormsLib
        dotnetBuild (doRestore << addTarget "Build") Projects.wpfLib
    else if Environment.isMacOS then
        dotnetBuild (doRestore << addTarget "Build") Projects.macosWkLib
)

Target.create "Run" (fun _ ->
    Trace.log " --- Running example app --- "
    if Environment.isWindows then
        DotNet.exec id "run" ("-p " + Projects.wpfExampleApp) |> ignore
    else
        Shell.cd (Path.GetDirectoryName Projects.macosExampleApp)
        dotnetBuild (addTarget "Run") Projects.macosExampleApp
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
    Git.CommandHelper.runSimpleGitCommand "." (sprintf "clone %s temp/gh-pages --depth 1 -b gh-pages" packageProps.RepositoryUrl) |> ignore
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
    let dotnetBuild f = dotnetBuild (doRestore << addTargets ["Pack"] << addProperties props << f)
    Trace.log (sprintf "PROJECT LIST: %A" projects)
    for proj in projects do
        dotnetBuild id proj
        // Collect all generated package archives into a common folder
        let vstr = currentVersionInfo.versionName
        let oldNupkgPath = getNupkgPath (Some vstr) proj
        Shell.mkdir artifactsPath
        Shell.moveFile artifactsPath oldNupkgPath
    // see https://github.com/fsprojects/Interstellar/issues/3
    !! (Path.Combine (artifactsPath, "**", "*.nupkg"))
    |> Seq.iter (``Nupkg-hack``.hackNupkgAtPath)
)

Target.create "BuildTemplateProjects" (fun _ ->
    Trace.log " --- Building template projects --- "
    if Environment.isWindows then
        let p = [ yield! Templates.winProjects ]
        for proj in p do
            DotNet.restore id proj
        for proj in p do
            DotNet.build id proj
    else if Environment.isMacOS then
        let p = [ yield! Templates.macosProjects ]
        for proj in p do
            dotnetBuild (addTarget "Restore") proj
        for proj in p do
            dotnetBuild (addTarget "Build") proj
)

Target.create "PackTemplates" (fun _ ->
    Trace.log " --- Packing template packages --- "
    Shell.mkdir artifactsPath
    for nuspecPath in Templates.nuspecPaths do
        NuGet.NuGetPack
            (fun opt -> {
                opt with
                    WorkingDir = Path.GetDirectoryName nuspecPath
                    OutputPath = artifactsPath
                    Version = sprintf "%s.%d" currentVersionInfo.versionName currentTemplateMinorVersion
            })
            nuspecPath
)

Target.create "PackAll" ignore

Target.create "TestAll" ignore

Target.create "All" ignore

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Restore"
    ==> "Build"
    ==> "Pack"
    ==> "PackAll"
    ==> "All"

"Restore"
    ==> "Run"

"PackTemplates"
    ==> "PackAll"
    ==> "All"

"Build"
    ==> "BuildDocs"
    ==> "ReleaseDocs"
    ==> "All"

"BuildTemplateProjects"
    ==> "TestAll"

// "Build"
    // ==> "Test"
"Test"
    ==> "TestAll"

"Build"
    ==> "BuildTemplateProjects"

// *** Start Build ***
Target.runOrDefault "Build"