# Interstellar

![CI (macOS)](https://github.com/jwosty/Interstellar/workflows/CI%20(macOS)/badge.svg) ![CI (Windows)](https://github.com/jwosty/Interstellar/workflows/CI%20(Windows)/badge.svg)

> NOTE: This API is not yet guarenteed to be stable or backward-compatible until v1.0, so breaking changes may occur at any time.

Interstellar is an F# library providing a standard, mixed-paradigm API for accessing browser controls on various platforms. Currently, there are 3 combinations platform and browser hosts available. See [Examples](Examples) for a simple sample application. See https://github.com/jwosty/InterstellarFableHelloWorld for an example of combining Interstellar with [Fable](https://fable.io/), achieving a cross-platform desktop app built completely in F#.

## Project breakdown
This project is composed of several NuGet packages. Let's break them down and describe their purposes:

- [Interstellar.Core](https://www.nuget.org/packages/Interstellar.Core/)
  - Core API definition. All Interstellar apps reference this. It contains everything you need to define the lifecycle for a browser-based application, agnostic to host platform and browser engine. Interstellar.Core is to Interstellar.macOS.WebKit and Interstellar.Wpf.Chromium as .Net Standard is to .Net Framework and .Net Core (though not a perfect analogy, it's helpful to think of it in that way).
- [Interstellar.macOS.WebKit](https://www.nuget.org/packages/Interstellar.macOS.WebKit/)
  - Implements Interstellar API on macOS using native Cocoa and WebKit. WebKit comes with the operating system, and therefore doesn't ship with a browser redistributable. The advantage is that your application bundle is lean. The downside is you have no control whatsoever over what version of WebKit your application will end up getting because its updates come with the OS. For end users with older versions of macOS, your application will run with an older version of WebKit.
- [Interstellar.Wpf.Chromium](https://www.nuget.org/packages/Interstellar.Wpf.Chromium/)
  - Implements Interstellar on Windows for WPF + Chromium. Because it uses [CEF](https://bitbucket.org/chromiumembedded/cef) (Chromium Embedded Framework), CEF will be included with your bundle application if you use this. The advantage is that you get to control the exact version of browser control that ships with your application. The downside is that your application bundle is large: a release build of [Examples.Wpf.Chromium](Examples/Examples.Wpf.Chromium/BrowserWindow.fs) clocks in at nearly 350MB. This is the Electron way of doing things, if you will.
- [Interstellar.WinForms.Chromium](https://www.nuget.org/packages/Interstellar.WinForms.Chromium/)
  - Just like Interstellar.Wpf.Chromium -- the only difference is that it uses the WinForms APIs. Most people should probably choose the WPF package, as it is more modern.
- [Interstellar.Chromium](https://www.nuget.org/packages/Interstellar.Chromium/)
  - Contains shared code between the Windows Chromium packages.

I intend to create a Windows package that uses the built-in Windows browser control (likely Edge with Chromium), as well as a macOS package that uses CEF (however this is a bigger undertaking as non of the CEF .NET bindings work on macOS yet). Contributions for either of these are welcome, as well as any other possible targets! Linux would be nice to support, but I don't have GTK experience (or whatever you'd use to wrap a browser control).

## Building

```bash
dotnet tool restore
dotnet paket restore
dotnet fake build
```

## Creating the NuGet package

After building, run:

```bash
dotnet fake build -t Pack
```

The resuling *.nupkg files will end up in ``./artifacts/``

## Building docs

```bash
dotnet fake build -t BuildDocs
// to actually release docs, assuming you have push priveleges to the repo:
dotnet fake biuld -t ReleaseDocs
```