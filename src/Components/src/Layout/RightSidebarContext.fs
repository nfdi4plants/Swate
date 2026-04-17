module Swate.Components.LayoutContexts.RightSidebarContext

open Feliz

type RightSidebarState<'A> = {
    isOpen: bool
    setIsOpen: bool -> unit
    sidebarType: 'A
    setSidebarType: 'A -> unit
}
with

    static member Empty<'A>() : RightSidebarState<'A> = {
        isOpen = false
        setIsOpen = ignore
        sidebarType = Unchecked.defaultof<'A>
        setSidebarType = ignore
    }

type RightSidebarCtxState = {
    isOpen: bool
    setIsOpen: bool -> unit
    sidebarType: obj
    setSidebarType: obj -> unit
}

let Empty: RightSidebarCtxState = {
    isOpen = false
    setIsOpen = ignore
    sidebarType = null
    setSidebarType = ignore
}

let RightSidebarCtx =
    React.createContext<RightSidebarCtxState> (Empty)

let toRightSidebarCtxState (state: RightSidebarState<'A>) : RightSidebarCtxState = {
    isOpen = state.isOpen
    setIsOpen = state.setIsOpen
    sidebarType = box state.sidebarType
    setSidebarType = fun nextSidebarType -> state.setSidebarType (unbox<'A> nextSidebarType)
}

let private unboxOrDefault<'A> (value: obj) : 'A =
    if obj.ReferenceEquals(value, null) then
        Unchecked.defaultof<'A>
    else
        unbox<'A> value

[<Hook>]
let useRightSidebarCtx<'A> () : RightSidebarState<'A> =
    let context = React.useContext RightSidebarCtx

    {
        isOpen = context.isOpen
        setIsOpen = context.setIsOpen
        sidebarType = unboxOrDefault<'A> context.sidebarType
        setSidebarType = fun nextSidebarType -> context.setSidebarType (box nextSidebarType)
    }
