module build.Main

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
open build

let [<Literal>] pathSep =
    #if OS_WINDOWS
        "\\"
    #else
        "/"
    #endif
let [<Literal>] repoDir = __SOURCE_DIRECTORY__ + pathSep + ".." + pathSep
let [<Literal>] asmAndPackageInfoFilePath = "AssemblyAndPackageInfo.props"

let srcDir = Path.Combine (repoDir, "src")
let examplesDir = Path.Combine (repoDir, "Examples")

module Projects =
    let coreLib = Path.Combine (srcDir, "Interstellar.Core", "Interstellar.Core.fsproj")
    let chromiumLib = Path.Combine (srcDir, "Interstellar.Chromium", "Interstellar.Chromium.fsproj")
    let winFormsLib = Path.Combine (srcDir, "Interstellar.WinForms.Chromium", "Interstellar.WinForms.Chromium.fsproj")
    let wpfLib = Path.Combine (srcDir, "Interstellar.Wpf.Chromium", "Interstellar.Wpf.Chromium.fsproj")
    let macosWkLib = Path.Combine (srcDir, "Interstellar.macOS.WebKit", "Interstellar.macOS.WebKit.fsproj")
    let gtkSharpLib = Path.Combine (srcDir, "Interstellar.GtkSharp.WebKit", "Interstellar.GtkSharp.WebKit.fsproj")
    let winFormsExampleApp = Path.Combine (examplesDir, "Examples.winForms.Chromium", "Examples.WinForms.Chromium.fsproj")
    let wpfExampleApp = Path.Combine (examplesDir, "Examples.Wpf.Chromium", "Examples.Wpf.Chromium.fsproj")
    let macosExampleApp = Path.Combine (examplesDir, "Examples.macOS.WebKit", "Examples.macOS.WebKit.fsproj")
    let gtkSharpExampleApp = Path.Combine (examplesDir, "Examples.GtkSharp.WebKit", "Examples.GtkSharp.WebKit.fsproj")

module Solutions =
    let windows = Path.Combine (repoDir, "Interstellar.Windows.sln")
    let macos = Path.Combine (repoDir, "Interstellar.MacOS.sln")
    let linux = Path.Combine (repoDir, "Interstellar.Linux.sln")

let artifactsPath = Path.Combine (repoDir, "artifacts")

module Templates =
    let path = Path.Combine (repoDir, "templates")

    let nuspecPaths = !! (Path.Combine (path, "*.nuspec"))
    let allProjects =
        !! (Path.Combine (path, "**/*.fsproj"))
        |> Seq.map (fun p -> p)
    let winProjects =
        !! (Path.Combine (path, "**/*windows*.fsproj"))
        |> Seq.map (fun p -> p)
    let macosProjects =
        !! (Path.Combine (path, "**/*macos*.fsproj"))
        |> Seq.map (fun p -> p)
    let linuxProjects =
        !! (Path.Combine (path, "**/*GtkSharp*.fsproj"))
        |> Seq.map (fun p -> p)

type PackageInfo = XmlProvider<asmAndPackageInfoFilePath, ResolutionFolder=repoDir>
let packageInfo = lazy(PackageInfo.Load(Path.Combine(repoDir, asmAndPackageInfoFilePath)))
let packageProps = lazy(packageInfo.Force().PropertyGroup)

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
    let changelog = System.IO.File.ReadAllText (Path.Combine (repoDir, "CHANGELOG.md"))
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

let changelog = lazy (scrapeChangelog () |> Seq.toList)
let currentVersionInfo = lazy (changelog.Force()[0])
/// Indicates the extra version number that's added to the template package. When releasing a new version of Interstellar, reset this to 0. Whenever making a
/// change to just the template, increment this.
let currentTemplateMinorVersion = 1

let asmPkgInfo = lazy (System.IO.File.ReadAllText "AssemblyAndPackageInfo.props")

// Extract assembly info property value
let extractAsmPkgInfoProp propName =
    let r = Regex(sprintf "(<%s>)(?'value'.*)(</%s>)" propName propName)
    r.Match(asmPkgInfo.Force()).Groups.["value"].Value

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
    if Environment.isWindows then yield! [ Projects.chromiumLib; Projects.winFormsLib; Projects.wpfLib ]
    if Environment.isMacOS then yield! [ Projects.macosWkLib ]
    if Environment.isLinux then yield! [ Projects.gtkSharpLib ]
]

let msBuildCfg (args: TargetParameter) =
    args.Context.Arguments
    |> List.tryPick (fun x -> if x.ToLower () = "debug" then Some "Debug" else None)
    |> Option.defaultValue "Release"

let buildOptions args setParams =
    let buildMode = msBuildCfg args
    let commit = Git.Information.getCurrentSHA1 repoDir

    quiet <<
    setParams <<
    addProperties ["Configuration", buildMode; "RepositoryCommit", commit] <<
    addVersionInfo (currentVersionInfo.Force()) << setParams

let dotnetBuild args (setParams: DotNet.BuildOptions -> DotNet.BuildOptions) project = project |> DotNet.build (buildOptions args setParams)

// *** Define Targets ***
let PackageDescription _ =
    let changelog = scrapeChangelog ()
    let currentVersion = Seq.head changelog
    let str = sprintf "Changes in package version %s\n%s" currentVersion.versionName currentVersion.versionChanges
    Trace.log str

let doRestore (dotnetBuildOptions: DotNet.BuildOptions) = { dotnetBuildOptions with MSBuildParams = { dotnetBuildOptions.MSBuildParams with DoRestore = true } }

let getNupkgPath version (projPath: string) =
    let vstr = match version with Some v -> sprintf ".%s" v | None -> ""
    let projDir = Path.GetDirectoryName projPath
    Path.Combine [|projDir; "bin"; "Release";
                    sprintf "%s%s.nupkg" (Path.GetFileNameWithoutExtension projPath) vstr|]

let getOsName () =
    if Environment.isWindows then "Windows"
    elif Environment.isMacOS then "macOS"
    elif Environment.isLinux then "Linux"
    else Environment.OSVersion.Platform.ToString ()

let raisePlatNotSupported () = failwithf $"Platform not supported (%s{getOsName ()})"

let Clean args =
    Trace.log " --- Cleaning --- "
    for proj in projects do
        let vstr = currentVersionInfo.Force().versionName
        File.delete (getNupkgPath (Some vstr) proj)
    !! (Path.Combine (artifactsPath, "**/*.nupkg")) |> File.deleteAll
    let projects =
        if Environment.isWindows then [for p in [ Projects.winFormsLib; Projects.wpfLib; yield! Templates.winProjects ] -> p]
        elif Environment.isMacOS then [ yield Solutions.macos; for p in Templates.macosProjects -> p ]
        elif Environment.isLinux then [ yield Solutions.linux; for p in Templates.linuxProjects -> p ]
        else raisePlatNotSupported ()
    for proj in projects do
        dotnetBuild args (addTarget "Clean") proj
    Shell.deleteDir ".fsdocs"
    Shell.deleteDir "output"
    Shell.deleteDir "temp"

let Restore _ =
    DotNet.exec id "tool" "restore" |> ignore
    let proj =
        if Environment.isWindows then
            Solutions.windows
        else if Environment.isMacOS then
            Solutions.macos
        else if Environment.isLinux then
            Solutions.linux
        else
            raisePlatNotSupported ()
    DotNet.restore id proj

let Build args =
    Trace.log " --- Building --- "

    let restoreProj, buildProjs =
        if Environment.isWindows then
            Solutions.windows, [ Projects.winFormsLib; Projects.wpfLib; Projects.winFormsExampleApp; Projects.wpfExampleApp ]
        elif Environment.isMacOS then
            Solutions.macos, [ Projects.macosWkLib; Projects.macosExampleApp ]
        elif Environment.isLinux then
            Solutions.linux, [ Projects.gtkSharpLib; Projects.gtkSharpExampleApp ]
        else
            raisePlatNotSupported ()

    dotnetBuild args (addTarget "Restore") restoreProj
    for proj in buildProjs do
        dotnetBuild args (doRestore << addTarget "Build") proj

let Run args =
    Trace.log " --- Running example app --- "
    if Environment.isWindows then
        DotNet.exec id "run" ("-p " + Projects.wpfExampleApp) |> ignore
    elif Environment.isMacOS then
        Shell.cd (Path.GetDirectoryName Projects.macosExampleApp)
        dotnetBuild args (addTarget "Run") Projects.macosExampleApp
    elif Environment.isLinux then
        Shell.cd (Path.GetDirectoryName Projects.gtkSharpExampleApp)
        dotnetBuild args (addTarget "Run") Projects.gtkSharpExampleApp
    else
        raisePlatNotSupported ()

let Test _ =
    Trace.log " --- Running tests --- "
    // TODO: add some tests!

let BuildDocs _ =
    Trace.log " --- Building documentation --- "
    let result = DotNet.exec id "fsdocs" ("build --clean --projects=" + Projects.coreLib + " --property Configuration=Release")
    Trace.logfn "%s" (result.ToString())

let ReleaseDocs _ =
    Trace.log "--- Releasing documentation --- "
    Git.CommandHelper.runSimpleGitCommand "." (sprintf "clone %s temp/gh-pages --depth 1 -b gh-pages" (packageProps.Force().RepositoryUrl)) |> ignore
    Shell.copyRecursive "output" "temp/gh-pages" true |> printfn "%A"
    Git.CommandHelper.runSimpleGitCommand "temp/gh-pages" "add ." |> printfn "%s"
    let commit = Git.Information.getCurrentHash ()
    Git.CommandHelper.runSimpleGitCommand "temp/gh-pages"
        (sprintf """commit -a -m "Update generated docs for version %s from %s" """ (currentVersionInfo.Force().versionName) commit)
    |> printfn "%s"
    Git.Branches.pushBranch "temp/gh-pages" "origin" "gh-pages"

let Pack args =
    Trace.log " --- Packing NuGet packages --- "
    let props = ["SolutionDir", repoDir; "RepositoryCommit", Git.Information.getCurrentSHA1 repoDir]
    let dotnetBuild f = dotnetBuild args (doRestore << addTargets ["Pack"] << addProperties props << f)
    Trace.logf "PROJECT LIST: %A" projects
    for proj in projects do
        dotnetBuild id proj
        // Collect all generated package archives into a common folder
        let vstr = currentVersionInfo.Force().versionName
        let oldNupkgPath = getNupkgPath (Some vstr) proj
        Shell.mkdir artifactsPath
        Shell.moveFile artifactsPath oldNupkgPath
    // see https://github.com/fsprojects/Interstellar/issues/3
    !! (Path.Combine (artifactsPath, "**", "*.nupkg"))
    |> Seq.iter (NupkgHack.hackNupkgAtPath)

let BuildTemplateProjects args =
    Trace.log " --- Building template projects --- "
    if Environment.isWindows then
        let p = [ yield! Templates.winProjects ]
        for proj in p do
            DotNet.restore id proj
        for proj in p do
            DotNet.build id proj
    elif Environment.isMacOS then
        let p = [ yield! Templates.macosProjects ]
        for proj in p do
            dotnetBuild args (addTarget "Restore") proj
        for proj in p do
            dotnetBuild args (addTarget "Build") proj
    elif Environment.isLinux then
        let p = [ yield! Templates.linuxProjects ]
        for proj in p do
            dotnetBuild args (addTarget "Restore") proj
        for proj in p do
            dotnetBuild args (addTarget "Build") proj
    else
        raisePlatNotSupported ()

let PackTemplates _ =
    Trace.log " --- Packing template packages --- "
    Shell.mkdir artifactsPath
    for nuspecPath in Templates.nuspecPaths do
        NuGet.NuGetPack
            (fun opt -> {
                opt with
                    WorkingDir = Path.GetDirectoryName nuspecPath
                    OutputPath = artifactsPath
                    Version = sprintf "%s.%d" (currentVersionInfo.Force().versionName) currentTemplateMinorVersion
            })
            nuspecPath

let PackAll _ = ()

let TestAll _ = ()

let All _ = ()

open Fake.Core.TargetOperators

// FS0020: The result of this expression has type 'string' and is explicitly ignored. ...
#nowarn "0020"

let initTargets () =
    Target.create "PackageDescription" PackageDescription
    Target.create "Clean" Clean
    Target.create "Restore" Restore
    Target.create "Build" Build
    Target.create "Run" Run
    Target.create "Test" Test
    Target.create "BuildDocs" BuildDocs
    Target.create "ReleaseDocs" ReleaseDocs
    Target.create "Pack" Pack
    Target.create "BuildTemplateProjects" BuildTemplateProjects
    Target.create "PackTemplates" PackTemplates
    Target.create "PackAll" PackAll
    Target.create "TestAll" TestAll
    Target.create "All" All

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
        ==> "Run"

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

[<EntryPoint>]
let main args =
    Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

    args
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fs"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    initTargets ()
    Target.runOrDefaultWithArguments "Build"

    0
