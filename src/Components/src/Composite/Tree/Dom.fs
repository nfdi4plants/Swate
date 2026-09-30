module internal Swate.Components.Composite.Tree.Dom

open Browser.Types
open Fable.Core
open Feliz
open Swate.Components

[<Literal>]
let private InteractiveElementSelector =
    "a[href],button,input,select,textarea,[role='button'],[role='link'],[contenteditable='true']"

type private Css =
    abstract escape: string -> string

[<Global("CSS")>]
let private css: Css = jsNative

let tryGetNodeId (event: MouseEvent) =
    BrowserEvent.tryGetClosest "[data-tree-node-id]" event
    |> Option.bind (fun (element: Element) -> element.getAttribute "data-tree-node-id" |> Option.ofObj)

let focusTreeItemFromEvent (event: MouseEvent) =
    BrowserEvent.tryGetClosest "[role='treeitem']" event
    |> Option.iter (fun element -> (element :?> HTMLElement).focus ())

let focusNode (treeRef: IRefValue<HTMLElement option>) nodeId =
    match treeRef.current with
    | Some root ->
        let selector = $"[data-tree-node-id=\"{css.escape nodeId}\"]"
        let element = root.querySelector selector

        if not (isNull element) then
            (element :?> HTMLElement).focus ()
    | None -> ()

let focusNodeAfterRender treeRef nodeId =
    Browser.Dom.window.requestAnimationFrame (fun _ -> focusNode treeRef nodeId)
    |> ignore

let originatesFromInteractiveDescendant (event: MouseEvent) =
    if obj.ReferenceEquals(event.target, event.currentTarget) then
        false
    else
        BrowserEvent.tryGetClosest InteractiveElementSelector event
        |> Option.exists (fun interactive ->
            let row = event.currentTarget :?> Element
            not (obj.ReferenceEquals(interactive, row)) && row.contains interactive
        )

let focusMovedOutsideTree (event: FocusEvent) =
    let tree: HTMLElement = unbox event.currentTarget
    let related: HTMLElement = unbox event.relatedTarget

    isNull (box related) || not (tree.contains related)
