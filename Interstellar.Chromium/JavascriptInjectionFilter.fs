namespace Interstellar.Chromium
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text
open CefSharp
open CefSharp.Handler

type ScriptLocation = Head | Body

type JavascriptInjectionFilter(injectionPayload, ?injectionLocation) =
    let searchTag =
        match injectionLocation with
        | Some Head | None -> "<head>"
        | Some Body (* once told me the world is gonna roll me *) -> "<body>"

    let overflow = new List<byte>()
    let mutable offset = 0

    interface IResponseFilter with
        override this.Dispose() = ()
        override this.Filter (dataIn, dataInRead, dataOut, dataOutWritten) =
            // FIXME: escaping script tags is hard. but we should figure out some way to do that, or at least disallow invalid inputs.
            // https://blog.uploadcare.com/vulnerability-in-html-design-the-script-tag-33d24642359e
            let injection = sprintf "<script>%s</script>" injectionPayload
            
            dataInRead <- if dataIn = null then 0L else dataIn.Length
            dataOutWritten <- 0L

            if overflow.Count > 0 then
                let bufferSize = min overflow.Count (int dataOut.Length)
                dataOut.Write (overflow.ToArray (), 0, bufferSize)
                dataOutWritten <- dataOutWritten + int64 bufferSize

                if (bufferSize < overflow.Count) then
                    overflow.RemoveRange (0, bufferSize - 1)
                else overflow.Clear ()
            
            for _ in 0L .. (dataInRead - 1L) do
                let readByte = byte (dataIn.ReadByte ())
                let readChar = Convert.ToChar readByte
                let mutable bufferSize = dataOut.Length - dataOutWritten
                
                if bufferSize > 0L then
                    dataOut.WriteByte readByte
                    dataOutWritten <- dataOutWritten + 1L
                else overflow.Add readByte
                
                if (Char.ToLower readChar) = searchTag.[offset] then
                    offset <- offset + 1
                    if offset >= searchTag.Length then
                        offset <- 0
                        bufferSize <- min (int64 injection.Length) (dataOut.Length - dataOutWritten)
                        if bufferSize > 0L then
                            let data = Encoding.UTF8.GetBytes injection
                            dataOut.Write (data, 0, int bufferSize)
                            dataOutWritten <- dataOutWritten + bufferSize
                        if bufferSize < int64 injection.Length then
                            let remaining = injection.Substring (int bufferSize, int ((int64 injection.Length) - bufferSize))
                            overflow.AddRange (Encoding.UTF8.GetBytes remaining)
                else offset <- 0
            
            if overflow.Count > 0 || offset > 0 then
                FilterStatus.NeedMoreData
            else FilterStatus.Done

        override this.InitFilter () = true

type JSInjectionResourceRequestHandler(injectionPayload) =
    inherit ResourceRequestHandler()

    override this.GetResourceResponseFilter (browserControl,  browser, frame, request, response) =
        upcast new JavascriptInjectionFilter(injectionPayload)

type JSInjectionRequestHandler(injectionPayload) =
    inherit RequestHandler()

    override this.GetResourceRequestHandler (chromiumWebBrowser, browser, frame, request, isNavigation, isDownload, requestInitiator, disableDefaultHandling) =
        if frame.IsMain && request.ResourceType = ResourceType.MainFrame then
            upcast new JSInjectionResourceRequestHandler(injectionPayload)
        else null