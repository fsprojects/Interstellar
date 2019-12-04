namespace Interstellar.Windows.Chromium.Wpf
open System
open System.Windows
open CefSharp
open CefSharp.Wpf
open Interstellar.Core

type BrowserWindow() =
    static do
        Platform.Initialize ()

    interface IBrowserWindow