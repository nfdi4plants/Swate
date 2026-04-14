module Swate.Components.Context

open Browser.Dom
open Browser.Types
open Feliz
open Swate.Components.Types

type TableState
    (
        isActive: CellCoordinate -> bool,
        isOrigin: CellCoordinate -> bool,
        isSelected: CellCoordinate -> bool,
        onBlur: CellCoordinate -> FocusEvent -> unit,
        onKeyDown: CellCoordinate -> KeyboardEvent -> unit,
        onClick: CellCoordinate -> MouseEvent -> unit
    ) =
    member val isActive = isActive with get, set
    member val isOrigin = isOrigin with get, set
    member val isSelected = isSelected with get, set
    member val onBlur = onBlur with get, set
    member val onKeyDown = onKeyDown with get, set
    member val onClick = onClick with get, set

module TableState =

    let init () =
        TableState(
            (fun _ ->
                console.warn "TableCtx default isActive"
                false
            ),
            (fun _ ->
                console.warn "TableCtx default isOrigin"
                false
            ),
            (fun _ ->
                console.warn "TableCtx default isSelected"
                false
            ),
            (fun _ _ -> console.warn "TableCtx default onBlur"),
            (fun _ _ -> console.warn "TableCtx default onKeyDown"),
            (fun _ _ -> console.warn "TableCtx default onClick")
        )

let TableStateCtx = React.createContext<TableState> (TableState.init ())

[<Hook>]
let useTableStateCtx () = React.useContext TableStateCtx
