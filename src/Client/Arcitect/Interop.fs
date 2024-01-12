module ARCitect.Interop

open Fable.Core.JsInterop
open Model.ARCitect

let inline getUnionCaseName (x:'a) = 
    match Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(x, typeof<'a>) with
    | case, _ -> case.Name  

let inline getEventHandlerByName (inst: 'A) (s:string) =
    let fields = Microsoft.FSharp.Reflection.FSharpType.GetRecordFields(typeof<'A>)
    match fields |> Array.tryFind(fun t -> t.Name = s) with
    | Some pi -> Some(pi.GetValue(inst))
    | None -> None

let verifyARCitectMsg (e: Browser.Types.MessageEvent) =
    let content = e.data :?> {|swate: bool; api: string; data: obj|}
    let source = e.source
    if content.swate (*check source*) then
        Some content
    else
        None

let inline runApiFromName (apiHandler: 'E) (apiName: string) (data: 'A) =
    let func = getEventHandlerByName apiHandler apiName
    match func with
    | Some f ->
        let f: 'A -> unit = !!f
        f data
    | None ->
        ()

let inline postMessageToARCitect (msg: 'A, data) =
    let methodName = getUnionCaseName msg
    let createContent (data) = {|swate = true; api = methodName; data = data|}
    Browser.Dom.window.top.postMessage(createContent data, "*")

/// <summary>
/// Returns a function to remove the event listener
/// </summary>
/// <param name="eventHandler"></param>
let initEventListener (eventHandler: IEventHandler) : unit -> unit =
    let handle =
        fun (e: Browser.Types.Event) ->
            let e = e :?> Browser.Types.MessageEvent
            match verifyARCitectMsg e with
            | Some content ->
                runApiFromName eventHandler content.api content.data
            | None ->
                ()
    Browser.Dom.window.addEventListener("message", handle)
    fun () -> Browser.Dom.window.removeEventListener("message", handle)