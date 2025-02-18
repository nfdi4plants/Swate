module MessageInterop

open System
open FSharp.Reflection
open Fable.Core
open System.Collections.Generic
open Fable.Core.JsInterop

type private IMessagePayload = {|
    /// must be set to true to target correct event handler
    swate: bool
    /// if action started, set to api name
    /// This functions as additional fail save:
    /// 1. If api is set, the message is a request.
    /// 2. If PendingRequests contains the requestId, the message is a request.
    /// Only if both conditions are met, Swate sends a response.
    api: string option
    requestId: string option
    data: obj option
    error: string option
|}

type PendingRequests = Dictionary<string, (obj -> unit) * (exn -> unit)>

type InteropOptions = {
    Target: Browser.Types.Window
    GenericErrorHandler: exn -> unit
}

open System
open Fable.Core
open Fable.SimpleJson
open Browser.Types

module private Helper =

    let private sendMsgWithResponse (pendingRequestsDictionary: PendingRequests) (target: Browser.Types.Window) (payload: IMessagePayload) =
        Promise.create (fun resolve reject ->
            // create timeout for response
            let timeout =
                Fable.Core.JS.setTimeout
                    (fun () ->
                        pendingRequestsDictionary.Remove(payload.requestId.Value) |> ignore
                        reject (new TimeoutException("Request timed out"))
                    )
                    5000
            pendingRequestsDictionary.Add(payload.requestId.Value, (resolve, reject))
            target.postMessage(payload, "*")
        )

    let rec getReturnType typ =
        if Reflection.FSharpType.IsFunction typ then
            let _, res = Reflection.FSharpType.GetFunctionElements typ
            getReturnType res
        elif typ.IsGenericType then
            typ.GetGenericArguments () |> Array.head
        else
            typ

    let proxyCall (target: Browser.Types.Window) (func: RecordField) pendingRequestsDictionary =
        let funcArgs : (TypeInfo [ ]) =
            match func.FieldType with
            | TypeInfo.Func getArgs -> getArgs()
            | _ -> failwithf "MessageInterop-Error: Field %s does not have a valid definiton" func.FieldName

        let argumentCount = (Array.length funcArgs) - 1
        let returnTypeAsync = Array.last funcArgs

        let funcNeedParameters =
            match funcArgs with
            | [| TypeInfo.Async _ |] -> false
            | [| TypeInfo.Promise _ |] -> false
            | [| TypeInfo.Unit; TypeInfo.Async _ |] -> false
            | otherwise -> true

        let executeRequest =
            let returnType =
                match returnTypeAsync with
                | TypeInfo.Promise getPromiseTypeArgument -> getPromiseTypeArgument()
                | _ -> failwithf "MessageInterop-Error:: Expected field %s to have a return type of Async<'t> or Task<'t>" func.FieldName

            fun requestBody -> sendMsgWithResponse pendingRequestsDictionary target requestBody

        fun arg0 arg1 arg2 arg3 arg4 arg5 arg6 arg7 ->
            let inputArguments =
               if funcNeedParameters
               then Array.take argumentCount [| box arg0;box arg1;box arg2;box arg3; box arg4; box arg5; box arg6; box arg7 |]
               else [| |]

            let requestBody: IMessagePayload =
                {| swate = true; api = Some func.FieldName; data = Some inputArguments; requestId = Some (System.Guid.NewGuid().ToString()); error = None |}

            executeRequest requestBody

module MessageInterop =

    let createApi() : InteropOptions = {
        Target = Browser.Dom.window.parent
        GenericErrorHandler = fun exn -> Browser.Dom.console.log($"Proxy Error: {exn.Message}")
    }

    let withErrorHandler errorHandler options : InteropOptions = { options with GenericErrorHandler = errorHandler }

    let withTarget target options : InteropOptions = { options with Target = target }

type MessageInterop() =

    // Function to generate a new instance dynamically
    static member buildOutProxy (target: Browser.Types.Window, resolvedType: Type, pendingRequestsDictionary: PendingRequests) : 'T =
    
        if not (FSharpType.IsRecord resolvedType) then
            failwithf "MessageInterop-Error: Provided type is not a record. %s" resolvedType.FullName

        let schemaType = createTypeInfo resolvedType
        match schemaType with
        | TypeInfo.Record getFields ->
            let (fields, recordType) = getFields()
            let recordFields = [|
                for field in fields do
                    let normalize n =
                        let fn = Helper.proxyCall target field pendingRequestsDictionary
                        // this match case comes from Fable.Remoting
                        // https://github.com/Zaid-Ajaj/Fable.Remoting/blob/9bf4dab1987abad342c671cb4ff1a8a7e0e846d0/Fable.Remoting.Client/Remoting.fs#L58
                        // I cannot trigger any case other than 1 arguments, as all record type arguments are parsed into a tuple
                        match n with
                        | 0 ->
                            box (fn null null null null null null null null)
                        | 1 ->
                            box (fun a ->
                                fn a null null null null null null null)
                        | 2 ->
                            let proxyF a b = fn a b null null null null null null
                            unbox (System.Func<_,_,_> proxyF)
                        | 3 ->
                            let proxyF a b c = fn a b c null null null null null
                            unbox (System.Func<_,_,_,_> proxyF)
                        | 4 ->
                            let proxyF a b c d = fn a b c d null null null null
                            unbox (System.Func<_,_,_,_,_> proxyF)
                        | 5 ->
                            let proxyF a b c d e = fn a b c d e null null null
                            unbox (System.Func<_,_,_,_,_,_> proxyF)
                        | 6 ->
                            let proxyF a b c d e f = fn a b c d e f null null
                            unbox (System.Func<_,_,_,_,_,_,_> proxyF)
                        | 7 ->
                            let proxyF a b c d e f g = fn a b c d e f g null
                            unbox (System.Func<_,_,_,_,_,_,_,_> proxyF)
                        | 8 ->
                            let proxyF a b c d e f g h = fn a b c d e f g h
                            unbox (System.Func<_,_,_,_,_,_,_,_,_> proxyF)
                        | _ ->
                            failwithf "MessageInterop-Error: Cannot generate proxy function for %s. Only up to 8 arguments are supported. Consider using a record type as input" field.FieldName

                    let argumentCount =
                        match field.FieldType with
                        | TypeInfo.Async _  -> 0
                        | TypeInfo.Promise _  -> 0
                        | TypeInfo.Func getArgs -> Array.length (getArgs()) - 1
                        | _ -> 0

                    normalize argumentCount
                |]

            let proxy = FSharpValue.MakeRecord(recordType, recordFields)
            unbox<'T> proxy
        | _ ->
            failwithf "MessageInterop-Error: Cannot build proxy. Exepected type %s to be a valid protocol definition which is a record of functions" resolvedType.FullName

    static member buildInProxy(recordType, recordTypeType: Type, target: Browser.Types.Window, handleGenericError, pendingRequestsDictionary: PendingRequests) =

        let schemaType = createTypeInfo recordTypeType
        match schemaType with
        | TypeInfo.Record getFields ->
            let (fields, _) = getFields()
            for field in fields do
                let funcArgs : (TypeInfo [ ]) =
                    match field.FieldType with
                    | TypeInfo.Async _ -> [| field.FieldType |]
                    | TypeInfo.Promise _ -> [| field.FieldType |]
                    | TypeInfo.Func getArgs -> getArgs()
                    | _ -> failwithf "MessageInterop-Error: Field %s does not have a valid definiton" field.FieldName
                let returnTypeAsync = Array.last funcArgs
                match returnTypeAsync with
                | TypeInfo.Promise _ -> ()
                | _ -> failwith "MessageInterop-Error: Only Promise return types are supported for incoming messages"
        | _ ->
            ()

        let verifyMsg (e: Browser.Types.MessageEvent) =
            let content = e.data :?> IMessagePayload
            if content.swate then
                Some content
            else
                None

        let getEventHandlerByName (inst: 'A) (s:string) =
            let fields = Microsoft.FSharp.Reflection.FSharpType.GetRecordFields(recordTypeType)
            match fields |> Array.tryFind(fun t -> t.Name = s) with
            | Some pi -> Some(pi.GetValue(inst))
            | None -> None

        let runApiFromName (apiHandler: 'E) (apiName: string) (data: 'A) =
            let func = getEventHandlerByName apiHandler apiName
            match func with
            | Some f ->
                let f: 'A -> JS.Promise<obj> = !!f
                f data
            | None ->
                failwith $"MessageInterop-Error: No such API function found in Incoming API: {apiName}"

        // TODO: support async functions
        let resolveIncMessage (apiHandler: 'E) (content: IMessagePayload) =
            match content.api with
            | Some api ->
                promise {
                    let! payload = 
                        try
                            promise {
                                let! r = runApiFromName apiHandler api content.data
                                return {| content with data = Some r|}
                            }
                        with
                            | exn ->
                                let p: IMessagePayload = {| content with error = Some exn.Message; data = None|}
                                Promise.lift p
                        |> Promise.map (fun (payload: IMessagePayload) ->
                            let result: IMessagePayload = {| payload with api = None |}
                            result
                        )
                    
                    target.postMessage(payload, "*")
                }
                |> Promise.start
            | None ->
                let payload: IMessagePayload =
                    {| content with error = Some "No API name given!"|}
                target.postMessage(payload, "*")

        let handle = 
            fun (e: Browser.Types.Event) ->
                let e = e :?> Browser.Types.MessageEvent
                match verifyMsg e with
                | Some content ->
                    if content.error.IsSome then
                        let exn = new Exception(content.error.Value)
                        match pendingRequestsDictionary.TryGetValue(content.requestId.Value) with
                        | true, (_, reject) ->
                            pendingRequestsDictionary.Remove(content.requestId.Value) |> ignore
                            reject exn
                        | _ ->
                            handleGenericError exn
                    elif content.requestId.IsSome then
                        match pendingRequestsDictionary.TryGetValue(content.requestId.Value) with
                        | true, (resolve, _) ->
                            log "[Swate] response from ARCitect"
                            pendingRequestsDictionary.Remove(content.requestId.Value) |> ignore
                            resolve content.data
                        | _ ->
                            log "[Swate] request from ARCitect"
                            resolveIncMessage recordType content
                    else
                        log "MessageInterop-Warning: Unhandled ARCitect msg"
                | None ->
                    ()

        Browser.Dom.window.addEventListener("message", handle)
        fun () -> Browser.Dom.window.removeEventListener("message", handle)
        
    static member inline buildProxy<'o, 'i> (incomingMsgHandler: 'i) (options: InteropOptions) : 'o * (unit -> unit) =
        let PendingRequests = PendingRequests()
        let inType = typeof<'i>
        let outType = typeof<'o>
        MessageInterop.buildOutProxy(options.Target, outType, PendingRequests),
        MessageInterop.buildInProxy(incomingMsgHandler, inType, options.Target, options.GenericErrorHandler, PendingRequests)
