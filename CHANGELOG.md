# Changelog

## 0.4.0

* Fully support .NET 6 on Windows and macOS

## 0.3.3

* Fix FSharp.Core dependency version mistake in last release

## 0.3.2

* WinForms, WPF - Fix a bug that happens under Windows high-DPI mode
* Change targets to netcoreapp3.1

## 0.3.1

* WinForms - Fixed an issue that could cause apps to crash at startup in release mode (https://github.com/jwosty/Interstellar/issues/10 ; thanks to @amaitland for the fix)

## 0.3.0

* Add IBrowser.CanShowDevTools
* Interstellar.MacOS.WebKit
    * Remove unnecessary assembly references

## 0.2.0

* Add a generic parameter to IBrowserWindow that carries the type of the underlying window implementation, allowing for usages of native APIs within an Interstellar application without requiring dynamic casting
    * This also affects all types and functions that depend on IBrowserWindow -- so just about everything except IBrowser 
* Add executeJavascriptf, javascriptf, kjavascriptf, and IBrowser.ExecuteJavascriptf, which make it easy to safely format untrusted inputs into Javascript scripts for execution (a form of code injection)

## 0.1.0

* Fix BrowserApp.create not waiting for the window to close
* Make referencing events from non-UI threads always be a safe operation
* Add IBrowserWindow.IsShowing
* Add IBrowser.LoadAsync and IBrowser.LoadStringAsync
* Add BrowserWindowConfig.title record field and a WindowTitle type to accompany it, and specify the default title to track the page title
* (WinForms) - Fix the implementation of BrowserWindow.Dispose

## 0.0.1

* Initial release
