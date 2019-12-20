namespace Interstellar.Chromium
open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Text
open CefSharp
open CefSharp.Handler

type ScriptLocation = Head | Body

module JSInjectionHelpers =
    // Dealing with inserting arbitrary contents into a script tag is hard to do right, because there's really no right way to do it, since
    // the inside of a script tag doesn't recognize HTML entities, meaning we can't just escape it for HTML. Granted, the way that this library
    // is meant to be used, I don't expect it to be a vulnerability source, but we can at least try this.
    // see https://blog.uploadcare.com/vulnerability-in-html-design-the-script-tag-33d24642359e
    // and the HTML spec's recommendations: https://www.w3.org/TR/html52/semantics-scripting.html#restrictions-for-contents-of-script-elements
    let escapeScriptTagContents (contents: string) =
        contents
            .Replace("<!--", "<\\!--")
            .Replace("<script>", "<\\script>")
            .Replace("</script>", "<\\/script>")

type JavascriptInjectionFilter(injectionPayload, ?injectionLocation) =
    let searchTag =
        match injectionLocation with
        | Some Head | None -> "<head>"
        | Some Body (* once told me the world is gonna roll me *) -> "<body>"

    let injection = sprintf "<script>%s</script>" (JSInjectionHelpers.escapeScriptTagContents injectionPayload)
    let overflow = new List<byte>()
    let mutable offset = 0

    interface IResponseFilter with
        override this.Dispose() = ()
        override this.Filter (dataIn, dataInRead, dataOut, dataOutWritten) =
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