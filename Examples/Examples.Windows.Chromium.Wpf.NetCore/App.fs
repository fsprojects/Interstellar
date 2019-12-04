namespace Example.Windows.Chromium.Wpf
open System
open System.Windows
open System.Windows.Controls

type MainWindow() =
    inherit Window()
    
    override this.OnInitialized (e: EventArgs) =
        base.OnInitialized e
        let closeButton = new Button(Content = "Close")
        this.Content <- closeButton
        closeButton.Click.Add (fun e -> this.Close ())

type App() =
    inherit Application()
    override this.OnStartup(e: StartupEventArgs) =
        base.OnStartup e
        let window = new MainWindow()
        window.Show ()

module Main =
    [<EntryPoint; STAThread>]
    let main argv =
        let app = new App()
        app.Run ()