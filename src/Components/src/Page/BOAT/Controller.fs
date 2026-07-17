module Modals.Controller

open Feliz

[<Literal>]
let private ModalContainerId_inner = "modal_inner_"

let private createId(name:string) = ModalContainerId_inner + name

let removeModal(name:string) =
    let id = createId name
    let ele = Browser.Dom.document.getElementById(id)
    if not <| isNull ele then ele.remove()

///<summary>Function to add a modal to the html body of the active document. If an object with the same name exists, it is removed first.</summary>
///<param name="name">The name of the modal, this is used for generate an Id for the modal by which it is later identified.</param>
///<param name="reactElement">The modal itself with a open parameter which will be the correct remove function for the modal.</param>
let renderModal(name: string, reactElement: (_ -> unit) -> ReactElement) =
    let parent = Browser.Dom.document.getElementById("modal-container")
    let id = createId name
    /// check if existing and if so remove
    let _ =
        let ele = Browser.Dom.document.getElementById(id)
        if not <| isNull ele then ele.remove()
    let child = Browser.Dom.document.createElement "div"
    child.id <- id
    parent.appendChild(child) |> ignore
    let ModalRoot = ReactDOM.createRoot(Browser.Dom.document.getElementById id)
    let rmv = fun _ -> 
        ModalRoot.unmount()
        removeModal(name)
    ModalRoot.render (reactElement rmv)
