# Changelog

## 0.2.1-alpha

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
