module Swate.Components.Primitive.Tree.Context

open Feliz
open Swate.Components.Primitive.Tree.Types

let TreeCtx = React.createContext<TreeContextValue<obj> option> None

[<Hook>]
let useTreeCtx<'T> () =
    React.useContext TreeCtx |> Option.map unbox<TreeContextValue<'T>>

let toBoxedContext (value: TreeContextValue<'T>) =
    value |> box |> unbox<TreeContextValue<obj>>
