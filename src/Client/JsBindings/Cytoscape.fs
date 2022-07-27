namespace Cytoscape

module JS =

    open Fable.Core
    open Fable.Core.JsInterop

    [<AutoOpen>]
    module rec Types =

        type ILayout =
            abstract member run: unit -> unit

        type Position = {|
            x: int
            y: int
        |}

        type ICytoscape =
            abstract member mount: Browser.Types.HTMLElement -> unit
            abstract member unmount: unit -> unit
            abstract member add: obj -> unit
            abstract member center: unit -> unit
            abstract member center: obj -> unit
            abstract member fit: unit -> unit
            abstract member layout: options:obj -> ILayout
            abstract member bind: event:string -> element:string -> (Browser.Types.MouseEvent -> unit) -> unit

    open Types

    type Node =
        static member create(id, ?data:seq<string*obj>, ?position: Position) =
            createObj [
                "data" ==>
                    createObj [
                        "id" ==> string id
                        if data.IsSome then
                            let obj = data.Value |> Seq.map (fun (key, v) -> key ==> v)
                            yield! obj
                    ]
                if position.IsSome then
                    printfn "target accession (%A) %A" id position.Value
                    "position" ==> createObj [
                        "x" ==> position.Value.x
                        "y" ==> position.Value.y
                    ]
            ]

    type Edge =
        static member create(id, source, target, ?data:seq<string*obj>) =
            createObj [
                "data" ==>
                    createObj [
                        "id" ==> string id
                        "source" ==> string source
                        "target" ==> string target
                        if data.IsSome then
                            let obj = data.Value |> Seq.map (fun (key, v) -> key ==> v)
                            yield! obj
                    ]
            ]

    let createNode id =
        {|data = {|id = id|}|} |> box

    let createEdge id source target =
        {|data = {|id = id; source = source; target = target|}|} |> box

    [<ImportDefault("Cytoscape")>]
    let cy(options:obj): ICytoscape = jsNative
