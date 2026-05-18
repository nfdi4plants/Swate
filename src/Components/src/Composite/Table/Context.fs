module Swate.Components.Composite.Table.Context

open Browser.Dom
open Browser.Types
open Feliz
open Swate.Components
open Swate.Components.Composite.Table.Types

let TableStateCtx = React.createContext<TableState> (TableState.init ())

[<Hook>]
let useTableStateCtx () = React.useContext TableStateCtx