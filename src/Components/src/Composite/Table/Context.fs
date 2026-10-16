module ARCitISA.Components.Composite.Table.Context

open Browser.Dom
open Browser.Types
open Feliz
open ARCitISA.Components
open ARCitISA.Components.Composite.Table.Types

let TableStateCtx = React.createContext<TableState> (TableState.init ())

[<Hook>]
let useTableStateCtx () = React.useContext TableStateCtx
