module Swate.Components.GenericComponents.Popover.Context

open Fable.Core
open Feliz
open Swate.Components

type PopoverContext = {
    isOpen: bool
    setIsOpen: bool -> unit
    floating: FloatingUI.UseFloatingReturn
    interactions: FloatingUI.UseInteractionsReturn
    isMounted: bool
    status: FloatingUI.Status
    modal: bool
    labelId: string option
    setLabelId: (string option -> string option) -> unit
    descriptionId: string option
    setDescriptionId: (string option -> string option) -> unit
    debug: string option
    portalId: string option
    preserveTabOrder: bool
    initialFocus: obj option
    returnFocus: obj option
    visuallyHiddenDismiss: obj option
    closeOnFocusOut: bool option
    outsideElementsInert: bool option
    focusManagerDisabled: bool option
}

let PopoverCtx = React.createContext<PopoverContext option> None

[<Hook>]
let usePopoverCtx () = React.useContext PopoverCtx
