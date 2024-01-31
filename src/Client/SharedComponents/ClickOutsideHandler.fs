namespace Components

open Feliz
open Fable.Core
open Browser.Types
open Fable.Core.JsInterop

type ClickOutsideHandler =

    static member AddListener(containerId: string, clickedOutsideEvent: Event -> unit) =
        let rec closeEvent = fun (e: Event) ->
            let dropdown = Browser.Dom.document.getElementById(containerId)
            let isClickedInsideDropdown : bool = dropdown?contains(e.target)
            if not isClickedInsideDropdown then 
                clickedOutsideEvent e
                Browser.Dom.document.removeEventListener("click", closeEvent)
        Browser.Dom.document.addEventListener("click", closeEvent)

    static member AddListener(element: IRefValue<HTMLElement option>, clickedOutsideEvent: Event -> unit) =
        let rec closeEvent = fun (e: Event) ->
            let dropdown = element.current
            let isClickedInsideDropdown : bool = dropdown?contains(e.target)
            if not isClickedInsideDropdown then 
                clickedOutsideEvent e
                Browser.Dom.document.removeEventListener("click", closeEvent)
        Browser.Dom.document.addEventListener("click", closeEvent)
