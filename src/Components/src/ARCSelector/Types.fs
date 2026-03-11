module Swate.Components.SelectorTypes

type ARCPointer = {
    name: string
    path: string
    isActive: bool
} with

    static member create(name: string, path: string, isActive: bool) = {
        name = name
        path = path
        isActive = isActive
    }

type SelectorRef = { toggle: unit -> unit }