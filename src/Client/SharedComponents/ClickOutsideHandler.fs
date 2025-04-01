namespace Components

open Feliz
open Fable.Core
open Browser.Types
open Fable.Core.JsInterop

type ClickOutsideHandler =

    static member AddListener(containerId: string, clickedOutsideEvent: Event -> unit) =
        let rec closeEvent =
            fun (e: Event) ->
                let rmv = fun () -> Browser.Dom.document.removeEventListener ("click", closeEvent)
                let dropdown = Browser.Dom.document.getElementById (containerId)

                if isNull dropdown then
                    rmv ()
                else
                    let isClickedInsideDropdown: bool = dropdown?contains (e.target)

                    if not isClickedInsideDropdown then
                        clickedOutsideEvent e
                        rmv ()

        Browser.Dom.document.addEventListener ("click", closeEvent)

    static member AddListener(element: IRefValue<HTMLElement option>, clickedOutsideEvent: Event -> unit) =
        let rec closeEvent =
            fun (e: Event) ->
                let rmv = fun () -> Browser.Dom.document.removeEventListener ("click", closeEvent)
                let dropdown = element.current

                if dropdown.IsNone then
                    rmv ()
                else
                    let isClickedInsideDropdown: bool = dropdown?contains (e.target)

                    if not isClickedInsideDropdown then
                        clickedOutsideEvent e
                        rmv ()

        Browser.Dom.document.addEventListener ("click", closeEvent)