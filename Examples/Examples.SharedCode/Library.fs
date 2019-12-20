namespace Examples.SharedCode
open System
open System.Diagnostics
open Interstellar
open System.Threading
open System.Reflection
open System.Runtime.Versioning

module SimpleBrowserApp =
    let runtimeFramework = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName
    
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

    //let app = BrowserApp.create (fun mainCtx createWindow -> async {
    //    do! Async.SwitchToContext mainCtx
    //    Trace.WriteLine "Opening window"
    //    let page = sprintf "
    //        <html>
    //            <head>
    //                <title>Static HTML Example</title>
    //                <script>console.log('head script executed')</script>
    //            </head>
    //            <body>
    //                <p>Here is some static HTML.</p>
    //                <input type=\"button\" value=\"Click me\" onclick=\"window.interstellarBridge.postMessage('Hello from Javascript')\"/>
    //                <p id=\"dynamicContent\" />
    //                <p id=\"host\" />
    //                <p id=\"runtimeFramework\" />
    //                <p id=\"browserWindowPlatform\"/>
    //                <p id=\"browserEngine\" />
    //                <script>console.log('body script executed')</script>
    //            </body>
    //        </html>"
    //    let interstellarDetectorPageUri = Uri "https://gist.githack.com/jwosty/239408aaffd106a26dc2161f86caa641/raw/5af54d0f4c51634040ea3859ca86032694afc934/interstellardetector.html"
    //    let! page = async {
    //        use webClient = new System.Net.WebClient() in
    //            return webClient.DownloadString interstellarDetectorPageUri
    //    }
    //    let window = createWindow { defaultBrowserWindowConfig with showDevTools = true; html = Some page; address = None }
    //    window.Browser.JavascriptMessageRecieved.Add (fun msg ->
    //        Trace.WriteLine (sprintf "Recieved message: %s" msg)
    //    )
    //    startTitleUpdater mainCtx (sprintf "BrowserApp - %s") window
    //    do! window.Show ()
    //    do! Async.SwitchToThreadPool ()
    //    do! Async.Sleep 1_000 // FIXME: introduce some mechanism to let us wait until it is valid to start executing Javascript
    //    do! Async.SwitchToContext mainCtx
    //    let w, h = window.Size
    //    window.Size <- w + 100., h + 100.
    //    do! Async.SwitchToThreadPool ()
    //    do! Async.AwaitEvent window.Closed
    //})


    let showCalculatorWindow mainCtx (createWindow: BrowserWindowCreator) = async {
        let page = """
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
        let window = createWindow { defaultBrowserWindowConfig with showDevTools = true; html = Some page }
        do! window.Show ()
        return window
    }

    let app = BrowserApp.create (fun mainCtx createWindow -> async {
        let! calcWindow = showCalculatorWindow mainCtx createWindow
        do! Async.AwaitEvent calcWindow.Closed
    })