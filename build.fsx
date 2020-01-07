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

open Fake.Core
open Fake.DotNet

module Projects =
    let winFormsLib = "Interstellar.WinForms.Chromium/Interstellar.WinForms.Chromium.fsproj"
    let wpfLib = "Interstellar.Wpf.Chromium/Interstellar.Wpf.Chromium.fsproj"

module Solutions =
    let windows = "Interstellar.Windows.sln"
    let macos = "Interstellar.MacOS.sln"

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let addTargets targets (defaults: MSBuildParams) = { defaults with Targets = defaults.Targets @ targets }
let addTarget target (defaults: MSBuildParams) = { defaults with Targets = defaults.Targets @ [target] }

let quiet (defaults: MSBuildParams) = { defaults with Verbosity = Some MSBuildVerbosity.Quiet }

let msbuild setTargets project = MSBuild.build (quiet << setTargets) project

// *** Define Targets ***
Target.create "Clean" (fun _ ->
    if Environment.isWindows then
        msbuild (addTarget "Clean") Solutions.windows
    else
        msbuild (addTarget "Clean") Solutions.macos
)

Target.create "Build" (fun _ ->
    Trace.log " --- Building the app --- "
    let buildMode = Environment.environVarOrDefault "buildMode" "Release"
    if Environment.isWindows then
        msbuild (addTarget "Restore") Solutions.windows
    else
        msbuild (addTarget "Restore") Solutions.macos   
    msbuild (addTarget "Build") Projects.winFormsLib
    msbuild (addTarget "Build") Projects.wpfLib
)

open Fake.Core.TargetOperators

// *** Define Dependencies ***
"Clean"
  ==> "Build"

// *** Start Build ***
Target.runOrDefault "Deploy"