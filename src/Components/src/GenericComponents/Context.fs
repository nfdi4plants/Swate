module Swate.Components.GenericComponents.Context

open Feliz

type BaseModalContext = {
    isOpen: bool
    setIsOpen: bool -> unit
    headerId: string
    descId: string
}

let BaseModalCtx = React.createContext<BaseModalContext option> (None)

[<Hook>]
let useBaseModalCtx () = React.useContext BaseModalCtx

type SelectContextValue = {|
    activeIndex: int option
    selectedIndices: Set<int>
    optionCount: int
    getItemProps: obj -> obj
    handleSelect: int option -> unit
|}

module SelectContextValue =

    let init () : SelectContextValue = {|
        activeIndex = None
        selectedIndices = Set.empty
        optionCount = 0
        getItemProps = fun _ -> null
        handleSelect = fun _ -> ()
    |}

let SelectCtx = React.createContext<SelectContextValue> (SelectContextValue.init ())

[<Hook>]
let useSelectCtx () = React.useContext SelectCtx
