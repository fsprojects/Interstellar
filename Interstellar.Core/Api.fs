namespace Interstellar
open System
open System.Threading

/// <summary>Identifies a browser engine, which have web layout and Javascript execution capabilities</summary>
type BrowserEngine =
    /// <summary>
    ///     Google's open source <a href="https://www.chromium.org/Home">Chromium</a> browser engine, embedded and bundled along with the application using the
    ///     <a href="https://bitbucket.org/chromiumembedded/cef/src/master/">ChromiumEmbeddedFramework</a>
    /// </summary>
    | Chromium = 0
    /// <summary>Apple's native <a href="https://developer.apple.com/documentation/webkit">WebKit</a> browser engine built-in to macOS</summary>
    | AppleWebKit = 0b1
/// <summary>Indicates a host GUI framework, which is whatever will be used to create new windows and interact with the graphical system of the OS.</summary>
type BrowserWindowPlatform = | Wpf = 0b01 | WinForms = 0b11 | MacOS = 0b100

type IBrowser =
    /// <summary>The address which the browser is currently displaying, if any</summary>
    abstract Address : Uri option
    /// <summary>Whether or not this browser instance is showing any dev tools</summary>
    abstract AreDevToolsShowing : bool
    /// <summary>Indicates whether or not there is a previous page to go to</summary>
    abstract CanGoBack : bool
    /// <summary>Indicates whether or not there is a next page to go to</summary>
    abstract CanGoForward : bool
    /// <summary>Closes the developer tools for this browser if they are open</summary>
    abstract CloseDevTools : unit -> unit
    /// Identifies the browser engine that this Browser is using
    abstract Engine : BrowserEngine
    /// <summary>Executes some Javascript in the browser, returning immediately.</summary>
    abstract ExecuteJavascript : string -> unit
    /// <summary>
    ///     Loads a page from a given Uri, returning nested asyncs to signal changes in state relating to loading status of the page: the first async calls back when the
    ///     page finishes loading, and the second async calls back when the page is ready to start executing Javascript.
    /// </summary>
    abstract LoadAsync : Uri -> Async<Async<unit>>
    /// <summary>Starts loading a page from a given Uri, returning immediately.</summary>
    abstract Load : Uri -> unit
    /// <summary>
    ///     Directly loads the string as content for display. If a <see cref="uri"/> is given, it is used as the origin page.
    ///     Any Javascript AJAX calls will communicate using that URI. This method rturns nested asyncs to signal changes in
    ///     state relating to loading status of the page: the first async calls back when the page finishes loading, and the
    ///     second async calls back when the page is ready to start executing Javascript.
    /// </summary>
    abstract LoadStringAsync : html: string * ?uri: Uri -> Async<Async<unit>>
    /// <summary>
    ///     Directly loads the string as content for display, returning immediately. If a <see cref="uri"/> is given, it is
    ///     used as the origin page. Any Javascript AJAX calls will communicate using that URI.
    /// </summary>
    abstract LoadString : html:string * ?uri: Uri -> unit
    /// <summary>Attempts to navigate to the previous page in the history stack</summary>
    abstract GoBack : unit -> unit
    /// <summary>Attempts to navigate to the next page in the history stack</summary>
    abstract GoForward : unit -> unit
    /// <summary>Event handler that is called whenever a message sent from Javascript is recieved. It is safe to reference this event from a non-main thread.</summary>
    [<CLIEvent>] abstract JavascriptMessageRecieved : IEvent<string>
    /// <summary>The title of the currently loaded page</summary>
    abstract PageTitle : string
    [<CLIEvent>]
    /// <summary>Event handler that is called whenever a page load is completed. It is safe to reference this event from a non-main thread.</summary>
    abstract PageLoaded : IEvent<EventArgs>
    /// <summary>Initiates a reload of the current page</summary>
    abstract Reload : unit -> unit
    /// <summary>Event handler that is called whenever <see cref="PageTitle"/> changes. It is safe to reference this event from a non-main thread.</summary>
    [<CLIEvent>] abstract PageTitleChanged : IEvent<string>
    /// <summary>Shows the developer tools for this browser if they are not already showing</summary>
    abstract ShowDevTools : unit -> unit

/// <summary>
///     A natively-hosted graphical window that hosts a <see cref="Interstellar.IBrowser"/>
/// </summary>
type IBrowserWindow =
    inherit IDisposable
    /// <summary>The browser instance that this window is hosting</summary>
    abstract Browser : IBrowser
    /// <summary>Closes the window</summary>
    abstract Close : unit -> unit
    /// <summary>Whether or not the window has been shown but not yet closed.</summary>
    abstract IsShowing : bool
    /// <summary>Indicates the GUI platform that is hosting this window</summary>
    abstract Platform : BrowserWindowPlatform
    /// <summary>An event handler that is called when the window closes for any reason. It is safe to reference this event from a non-main thread.</summary>
    [<CLIEvent>] abstract Closed : IEvent<unit>
    /// <summary>Shows the window, asynchronously returning when <see cref="Browser"/> has finished initializing and is ready to use</summary>
    abstract Show : unit -> Async<unit>
    /// <summary>The size of the window, in pixels</summary>
    abstract Size : (float * float) with get, set
    /// <summary>An event handler this is called when the window is shown. It is safe to reference this event from a non-main thread.</summary>
    [<CLIEvent>] abstract Shown : IEvent<unit>
    /// <summary>Gets or sets text in the window's title bar</summary>
    abstract Title : string with get, set

type BrowserWindowConfig = {
    /// <summary>
    ///     When set to Some on its own with a value of None for <see cref="html"/>, it specifies the address for an <see cref="IBrowser"/> to initially load.
    ///     When both <see cref="address"/> and <see cref="html"/> are values of Some, they specify the html content to load directly in to a browser instance
    ///     when it first shows.
    /// </summary>
    address: Uri option;
    /// <summary>
    ///     When set, indicates the html to display in the browser directly when it is first shown. If <see cref="address"/> is also set, <see cref="address"/>
    ///     will be used as the origin URI for any requests made by AJAX calls in Javascript.
    /// </summary>
    html: string option
    /// <summary>Whether or not to show the dev tools when the browser window first opens</summary>
    showDevTools: bool
    /// <summary>A function that, when set, defines what the window title should be</summary>
    titleMapping: option<string -> IBrowserWindow -> Async<string>>
}

module BrowserWindowConfig =
    let defaultValue = {
        address = None
        html = None
        showDevTools = false
        titleMapping = Some(fun title _ -> async.Return title )
    }
    /// <summary>
    ///     Intended for use by platform implementations; applications generally shouldn't need this function. Attaches
    ///     a title mapping function to a browser window, making sure that the installed event handler gets cleaned up
    /// </summary>
    let attachTitleMappingHandler mainCtx (browserWindow: IBrowserWindow) (disposed: IEvent<_,_>) titleMapping =
        let titleMappingHandler = Handler(fun sender pageTitle ->
            Async.StartImmediate <| async {
                let! newTitle = titleMapping pageTitle browserWindow
                do! Async.SwitchToContext mainCtx
                browserWindow.Title <- newTitle
            }
        )
        let pageTitleChanged = browserWindow.Browser.PageTitleChanged
        pageTitleChanged.AddHandler titleMappingHandler
        // I believe this is necessary because out titleMapping function definitely references some other objects
        disposed.Add (fun _ ->
            try pageTitleChanged.RemoveHandler titleMappingHandler
            // if this log line ever shows up, I'm betting that we've gotten some threading wrong or something
            with e -> eprintfn "Exception while removing titleMappingHandler. This is likely indicates a bug in the library and may cause other issues; please contact the Interstellar maintainer(s) or add an issue on GitHub to fix the problem: https://github.com/jwosty/Interstellar\nFull exception: %A" e
        )

/// <summary>Represents a factory function that is used to instantiate a browser window for some host platform and engine</summary>
type BrowserWindowCreator = BrowserWindowConfig -> IBrowserWindow

type BrowserApp = {
    /// <summary>
    ///     A function that describes the entire asynchronous lifecycle of an browser application. Once all libraries and dependencies have loaded, and the program is
    ///     ready to create browser windows, this function will be called to kick off the user application's behavior. The first <see cref="SynchronizationContext"/>
    ///     parameter captures the GUI thread context, which can be used to safely call <see cref="IBrowserWindow"/> methods. The second paramter is a function which
    ///     can be used to instantiate browser windows for a given platform host and engine combination.
    /// </summary>
    onStart : SynchronizationContext -> BrowserWindowCreator -> Async<unit>
}

module BrowserApp =
    /// A do-nothing application
    let zero = { onStart = fun _ _ -> async { () } }
    /// Creates an application with the given onStart function
    let create onStart = { onStart = onStart }
    let openAddress address = { onStart = fun mainCtx createWindow -> async {
            do! Async.SwitchToContext mainCtx
            let window = createWindow { BrowserWindowConfig.defaultValue with address = Some address }
            do! window.Show ()
            do! Async.AwaitEvent window.Closed
        }
    }

[<AutoOpen>]
module Core =
    let defaultBrowserWindowConfig = BrowserWindowConfig.defaultValue