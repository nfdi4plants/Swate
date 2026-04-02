module Swate.Components.LayoutContext

open Feliz
open Fable.Core

type LayoutContextType = StateContext<bool>

type SidebarState<'A> = {
    isOpen: bool
    setIsOpen: bool -> unit
    sidebarType: 'A
    setSidebarType: 'A -> unit
} with

    static member Empty<'A>() : SidebarState<'A> = {
        isOpen = false
        setIsOpen = ignore
        sidebarType = Unchecked.defaultof<'A>
        setSidebarType = ignore
    }

module LayoutContextType =

    let Empty: LayoutContextType = { state = false; setState = ignore }

/// Holds one stable React context instance for left sidebar. Otherwise we run into consistency issues with a generic argument
type private RightSidebarContextHolder<'A>() =
    static member val Context = React.createContext<SidebarState<'A>> (SidebarState<'A>.Empty()) with get

let RightSidebarContext<'A> = RightSidebarContextHolder<'A>.Context

let LeftSidebarContext =
    React.createContext<LayoutContextType> (LayoutContextType.Empty)
