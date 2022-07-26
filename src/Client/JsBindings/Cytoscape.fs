namespace Cytoscape

module JS =

    open Fable.Core
    open Fable.Core.JsInterop

    [<AutoOpen>]
    module rec Types =

        type ILayout =
            abstract member run: unit -> unit

        type ICytoscape =
            abstract member mount: Browser.Types.HTMLElement -> unit
            abstract member unmount: unit -> unit
            abstract member add: obj -> unit
            abstract member center: unit -> unit
            abstract member fit: unit -> unit
            abstract member layout: options:obj -> ILayout
            abstract member bind: event:string -> element:string -> (Browser.Types.MouseEvent -> unit) -> unit

    open Types

    let createNode id =
        {|data = {|id = id|}|} |> box

    let createEdge id source target =
        {|data = {|id = id; source = source; target = target|}|} |> box

    [<ImportDefault("Cytoscape")>]
    let cy(options:obj): ICytoscape = jsNative
