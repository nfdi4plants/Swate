namespace Swate.Components.JsBindings

open Browser.Types
open Fable.Core

module Dom =

    [<Emit("$0.target && $0.target.closest ? $0.target.closest('[data-tree-node-id]') : null")>]
    let closestTreeNodeElement (_event: MouseEvent) : HTMLElement = jsNative

    [<Emit("$0.querySelector('[data-tree-node-id=\"' + CSS.escape($1) + '\"]')")>]
    let queryTreeNodeElement (_root: HTMLElement) (_nodeId: string) : HTMLElement = jsNative

    [<Emit("requestAnimationFrame($0)")>]
    let requestAnimationFrame (_callback: unit -> unit) : int = jsNative
