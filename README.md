# Interstellar

Interstellar is an F# library providing a standard, mixed-paradigm API for accessing browser controls on various platforms. Refer the Examples/ folder for sample projects. Currently, there are 3 combinations platform and browser hosts available. Here's a breakdown of the NuGet packages and what they correspond to (though they should be pretty self-explanatory):

- Interstellar.Core
  - Contains all the shared code, as well as the API definitions for all Interstellar platform implementations. It contains everything you need to define the lifecycle for a browser-based application, agnostic to host platform and browser engine. Interstellar.Core is to Interstellar.macOS.WebKit and Interstellar.Wpf.Chromium as .Net Standard is to .Net Framework and .Net Core.
- Interstellar.macOS.WebKit
  - Implements Interstellar on macOS, using Cocoa Apis + WebKit. It uses the OS's built-in WebKit browser control, and therefore doesn't ship with a browser redistributable. The advantage is that your application bundle shouldn't be massive. The downside is you have no control whatsoever over what version of WebKit your application will end up using. For end users with old versions of macOS, your application will run with an old WebKit.
- Interstellar.Wpf.Chromium
  - Implements Interstellar on Windows for WPF + Chromium. Because it uses CEF, CEF will be included with your bundle application if you use this. The advantage is that you get to control the exact version of browser control that ships with your application. The downside is that your application bundle is now very large. This is the Electron method, if you will.
- Interstellar.WinForms.Chromium
  - Just like Interstellar.Wpf.Chromium -- the only difference is that it uses the WinForms apis. Most people should probably choose the WPF package, as it is more modern.
- Interstellar.Chromium
  - Used internally. Contains shared code between the Windows Chromium packages.

I intend to create a Windows package that uses the built-in Windows browser control (likely Edge with Chromium), as well as a macOS package that uses CEF (however this is a bigger undertaking as there are no .NET bindings for CEF that work on macOS). Contributions for either of these are welcome, as well as any other possible targets! Linux would be nice to support, but I don't have GTK experience (or whatever you'd use to wrap a browser control).

This API is not yet guarenteed to be stable or backward-compatible until v1.0, so breaking changes may occur at any time.

## Building

In a Unix shell, or PowerShell:

```bash
dotnet tool restore
paket restore
dotnet fake run build.fsx
```

## Creating the NuGet package

After building, run:

```bash
dotnet fake run build.fsx -t pack
```
