#if FAKE
#load ".fake/build.fsx/intellisense.fsx"
#endif

// F# 4.7 due to https://github.com/fsharp/FAKE/issues/2001
#r "paket:
nuget FSharp.Core 4.7.0
nuget Fake.Core.Target
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.Paket
nuget Fake.Tools.Git //"

#if !FAKE
#r "netstandard"
// #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

#load "nupkg-hack.fsx"

open System
open System.Text.RegularExpressions
open System.IO
open Fake.Core
open Fake.DotNet
open Fake.DotNet.NuGet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools

type ProjectStyle = Sdk | Traditional

module Projects =
    let coreLib = Path.Combine ("src", "Interstellar.Core", "Interstellar.Core.fsproj"), ProjectStyle.Sdk
    let chromiumLib = Path.Combine ("src", "Interstellar.Chromium", "Interstellar.Chromium.fsproj"), ProjectStyle.Sdk
    let winFormsLib = Path.Combine ("src", "Interstellar.WinForms.Chromium", "Interstellar.WinForms.Chromium.fsproj"), ProjectStyle.Sdk
    let wpfLib = Path.Combine ("src", "Interstellar.Wpf.Chromium", "Interstellar.Wpf.Chromium.fsproj"), ProjectStyle.Sdk
    let macosWkLib = Path.Combine ("src", "Interstellar.MacOS.WebKit", "Interstellar.MacOS.WebKit.fsproj"), ProjectStyle.Sdk
    let macosWkFFLib = Path.Combine ("src", "Interstellar.MacOS.WebKit.FullFramework", "Interstellar.MacOS.WebKit.FullFramework.fsproj"), ProjectStyle.Traditional

module Solutions =
    let windows = "Interstellar.Windows.sln"
    let macos = "Interstellar.MacOS.sln"

let artifactsPath = "artifacts"

module Templates =
    let path = "templates"

    let nuspecPaths = !! (Path.Combine (path, "*.nuspec"))
    let allProjects =
        !! (Path.Combine (path, "**/*.fsproj"))
        |> Seq.map (fun p -> p, ProjectStyle.Sdk)
    let winProjects =
        !! (Path.Combine (path, "**/*windows*.fsproj"))
        |> Seq.map (fun p -> p, ProjectStyle.Sdk)
    let macosProjects =
        !! (Path.Combine (path, "**/*macos*.fsproj"))
        |> Seq.map (fun p -> p, ProjectStyle.Sdk)

let projectRepo = "https://github.com/fsprojects/Interstellar"

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
/// Indicates the extra version number that's added to the template package. When releasing a new version of Interstellar, reset this to 0. Whenever making a
/// change to just the template, increment this.
let currentTemplateMinorVersion = 1

let asmPkgInfo = System.IO.File.ReadAllText "AssemblyAndPackageInfo.props"

// Extract assembly info property value
let extractAsmPkgInfoProp propName =
    let r = new Regex(sprintf "(<%s>)(?'value'.*)(</%s>)" propName propName)
    r.Match(asmPkgInfo).Groups.["value"].Value

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
    if Environment.isMacOS then yield! [Projects.macosWkLib; Projects.macosWkFFLib]
]

let msbuild setParams project =
    let buildMode = Environment.environVarOrDefault "buildMode" "Release"
    let commit = Git.Information.getCurrentSHA1 __SOURCE_DIRECTORY__
    project |> MSBuild.build (
        quiet <<
        setParams <<
        addProperties ["Configuration", buildMode; "RepositoryCommit", commit] <<
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

Target.create "Clean" (fun _ ->
    Trace.log " --- Cleaning --- "
    for (proj, projStyle) in projects do
        let vstr = currentVersionInfo.versionName
        File.delete (getNupkgPath (Some vstr) proj)
    !! (Path.Combine (artifactsPath, "**/*.nupkg")) |> File.deleteAll
    let projects =
        if Environment.isWindows then [for (p,_) in [ Projects.winFormsLib; Projects.wpfLib; yield! Templates.winProjects ] -> p]
        else if Environment.isMacOS then [ yield Solutions.macos; for (p,_) in Templates.macosProjects -> p ]
        else []
    for proj in projects do
        msbuild (addTarget "Clean") proj
    Shell.deleteDir ".fsdocs"
    Shell.deleteDir "output"
    Shell.deleteDir "temp"
)

Target.create "Restore" (fun _ ->
    DotNet.exec id "tool" "restore" |> ignore
    // DotNet.restore id |> ignore
    if Environment.isWindows then
        DotNet.restore id Solutions.windows
    else if Environment.isMacOS then
        DotNet.restore id Solutions.macos
)

// Syncs AssemblyInfo.fs with AssemblyAndPackageInfo.props
Target.create "UpdateAssemblyInfo" (fun _ ->
    Trace.log " --- Updating AssemblyInfo.fs in Interstellar.MacOS.WebKit.FullFramework --- "

    let asmInfoPath = Path.Combine (Path.GetDirectoryName (fst Projects.macosWkFFLib), "AssemblyInfo.fs")
    let projNameWithoutExt = Path.GetFileNameWithoutExtension (fst Projects.macosWkFFLib)
    let asmInfo = File.ReadAllText asmInfoPath

    let replacements =
        Map.ofList [
            "AssemblyDescription", extractAsmPkgInfoProp "Description"
            "AssemblyCopyright", extractAsmPkgInfoProp "Copyright"
            "AssemblyCompany", extractAsmPkgInfoProp "Company"
            "AssemblyTitle", projNameWithoutExt
            "AssemblyProduct", projNameWithoutExt
            // Since the fsproj imports AssemblyAndPackageInfo.props and properly ingests this property, we do not need to include it here
            //"AssemblyVersion", currentVersionInfo.versionName + ".0
            "AssemblyInformationalVersion", currentVersionInfo.versionName
            "AssemblyFileVersion", currentVersionInfo.versionName + ".0"
        ]

    let result =
        Regex.Replace (
            asmInfo,
            // matches the following:
            // [<assembly: ${AttributeName}("${AttributeValue}")>]
            // where ${AttributeName} and ${AttributeValue} are named match groups that could be anything
            """(\[<assembly: )(?<AttributeName>.*)(\(")(?<OldValue>.*)("\)>\])""",
            MatchEvaluator(fun m ->
                match Map.tryFind (m.Groups.["AttributeName"].Value) replacements with
                | Some newValue ->
                    //printfn "VALUES"
                    //for x in m.Groups do printfn "%s -> %s" x.Name x.Value
                    m.Groups.[1].Value                    // [<assembly: 
                    + m.Groups.["AttributeName"].Value    // ${AttributeName}
                    + m.Groups.[2].Value                  // "
                    + newValue // the actual replacement
                    + m.Groups.[3].Value                  // ")
                | None -> m.ToString()
            )
        )

    //Trace.log result

    File.WriteAllText (asmInfoPath, result)
)

Target.create "Build" (fun _ ->
    Trace.log " --- Building --- "
    // if Environment.isWindows then
    //     msbuild (addTarget "Restore") Solutions.windows
    // else
    //     msbuild (addTarget "Restore") Solutions.macos
    if Environment.isWindows then
        msbuild (addTarget "Build") (fst Projects.winFormsLib)
        msbuild (addTarget "Build") (fst Projects.wpfLib)
    else if Environment.isMacOS then
        // this is so very strange that we have to treat them differently like so...
        // macosWkLib needs the `msbuild /restore` for whatever reason because it's an SDK-style project...
        msbuild (doRestore) (fst Projects.macosWkLib)
        // but this one needs the `dotnet restore *.sln` somehow because it's not an SDK-style project!
        msbuild id (fst Projects.macosWkFFLib)
        // this makes zero sense, but alright... seriously, what the heck? You try changing those around and see msbuild/dotnet scream
        // at you
)

Target.create "Test" (fun _ ->
    Trace.log " --- Running tests --- "
    // TODO: add some tests!
)

Target.create "BuildDocs" (fun _ ->
    Trace.log " --- Building documentation --- "
    let result = DotNet.exec id "fsdocs" ("build --clean --projects=" + (fst Projects.coreLib) + " --property Configuration=Release")
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
    let props = ["SolutionDir", __SOURCE_DIRECTORY__]
    Trace.log (sprintf "PROJECT LIST: %A" projects)
    for (proj, projStyle) in projects do
        match projStyle with
        | ProjectStyle.Sdk ->
            Trace.log (sprintf "Packing %s (sdk-style project)" proj)
            msbuild (addTargets ["Pack"] << addProperties props) proj
            // Collect all generated package archives into a common folder
            let vstr = currentVersionInfo.versionName
            let oldNupkgPath = getNupkgPath (Some vstr) proj
            Shell.mkdir artifactsPath
            Shell.moveFile artifactsPath oldNupkgPath
        | ProjectStyle.Traditional ->
            // `dotnet pack` and `msbuild pack` only work with sdk-style projects
            Trace.logfn "Packing %s (traditional-style project)" proj
            Trace.logfn "Authors = %A" ([for a in (extractAsmPkgInfoProp "Authors").Split(';') -> a.Trim()])
            NuGet.NuGetPack
                (fun opt -> {
                    opt with
                        WorkingDir = Path.GetDirectoryName proj
                        OutputPath = artifactsPath
                        Properties = ["Configuration", "Release"]
                        Version = currentVersionInfo.versionName
                        Authors = [for a in (extractAsmPkgInfoProp "Authors").Split(';') -> a.Trim()]
                        Copyright = extractAsmPkgInfoProp "Copyright"
                        ReleaseNotes = currentVersionInfo.versionChanges
                        Tags = String.Join (" ", (extractAsmPkgInfoProp "Tags").Split(';') |> Seq.map (fun a -> a.Trim()))
                })
                proj
    // see https://github.com/fsprojects/Interstellar/issues/3
    !! (Path.Combine (artifactsPath, "**", "*.nupkg"))
    |> Seq.iter (``Nupkg-hack``.hackNupkgAtPath)
)

Target.create "BuildTemplateProjects" (fun _ ->
    Trace.log " --- Building template projects --- "
    if Environment.isWindows then
        let p = [ yield! Templates.winProjects ]
        for (proj, projStyle) in p do
            DotNet.restore id proj
        for (proj, projStyle) in p do
            DotNet.build id proj
    else if Environment.isMacOS then
        let p = [ yield! Templates.macosProjects ]
        for (proj, projStyle) in p do
            msbuild (addTarget "Restore") proj
            // DotNet.restore id proj
        for (proj, projStyle) in p do
            msbuild (addTarget "Build") proj
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
    ==> "UpdateAssemblyInfo"
    ==> "Build"
    ==> "Pack"
    ==> "PackAll"
    ==> "All"

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