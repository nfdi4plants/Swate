module Swate.Components.Primitive.Tree.Dom

open Browser.Types
open Fable.Core
open Feliz

module TreeDom =

    [<Emit("$0.target && $0.target.closest ? $0.target.closest('[data-tree-node-id]') : null")>]
    let private closestTreeNodeElement (_event: MouseEvent) : HTMLElement = jsNative

    [<Emit("$0.querySelector('[data-tree-node-id=\"' + CSS.escape($1) + '\"]')")>]
    let private queryTreeNodeElement (_root: HTMLElement) (_nodeId: string) : HTMLElement = jsNative

    let tryGetNodeId (event: MouseEvent) =
        let element = closestTreeNodeElement event

        if isNull element then
            None
        else
            element.getAttribute "data-tree-node-id" |> Option.ofObj

    let focusNode (treeRef: IRefValue<HTMLElement option>) nodeId =
        match treeRef.current with
        | Some root ->
            let element = queryTreeNodeElement root nodeId

            if not (isNull element) then
                element.focus ()
        | None -> ()
