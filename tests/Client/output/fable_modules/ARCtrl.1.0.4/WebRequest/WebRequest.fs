module ARCtrl.WebRequest

open Fable.Core
open Fable.SimpleHttp
open Fable.Core.JsInterop

open Fable.SimpleHttp 

[<RequireQualifiedAccess>]
module NodeJs =
    open Fable.Core
    open Fable.Core.JsInterop
    open Fetch

    //// From here: https://github.com/fable-compiler/fable3-samples/blob/25ea2404b28c897988b144f0141bc116da292679/nodejs/src/App.fs#L7
    //#if FABLE_COMPILER
    //importSideEffects "isomorphic-fetch"
    //#endif

    let isNode() = 
        emitJsExpr 
            () 
            """typeof process !== "undefined" &&
process.versions != null &&
process.versions.node != null;"""

    let downloadFile url =
        fetch url []
        |> Promise.bind (fun res -> res.text()) // get the result
        |> Promise.map (fun txt -> // bind the result to make further operation
            txt
        )
        |> Async.AwaitPromise

let downloadFile url =
    let mutable isFable = false
    #if FABLE_COMPILER
    isFable <- true
    #endif
    let browserAndDotnet() =  
        async {
            let! (statusCode, responseText) = Http.get url

            return 
                match statusCode with
                | 200 -> responseText
                | _ -> failwithf "Status %d => %s" statusCode responseText
        }
    if not isFable then
        browserAndDotnet()
    else
        if NodeJs.isNode() then
            // From here: https://github.com/fable-compiler/fable3-samples/blob/25ea2404b28c897988b144f0141bc116da292679/nodejs/src/App.fs#L7
            importSideEffects "isomorphic-fetch"
            NodeJs.downloadFile url
        else
            browserAndDotnet()
