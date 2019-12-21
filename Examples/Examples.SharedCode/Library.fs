namespace Examples.SharedCode
open System
open System.Diagnostics
open Interstellar
open System.Threading
open System.Reflection
open System.Runtime.Versioning

module AppletIds =
    let [<Literal>] Calculator = "calculator"
    let [<Literal>] InterstellarDetector = "detector"
    let [<Literal>] InterWindowCommunication = "interwindow"

module SimpleBrowserApp =
    let runtimeFramework = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName
    let detectorPageUrl = new Uri("https://gist.githack.com/jwosty/239408aaffd106a26dc2161f86caa641/raw/5af54d0f4c51634040ea3859ca86032694afc934/interstellardetector.html")

    //let startTitleUpdater mainCtx mapPageTitle (window: IBrowserWindow) =
    //    let work = async {
    //        use! holder = Async.OnCancel (fun () ->
    //            Trace.WriteLine "Window title updater cancelled")
    //        while true do
    //            let! pageTitle = Async.AwaitEvent window.Browser.PageTitleChanged
    //            Trace.WriteLine (sprintf "Browser page title is: %s" pageTitle)
    //            do! Async.SwitchToContext mainCtx
    //            window.Title <- mapPageTitle pageTitle
    //            do! Async.SwitchToThreadPool ()
    //    }
    //    let cancellation = new CancellationTokenSource()
    //    Async.Start (work, cancellation.Token)
    //    Async.Start <| async {
    //        do! Async.AwaitEvent window.Closed
    //        Trace.WriteLine "Window closed"
    //        cancellation.Cancel ()
    //    }

    let showCalculatorWindow mainCtx (createWindow: BrowserWindowCreator) = async {
        let page = """
            <!DOCTYPE html>
            <html>
                <head>
                    <script>
                        function isNumeric(x) {
                            return !isNaN(x)
                        }
                        function recalculate() {
                            var input1 = Number(document.getElementById("input1").value)
                            var input2 = Number(document.getElementById("input2").value)
                            var operand = document.getElementById("operand").value
                            var fOperand = (x,y) => x + y
                            if (operand === "sub") {
                                fOperand = (x,y) => x - y
                            } else if (operand === "mul") {
                                fOperand = (x,y) => x * y
                            } else if (operand === "div") {
                                fOperand = (x,y) => x / y
                            }
                            document.getElementById("result").textContent = fOperand(input1, input2)
                        }
                    </script>
                </head>
                <body>
                    <span>
                        <input type="number" id="input1" oninput="recalculate()" />
                        <select id="operand" onchange="recalculate()">
                            <option value="add">+</option>
                            <option value="sub">-</option>
                            <option value="mul">&times;</option>
                            <option value="div">&divide;</option>
                        </select>
                        <input type="number" id="input2" oninput="recalculate()" /t>
                        <span>=</span>
                        <span id="result" />
                    </span>
                </body>
            </html>"""
        let window = createWindow { defaultBrowserWindowConfig with html = Some page }
        do! window.Show ()
        return window
    }

    let showDetectorWindow mainCtx (createWindow: BrowserWindowCreator) = async {
        let window = createWindow { defaultBrowserWindowConfig with address = Some detectorPageUrl }
        do! window.Show ()
        return window
    }

    let runCodependentWindows mainCtx (createWindow: BrowserWindowCreator) = async {
        let inputPage = """
            <!DOCTYPE html>
            <html>
                <head>
                    <title>Input window</title>
                    <script>
                        function sendToOtherWindow() {
                            interstellarBridge.postMessage(document.getElementById("theInput").value)
                        }
                    </script>
                </head>
                <body>
                    <input id="theInput" placeholder="Enter some text here" oninput="sendToOtherWindow()" />
                </body>
            </html>"""
        let outputPage = """
            <!DOCTYPE html>
            <html>
                <head>
                    <title>Output window</title>
                    <script>
                        function updateOutput(newText) {
                            document.getElementById("theOutput").textContent = newText
                        }
                    </script>
                </head>
                <body>
                    <p id="theOutput" />
                </body>
            </html>"""
        let inputWindow = createWindow { defaultBrowserWindowConfig with html = Some inputPage }
        let outputWindow = createWindow { defaultBrowserWindowConfig with html = Some outputPage }

        inputWindow.Browser.JavascriptMessageRecieved.Add (fun msg ->
            outputWindow.Browser.ExecuteJavascript (sprintf "updateOutput('%s')" (String (Array.rev (msg.ToCharArray ()))))
        )

        do! inputWindow.Show ()
        do! outputWindow.Show ()
        let! bothClosed = Async.Parallel [Async.AwaitEvent inputWindow.Closed; Async.AwaitEvent outputWindow.Closed]
        ()
    }

    let appletSelectorWindow mainCtx createWindow = async {
        let page = sprintf """
            <!DOCTYPE html>
            <html>
                <head>
                    <script>
                        function ex(which) {
                            interstellarBridge.postMessage(which)
                        }
                    </script>
                </head>
                <body>
                    <button onclick="ex('%s')">Calculator</button>
                    <br>
                    <button onclick="ex('%s')">Interstellar detector</button> - %s
                    <br>
                    <button onclick="ex('%s')">Inter window communication</button>
                </body>
            </html>""" AppletIds.Calculator AppletIds.InterstellarDetector detectorPageUrl.AbsoluteUri AppletIds.InterWindowCommunication
        let selectorWindow : IBrowserWindow = createWindow { defaultBrowserWindowConfig with html = Some page }
        do! selectorWindow.Show ()
        selectorWindow.Browser.JavascriptMessageRecieved.Add (fun msg ->
            Async.Start <| async {
                do! Async.SwitchToContext mainCtx
                match msg with
                | AppletIds.Calculator -> do! Async.Ignore (showCalculatorWindow mainCtx createWindow)
                | AppletIds.InterstellarDetector -> do! Async.Ignore (showDetectorWindow mainCtx createWindow)
                | AppletIds.InterWindowCommunication ->
                    do! runCodependentWindows mainCtx createWindow
                    ()
                | msg -> Trace.WriteLine (sprintf "Bad message: %s" msg)
            })
        return selectorWindow
    }

    let app = BrowserApp.create (fun mainCtx createWindow -> async {
        let! mainWindow = appletSelectorWindow mainCtx createWindow
        do! Async.AwaitEvent mainWindow.Closed
    })