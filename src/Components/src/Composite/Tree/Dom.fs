module Swate.Components.Composite.Tree.Dom

open Browser.Types
open Feliz
open Swate.Components.JsBindings

let tryGetNodeId (event: MouseEvent) =
    let element = Dom.closestTreeNodeElement event

    if isNull element then
        None
    else
        element.getAttribute "data-tree-node-id" |> Option.ofObj

let focusNode (treeRef: IRefValue<HTMLElement option>) nodeId =
    match treeRef.current with
    | Some root ->
        let element = Dom.queryTreeNodeElement root nodeId

        if not (isNull element) then
            element.focus ()
    | None -> ()

let focusNodeAfterRender treeRef nodeId =
    Dom.requestAnimationFrame (fun () -> focusNode treeRef nodeId) |> ignore
