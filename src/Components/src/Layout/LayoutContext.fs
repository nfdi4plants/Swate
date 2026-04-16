module Swate.Components.LayoutContext

open Feliz

type LeftSidebarContextType = StateContext<bool>

type RightSidebarState<'A> = {
    isOpen: bool
    setIsOpen: bool -> unit
    sidebarType: 'A
    setSidebarType: 'A -> unit
} with

    static member Empty<'A>() : RightSidebarState<'A> = {
        isOpen = false
        setIsOpen = ignore
        sidebarType = Unchecked.defaultof<'A>
        setSidebarType = ignore
    }

module LeftSidebarContextType =

    let Empty: LeftSidebarContextType = { state = false; setState = ignore }

/// Holds one stable React context instance for the right sidebar. Otherwise we run into consistency issues with a generic argument.
type private RightSidebarContextHolder<'A>() =
    static member val Context = React.createContext<RightSidebarState<'A>> (RightSidebarState<'A>.Empty()) with get

let RightSidebarContext<'A> = RightSidebarContextHolder<'A>.Context

let LeftSidebarContext =
    React.createContext<LeftSidebarContextType> (LeftSidebarContextType.Empty)
