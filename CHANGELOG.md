# Changelog

## 0.1.0

* Fix BrowserApp.create not waiting for the window to close
* Make referencing events from non-UI threads always be a safe operation
* Add IBrowserWindow.IsShowing
* Add IBrowser.LoadAsync and IBrowser.LoadStringAsync
* Add BrowserWindowConfig.title record field and a WindowTitle type to accompany it, and specify the default title to track the page title
* (WinForms) - Fix the implementation of BrowserWindow.Dispose

## 0.0.1

* Initial release
