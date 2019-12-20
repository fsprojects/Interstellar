# Interstellar
Interstellar is an F# library providing a standard, mixed-paradigm API for accessing browser controls on various platforms. Refer the Examples/ folder for sample projects. Currently, there are 3 supported platform+browser host combos supported:
- macOS host + WebKit browser engine
  - This package takes advantage of the browser engine that comes built-into the operating system, meaning that you won't have to ship a browser engine in your application bundle. This leads to a demonstrably smaller app bundle.
- Windows WPF host + Chromium browser engine via CEF (Chromium Embedded Framework)
  - This package ships the CEF redistributables, leading to larger app bundles. However, this gives you more control over the specific browser version that your app will use.
- Windows WinForms host + Chromium browser engine via CEF
  - This is the same as the other Windows package, just with a WinForms window host instead of WPF, if that's your thing.
  
I intend to add a Windows package that will allow you to target the built-in Windows browser control (likely Edge wih Chromium), as well as a macOS package that allows you to target CEF (however this is a bigger undertaking as there are no .NET bindings for CEF that work on macOS). Contributions for either of these are welcome, as well as any other possible targets! Linux would be nice to support, but I don't have GTK experience (or whatever you'd use to wrap a browser control).
