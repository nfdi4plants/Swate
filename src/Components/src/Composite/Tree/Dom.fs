module Swate.Components.Composite.Tree.Dom

open Browser.Types
open Fable.Core
open Feliz

[<Literal>]
let private InteractiveElementSelector =
    "a[href],button,input,select,textarea,[role='button'],[role='link'],[contenteditable='true']"

type private Css =
    abstract escape: string -> string

[<Global("CSS")>]
let private css: Css = jsNative

[<Emit("requestAnimationFrame($0)")>]
let private requestAnimationFrame (_callback: unit -> unit) : int = jsNative

let tryGetNodeId (event: MouseEvent) =
    let target = event.target :?> Element

    target.closest "[data-tree-node-id]"
    |> Option.bind (fun element -> element.getAttribute "data-tree-node-id" |> Option.ofObj)

let focusNode (treeRef: IRefValue<HTMLElement option>) nodeId =
    match treeRef.current with
    | Some root ->
        let selector = $"[data-tree-node-id=\"{css.escape nodeId}\"]"
        let element = root.querySelector selector

        if not (isNull element) then
            (element :?> HTMLElement).focus ()
    | None -> ()

let focusNodeAfterRender treeRef nodeId =
    requestAnimationFrame (fun () -> focusNode treeRef nodeId) |> ignore

let originatesFromInteractiveDescendant (event: MouseEvent) =
    if obj.ReferenceEquals(event.target, event.currentTarget) then
        false
    else
        let target = event.target :?> Element
        target.closest InteractiveElementSelector |> Option.isSome

let focusMovedOutsideTree (event: FocusEvent) =
    let tree: HTMLElement = unbox event.currentTarget
    let related: HTMLElement = unbox event.relatedTarget

    isNull (box related) || not (tree.contains related)
