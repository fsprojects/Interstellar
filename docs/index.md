# Interstellar

> NOTE: This API is not yet guarenteed to be stable or backward-compatible until v1.0, so breaking changes may occur at any time.

Interstellar is an F# library providing a standard, mixed-paradigm API for accessing browser controls on various platforms. Currently, there are 3 combinations platform and browser hosts available. See [Examples](https://github.com/jwosty/Interstellar/Examples) for a simple sample application. See https://github.com/jwosty/InterstellarFableHelloWorld for an example of combining Interstellar with [Fable](https://fable.io/), achieving a cross-platform desktop app built completely in F#.

## Quick Start

You will need the .Net 5 SDK (and the mono SDK on macOS). For the Windows projects, you should be able to use any of the standard IDEs (Visual Studio, Visual Studio Code, Rider \[untested but should work\]). For the macOS projects, you need to use Visual Studio for Mac.


Create a project from the template:

```bash
dotnet new -i Interstellar.Template
dotnet new interstellar -n <project-name>
```

On Windows, you can run it like so:

```bash
dotnet restore <project-name>.Windows.sln
dotnet run -p <project-name>.Windows\<project-name>.Windows.fsproj
```

On macOS, I recommend opening ``<project-name>.macOS.sln`` in Visual Studio for Mac and running it that way. It runs using the mono-based Xamarin.macOS runtime, and as a result you currently can't run it with ``dotnet run`` (see [xamarin/xamarin-macios#3955](https://github.com/xamarin/xamarin-macios/issues/8955)). This is on the roadmap for .NET 6. You'll have to use Mono's ``msbuild`` instead, if you want to use the CLI.

You should end up with a simple, cross-platform sample app that opens a window built using embeded HTML, CSS, and Javascript.

![Sample Interstellar app](https://jwosty.github.io/Interstellar/Interstellar%20sample%20app.gif)

### Customizing the template

By default, this will create a core sample project, and a host project for each platform (Windows and macOS). To generate a project without a macOS host, use the following:

```bash
dotnet new interstellar --macOS false
```

And to disable Windows:

```bash
dotnet new interstellar --Windows false
```

To see more info about these options:

```bash
dotnet new interstellar --help
```
