module Swate.Components.PopoverContext

open Fable.Core
open Feliz
open Swate.Components

type PopoverContext = {
    isOpen: bool
    setIsOpen: bool -> unit
    floating: FloatingUI.UseFloatingReturn
    interactions: FloatingUI.UseInteractionsReturn
    modal: bool
    labelId: string
    descriptionId: string
    debug: string option
    portalId: string option
    preserveTabOrder: bool
    initialFocus: obj option
    returnFocus: obj option
    visuallyHiddenDismiss: obj option
    closeOnFocusOut: bool option
}

let PopoverCtx = React.createContext<PopoverContext option> None

[<Hook>]
let usePopoverCtx () = React.useContext PopoverCtx