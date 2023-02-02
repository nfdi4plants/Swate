module Modal.Controller

open Fable.React
open Fable.React.Props
open Fulma

[<Literal>]
let ModalContainerId_inner = "modal_inner_"

let createId (name:string) = ModalContainerId_inner + name

let renderModal (reactElement: Fable.React.ReactElement) (name: string) =
    let body = Browser.Dom.document.body
    let id = createId name
    /// check if existing and if so remove
    let _ =
        let ele = Browser.Dom.document.getElementById(id)
        if not <| isNull ele then ele.remove()
    let child = Browser.Dom.document.createElement "div"
    child.id <- id
    body.appendChild(child) |> ignore
    Feliz.ReactDOM.render(reactElement, Browser.Dom.document.getElementById id)
    ()

let removeModal(name:string) =
    let id = createId name
    let ele = Browser.Dom.document.getElementById(id)
    if not <| isNull ele then ele.remove()


// https://shmew.github.io/Feliz.SweetAlert/#/SweetAlert/Examples/Elmish/DynamicQueue

open Elmish
open Feliz
open Feliz.ElmishComponents
open Fable.SimpleJson