# Interstellar

> NOTE: This API is not yet guarenteed to be stable or backward-compatible until v1.0, so breaking changes may occur at any time.

Interstellar is an F# library providing a standard, mixed-paradigm API for accessing browser controls on various platforms. Currently, there are 3 combinations platform and browser hosts available. See [Examples](https://github.com/jwosty/Interstellar/Examples) for a simple sample application. See https://github.com/jwosty/InterstellarFableHelloWorld for an example of combining Interstellar with [Fable](https://fable.io/), achieving a cross-platform desktop app built completely in F#.

## Quick Start

You will need the .Net 5 SDK. For the Windows projects, you should be able to use any of the standard IDEs (Visual Studio, Visual Studio Code, Rider \[untested but should work\]). For the macOS projects, you need to use Visual Studio for Mac.

Create a project from the template:

```bash
dotnet new -i Interstellar.Template
dotnet new interstellar -n <project-name>
```

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
